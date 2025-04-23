using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Transactions;
using ClosedXML.Excel;
using Dapper;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Hangfire;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.Common.PostgresTypes;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.Master;
using HomeCentre.PIM.DAL.Staging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using OfficeOpenXml;
using OfficeOpenXml.DataValidation;
using OfficeOpenXml.Style;

namespace HomeCentre.PIM.BAL.Implementations;

public class PgUploadManager : PgEntityAction<UploadedFile>, IUploadManager
{
    private readonly IBlobRepo _blobRepo;
    private readonly IDbCache _cache;
    private readonly IDataProcessorFactory _dataProcessorFactory;
    private readonly IUserContext _userContext;

    public PgUploadManager(IUserContext userContext, IDbConnectionFactory connectionFactory,
        IConfiguration configuration,
        IBlobRepo blobRepo,
        IDbCache cache,
        IDataProcessorFactory dataProcessorFactory)
        : base(userContext, connectionFactory, configuration)
    {
        _blobRepo = blobRepo;
        _cache = cache;
        _dataProcessorFactory = dataProcessorFactory;
        _userContext = userContext;
    }

    public async Task<UploadedFile> UploadFile(UploadedFile file, IDbTransaction transaction = null)
    {
        var result = await this.ValidationForUploadexcel(file);

        var existingFile = await this.Get(file.Id);

        // if published
        if (existingFile?.UploadedFileStatusId == 5)
        {
            throw new HandledException("File is already published");
        }

        if (existingFile != null)
        {
            file.CreatedBy = existingFile.CreatedBy;
            file.UpdatedBy = existingFile.UpdatedBy;
            file.TemplateId = existingFile.TemplateId;
            file.UploadedFileStatusId = existingFile.UploadedFileStatusId;
        }
        else
        {
            file.UploadedFileStatusId = 4; // failed
        }

        return await this.AddOrUpdate(file, transaction: transaction);
    }

    public async Task<List<UploadedFileData>> ParseFile(UploadedFile file, FileConfiguration fileConfiguration)
    {
        var tempFile = await _blobRepo.DownloadFile(file.BlobUrl);
        using var wb = new XLWorkbook(tempFile, XLEventTracking.Disabled);
        if (wb.CustomProperties.FirstOrDefault(x => x.Name == "templateId")?.Value?.ToString() != file.TemplateId.ToString())
        {
            throw new InvalidDataException("Template mismatch between uploaded file and selected template");
        }

        var ws = wb.Worksheet(1);
        var lastRow = ws.LastRowUsed().RowNumber();
        var rows = ws.Rows(1, lastRow);
        foreach (var row in rows)
        {
            if (row.IsEmpty())
            {
                row.Delete();
            }
        }

        var dataTable = ws.RangeUsed().AsTable().AsNativeDataTable();

        var dataList = new List<UploadedFileData>();
        foreach (DataRow row in dataTable.Rows)
        {
            var data = new UploadedFileData
            {
                ColumnData = row.Table.Columns
                    .Cast<DataColumn>()
                    .ToDictionary(c => c.ColumnName, c => row[c].ToFirstLetterUpperCase()),
                ErrorData = new Dictionary<string, List<string>>(),
                UploadedFileId = file.Id,
                DateCreated = DateTimeOffset.UtcNow,
                DateModified = DateTimeOffset.UtcNow,
                CreatedBy = await _userContext.GetUserId(),
                UpdatedBy = await _userContext.GetUserId(),
                IsActive = true
            };

            dataList.Add(data);
        }

        using var connection = this.GetConnection();
        using var transaction = connection.BeginTransaction();
        var dtNew = dataList.ToArray().GetDataTableFromObjects();
        transaction.BulkInsert(nameof(UploadedFileData), dtNew);
        transaction.Commit();

        return dataList;
    }

    public async Task<List<UploadedFileData>> ProcessFile(UploadedFile file)
    {
        using var connection = this.GetConnection();
        var userid = await _userContext.GetUserId();
        using var transaction = connection.BeginTransaction();
        await connection.ExecuteAsync("delete from UploadedFileData where uploadedfileid = @fileId", new
        {
            fileId = file.Id
        });

        var dataTable = this.ValidationForUploadexcel(file).Result;
        var attribute = await connection.QueryAsync<ProductAttribute>("select distinct name from productattribute where datatype = 0");

        var dataLists = dataTable.Rows.Cast<DataRow>()
            .Where(row => row != null)
            .Select(row => new UploadedFileData
            {
                ColumnData = row.Table.Columns
                                .Cast<DataColumn>()
                                .ToDictionary(c => c.ColumnName, c =>
                                    c.DataType == typeof(string) &&
                                    !attribute.Select(attr => attr.Name).Contains(c.ColumnName) ?
                                    row[c].ToString().MakeFirstLetterCapital() :
                                    row[c]),
                ErrorData = new Dictionary<string, List<string>>(),
                UploadedFileId = file.Id,
                DateCreated = DateTimeOffset.UtcNow,
                DateModified = DateTimeOffset.UtcNow,
                CreatedBy = userid,
                UpdatedBy = userid,
                IsActive = true
            })
            .ToList();

        var dataProcessor = await _dataProcessorFactory.GetDataProcessor(file.TableName);
        dataLists = await dataProcessor.ValidateData(dataLists, file.TemplateId);
        var errorCount = dataLists.Count(x => x.ErrorData.Values.Count > 0);
        await this.DoUpdate(
            new
            {
                Id = file.Id,
                ErrorCount = errorCount,
                UploadedFileStatusId = errorCount > 0 ? 4 : 3 // failed : success
            }, nameof(UploadedFile));

        var dtNew = dataLists.ToArray().GetDataTableFromObjects();
        transaction.BulkInsert(nameof(UploadedFileData), dtNew);
        transaction.Commit();
        return dataLists;
    }

    public async Task<DataTable> ValidationForUploadexcel(UploadedFile file)
    {
        using var connection = this.GetConnection();
        var templateId = file.TemplateId;
        var tempFile = await _blobRepo.DownloadFile(file.BlobUrl);
        using var wb = new XLWorkbook(tempFile, XLEventTracking.Disabled);
        var ws = wb.Worksheet(1);
        ws.Tables.Select(x => x.ShowAutoFilter = false);
        var dataTable = ws.Tables.Any() ? ws.Tables.First().AsNativeDataTable() : ws.RangeUsed().AsTable().AsNativeDataTable();
        if (dataTable.Columns.Contains("error data"))
        {
            dataTable.Columns.Remove("error data");
        }

        for (int i = dataTable.Rows.Count - 1; i >= 0; i--)
        {
            DataRow row = dataTable.Rows[i];
            bool isEmptyRow = true;

            foreach (var item in row.ItemArray)
            {
                if (item != null && !string.IsNullOrEmpty(item.ToString()))
                {
                    isEmptyRow = false;
                    break;
                }
            }

            if (isEmptyRow)
            {
                dataTable.Rows.RemoveAt(i);
            }
        }

        var dataRows = dataTable.Rows.Cast<DataRow>().Where(row => !row.ItemArray.All(field => field is DBNull || string.IsNullOrWhiteSpace(field?.ToString())));
        var templateAttributes = (await connection.QueryAsync<string>(
            "select lower(pa.name) from productattribute pa join templateattribute ta on ta.attributeid = pa.id where ta.templateid = @templateId and ta.isactive  and pa.isactive",
            new
            {
                file.TemplateId
            })).ToList();
        var inputColumns = dataTable.Columns.Cast<DataColumn>().Select(x => x.ColumnName.ToLower()).ToList();
        var missingInAttributes = templateAttributes.Except(inputColumns).Where(x => x != "Uid").ToList();
        var missingInInputs = inputColumns.Except(templateAttributes).Where(x => x != "Uid").ToList();

        if (missingInInputs.Any())
        {
            throw new MissingMemberException($"Additional columns found '{string.Join(",", missingInInputs)}'.");
        }

        if (missingInAttributes.Any())
        {
            throw new MissingMemberException($"Columns missing, '{string.Join(",", missingInInputs)}'.");
        }

        if (!dataRows.Any())
        {
            throw new Exception("Uploaded Excel file has no data.");
        }

        return dataTable;
    }

    public async Task<SearchResult<UploadedFile>> SearchFiles(string searchKey, int pageSize, int pageCount, long? templateId)
    {
        var dynParams = new DynamicParameters();
        dynParams.Add("uploadedFileId", templateId);
        dynParams.Add("userId", await this.GetUserId());
        return await this.Search(searchKey, pageSize, pageCount, " templateId = @templateId and createdby = @userId", dynParams, orderBy: "Id", orderByAsc: false);
    }

    public async Task<List<string>> GetTemplateKeys(long uploadedfileid)
    {
        string query = $"SELECT pa.name FROM productattribute pa INNER JOIN templateattribute b ON b.attributeid = pa.id INNER JOIN uploadedfile c ON c.templateid = b.templateid WHERE c.id = {uploadedfileid} ORDER BY b.order; ";
        using (var connection = this.GetConnection())
        {
            var result = (await connection.QueryAsync<string>(query)).ToList();
            return result;
        }
    }

    public async Task<SearchResult<UploadedFileData>> SearchRecords(string searchKey, int pageSize, int pageCount,
        long uploadedFileId)
    {
        var dynParams = new DynamicParameters();
        dynParams.Add("uploadedFileId", uploadedFileId);
        return await this.Search<UploadedFileData>(searchKey, pageSize, pageCount, nameof(UploadedFileData),
            "uploadedFileId = @uploadedFileId", dynParams);
    }

    public async Task UpdateRecords(IEnumerable<UploadedFileData> records)
    {
        var recordList = records.ToList();
        if (!recordList.Any())
        {
            return;
        }

        var file = await this.Get(recordList.First().UploadedFileId);
        var dataProcessor = await _dataProcessorFactory.GetDataProcessor(file.Name);
        await dataProcessor.SaveData(recordList, file.TemplateId);
    }

    public async Task PublishFile(UploadedFile file)
    {
        file = await this.Get(file.Id);

        // if not completed throw error
        if (file.UploadedFileStatusId != 3)
        {
            throw new ValidationException("There are errors in file");
        }

        using var connection = this.GetConnection();
        using var transaction = connection.BeginTransaction();
        await this.DoUpdate(
            new
            {
                Id = file.Id,
                UploadedFileStatusId = 5 // published
            }, nameof(UploadedFile), transaction: transaction);

        var dataList = await connection.GetListAsync<UploadedFileData>("where uploadedfileid = @fileId", new
        {
            fileId = file.Id
        });
        var newList = new List<UploadedFileData>(dataList);
        var uidtempdata = new Product();

        var templates = await _cache.GetTemplates();
        var template = templates.FirstOrDefault(x => x.Id == file.TemplateId);
        var userId = await this.GetUserId();

        var division = (await _cache.GetMaster<Division>()).FirstOrDefault(x => x.Id == template.DivisionId);

        var newDataList = new List<Product>();

        var uidKeys = division.UidKeys?.Split(",") ?? Array.Empty<string>();

        var completedUids = new List<string>();
        var templateUids = new List<string>();
        bool uidExist = false;
        bool uidExist1 = false;
        var templateColumns = (await _cache.GetTemplateAttributes(file.TemplateId)).Select(x => x.AttributeId).ToList();
        var validColumns = (await _cache.GetAttributes(template.DivisionId.GetValueOrDefault()))
            .Where(x => templateColumns.Contains(x.Id)).ToList();
        if (template.IsUidTemplate)
        {
            var uids = dataList.Select(productList => productList?.ColumnData?["Uid"]?.ToString()).Where(uid => !string.IsNullOrEmpty(uid)).ToList();
            if (uids.Count == 0)
            {
                uidExist = true;
            }

            if (uids.Any())
            {
                List<string> uidss = new List<string>();

                string selectQuery = $"select * from product where uid IN ({string.Join(",", uids.Select(uid => $"'{uid}'"))})";
                var productsToUpdate = await connection.QueryAsync<Product>(selectQuery);

                var uidList = newList.Select(x => x.ColumnData.ContainsKey("Uid") ? x.ColumnData["Uid"]?.ToString() : string.Empty).ToList();
                var deptList = newList.Select(x => x.ColumnData.ContainsKey("Dept Code") ? x.ColumnData["Dept Code"]?.ToString() : string.Empty).ToList();
                var duplicateClassList = newList.Select(x => x.ColumnData.ContainsKey("Class Code") ? x.ColumnData["Class Code"]?.ToString() : string.Empty).ToList();
                var duplicatesubClassList = newList.Select(x => x.ColumnData.ContainsKey("Subclass Code") ? x.ColumnData["Subclass Code"]?.ToString() : string.Empty).ToList();
                var duplicateGroupList = newList.Select(x => x.ColumnData.ContainsKey("Group Code") ? x.ColumnData["Group Code"]?.ToString() : string.Empty).ToList();
                var duplicateDivList = newList.Select(x => x.ColumnData.ContainsKey("Division Code") ? x.ColumnData["Division Code"]?.ToString() : string.Empty).ToList();
                var seasonList = newList.Select(x => x.ColumnData.ContainsKey("Season Code") ? x.ColumnData["Season Code"]?.ToString() : string.Empty).ToList();
                var mrpList = newList.Select(x => x.ColumnData.ContainsKey("Target MRP") ? x.ColumnData["Target MRP"]?.ToString() : string.Empty).ToList();
                var planoList = newList.Select(x => x.ColumnData.ContainsKey("Planogram/Family Code") ? x.ColumnData["Planogram/Family Code"]?.ToString() : string.Empty).ToList();
                var productDescList = newList.Select(x => x.ColumnData.ContainsKey("Product Desc") ? x.ColumnData["Product Desc"]?.ToString() : string.Empty).ToList();
                var priceList = newList.Select(x => x.ColumnData.ContainsKey("Price Grouping") ? x.ColumnData["Price Grouping"]?.ToString() : string.Empty).ToList();
                var purposeList = newList.Select(x => x.ColumnData.ContainsKey("Product Purpose") ? x.ColumnData["Product Purpose"]?.ToString() : string.Empty).ToList();

                var parameterList = new List<Dictionary<string, string>>();

                Parallel.For(0, newList.Count, i =>
                {
                    var parameters = new Dictionary<string, string>
    {
        { "Uid", uidList[i] },
        { "Dept Code", deptList[i] },
        { "Class Code", duplicateClassList[i] },
        { "Group Code", duplicateGroupList[i] },
        { "Subclass Code", duplicatesubClassList[i] },
        { "Division Code", duplicateDivList[i] },
        { "Season Code", seasonList[i] },
        { "Target MRP", mrpList[i] },
        { "Planogram/Family Code", planoList[i] },
        { "Product Desc", productDescList[i] },
        { "Price Grouping", priceList[i] },
        { "Product Purpose", purposeList[i] }
    };

                    parameterList.Add(parameters);
                });

                var jsonList = parameterList.Select(parameters =>
                {
                    var serializedParameters = parameters.ToDictionary(
                        kvp => kvp.Key,
                        kvp =>
                        {
                            if (kvp.Key == "Uid" || kvp.Key == "Product Desc")
                            {
                                return $@"""{kvp.Value}""";
                            }
                            else
                            {
                                return kvp.Value?.ToString();
                            }
                        });

                    return $"'{{ {string.Join(", ", serializedParameters.Select(kv => $@"""{kv.Key}"": {kv.Value}"))} }}'";
                }).ToList();

                var parametersString = $"ARRAY[{string.Join(",", jsonList)}]::JSONB[]";

                var result_val = await connection.ExecuteAsync($"SELECT * FROM update_uid_data({parametersString})");

                // Parallel.ForEach(dataList, productList =>
                // {
                //    using var connection = this.GetConnection();
                //    var uid = productList?.ColumnData?["Uid"]?.ToString();

                // if (!string.IsNullOrEmpty(uid))
                //    {
                //        var existingProduct = productsToUpdate.FirstOrDefault(p => p.Uid == uid);

                // if (existingProduct != null)
                //        {
                //            string updateQuery = $"UPDATE product " +
                //                                $"SET columndata = jsonb_set(jsonb_set(jsonb_set(jsonb_set(jsonb_set(jsonb_set(jsonb_set(jsonb_set(jsonb_set(jsonb_set(jsonb_set(columndata, " +
                //                                $"'{{\"Product Desc\"}}', '\"{productList.ColumnData["Product Desc"]}\"')::jsonb, " +
                //                                $" '{{\"Season Code\"}}', '\"{productList.ColumnData["Season Code"]}\"')::jsonb, " +
                //                                $" '{{\"Dept Code\"}}', '\"{productList.ColumnData["Dept Code"]}\"')::jsonb, " +
                //                                $" '{{\"Division Code\"}}', '\"{productList.ColumnData["Division Code"]}\"')::jsonb, " +
                //                                $" '{{\"Class Code\"}}', '\"{productList.ColumnData["Class Code"]}\"')::jsonb, " +
                //                                $" '{{\"Subclass Code\"}}', '\"{productList.ColumnData["Subclass Code"]}\"')::jsonb, " +
                //                                $" '{{\"Group Code\"}}', '\"{productList.ColumnData["Group Code"]}\"')::jsonb, " +
                //                                $" '{{\"Target MRP\"}}', '\"{productList.ColumnData["Target MRP"]}\"')::jsonb, " +
                //                                $" '{{\"Planogram/Family Code\"}}', '\"{productList.ColumnData["Planogram/Family Code"]}\"')::jsonb, " +
                //                                $" '{{\"Price Grouping\"}}', '\"{productList.ColumnData["Price Grouping"]}\"')::jsonb, " +
                //                                $" '{{\"Product Purpose\"}}', '\"{productList.ColumnData["Product Purpose"]}\"')::jsonb " +
                //                                $"WHERE uid IN ('{uid}')";
                //            connection.Execute(updateQuery);
                //        }
                //    }
                // });
            }

            if (uidExist)
            {
                var sequences = (await connection.QueryAsync<string>(
                    "select LPAD(nextval('product_sequence')::text, 8, '0') from generate_series(1, @countOfList);", new
                    {
                        countOfList = dataList.Count()
                    })).ToArray();

                HashSet<string> usedSequences = new HashSet<string>();
                object lockObject = new object();

                Parallel.For(0, sequences.Length, i =>
                {
                    lock (lockObject)
                    {
                        while (usedSequences.Contains(sequences[i]))
                        {
                            int sequenceValue = int.Parse(sequences[i]);
                            sequenceValue++;
                            sequences[i] = sequenceValue.ToString().PadLeft(4, '0');
                        }

                        usedSequences.Add(sequences[i]);
                    }
                });

                newDataList.AddRange(dataList.Select((x, i) =>
                {
                    var uid = string.Empty;
                    string sqlQuery1 = $"SELECT name FROM season WHERE code = '{x.ColumnData["Season Code"]}'";
                    var result = connection.QueryAsync(sqlQuery1).Result.FirstOrDefault();
                    if (result != null)
                    {
                        dynamic dynamicResult = result;
                        string name = dynamicResult.name;

                        if (!string.IsNullOrEmpty(name))
                        {
                            uid = $"{name}{x.ColumnData["Dept Code"]}{sequences[i]}";
                            x.ColumnData["Uid"] = uid;
                        }
                    }

                    var p = new Product
                    {
                        Uid = uid,
                        IsUidGenerated = x.ErrorData.Count == 0,
                        UidData = x.ColumnData.Where(y => uidKeys.Contains(y.Key)).ToDictionary(k => k.Key, v => v.Value),
                        ColumnData = x.ColumnData.Where(y => !uidKeys.Contains(y.Key)).ToDictionary(k => k.Key, v => v.Value),
                        ErrorData = x.ErrorData,
                        DateCreated = DateTimeOffset.UtcNow,
                        DateModified = DateTimeOffset.UtcNow,
                        CreatedBy = x.CreatedBy,
                        UpdatedBy = x.CreatedBy,
                        IsActive = true,
                        QcStatusId = 1
                    };
                    p.ColumnData["Uid"] = uid;
                    return p;
                }));

                completedUids.AddRange(newDataList
              .Where(x => x.IsUidGenerated)
              .Select(product => product.Uid));

                transaction.BulkMerge("uploadedfiledata", dataList.ToArray().GetDataTableFromObjects());
                uidExist1 = true;
            }
        }
        else
        {
            var tasks = dataList.Select(async productList =>
            {
                using var connection = this.GetConnection();
                var uidtempdata = (await connection.QueryAsync<Product>($"select columndata, pictureid,datecreated from product where uid = '{productList.ColumnData["Uid"]}'")).FirstOrDefault();

                var values = await this.ItemCodeAttributeJsonExtraction();

                var columnDataKeys = uidtempdata.ColumnData.Keys.Concat(productList.ColumnData.Keys);
                var count = values
                    .Where(key => key.Color.ToLower() == "w" && !string.IsNullOrWhiteSpace(key.Value))
                    .Select(key =>
                    {
                        key.Attribute = key.Value;
                        return key.Attribute;
                    })
                    .Where(attribute => !columnDataKeys.Contains(attribute))
                    .Count();

                var statusUpdate = count == 0 ? 1 : 0;

                await connection.QueryAsync<Product>($"update product set status = '{statusUpdate}' where uid = '{productList.ColumnData["Uid"]}'");

                return new Product
                {
                    Uid = productList.ColumnData.ContainsKey("Uid") ? productList.ColumnData["Uid"].ToString() : string.Empty,
                    ColumnData = productList.ColumnData
        .Where(y => !uidKeys.Contains(y.Key))
        .ToDictionary(k => k.Key, v =>
        {
            if (string.IsNullOrWhiteSpace(v.Value.ToString()))
            {
                if (productList.ColumnData.TryGetValue(v.Key, out var previousValue))
                {
                    return previousValue;
                }
                else
                {
                    return string.Empty;
                }
            }

            return v.Value;
        })
        .Concat(uidtempdata.ColumnData.Where(y => !productList.ColumnData.ContainsKey(y.Key)))
        .ToDictionary(k => k.Key, v =>
        {
            if (string.IsNullOrWhiteSpace(v.Value.ToString()))
            {
                if (uidtempdata.ColumnData.TryGetValue(v.Key, out var previousValue))
                {
                    return previousValue;
                }
                else
                {
                    return string.Empty;
                }
            }

            return v.Value;
        }),
                    ErrorData = productList.ErrorData,
                    CreatedBy = productList.CreatedBy,
                    UpdatedBy = productList.UpdatedBy,

                    // DateCreated = DateTimeOffset.UtcNow,
                    DateCreated = uidtempdata.DateCreated,
                    DateModified = DateTimeOffset.UtcNow,
                    PictureId = uidtempdata.PictureId,
                    IsUidGenerated = true,
                    IsActive = true,
                    UidData = productList.ColumnData
        .Where(y => uidKeys.Contains(y.Key))
        .ToDictionary(k => k.Key, v => v.Value)
                };
            });

            var results = await Task.WhenAll(tasks);

            newDataList.AddRange(results);

            templateUids.AddRange(newDataList.Select(x => x.Uid));
            uidExist1 = true;
        }

        if (uidExist1)
        {
            var dtProducts = newDataList.ToArray().GetDataTableFromObjects();
            transaction.BulkMerge(nameof(Product), dtProducts, new[] { "uid" });
            var allIdRecords = await connection.GetListAsync<Product>("where uid = any(@ids)", new
            {
                ids = completedUids
            });

            var templateRecs = await connection.GetListAsync<Product>("where uid = any(@ids)", new
            {
                ids = templateUids
            });

            var templatesOfDivision = templates.Where(x => x.DivisionId == template.DivisionId).ToList();
            var productTemplates = allIdRecords
                .SelectMany(product => templatesOfDivision
                    .Select(tod => new ProductTemplate
                    {
                        TemplateId = tod.Id,
                        ProductId = product.Id,
                        ProductTemplateStatusId = tod.IsUidTemplate && product.IsUidGenerated ? 3 : 2, // completed - 3, started - 2
                        DateCreated = DateTimeOffset.UtcNow,
                        DateModified = DateTimeOffset.UtcNow,
                        CreatedBy = userId,
                        UpdatedBy = userId,
                        IsActive = true
                    }))
                .ToList();

            productTemplates.AddRange(templateRecs
                .Select(rec => new ProductTemplate
                {
                    TemplateId = file.TemplateId,
                    ProductId = rec.Id,
                    ProductTemplateStatusId = !rec.ErrorData.Any() ? 3 : 2, // completed - 3, started - 2
                    DateCreated = DateTimeOffset.UtcNow,
                    DateModified = DateTimeOffset.UtcNow,
                    CreatedBy = userId,
                    UpdatedBy = userId,
                    IsActive = true
                }));

            transaction.BulkMerge(nameof(ProductTemplate), productTemplates.ToArray().GetDataTableFromObjects(), new[] { "templateid", "productid" });
        }

        if (newDataList.Count != 0)
        {
            if (template.IsUidTemplate)
            {
                var uidList = newDataList.Select(x => x.ColumnData.ContainsKey("Uid") ? x.ColumnData["Uid"]?.ToString() : string.Empty).ToList();
                var deptList = newDataList.Select(x => x.ColumnData.ContainsKey("Dept Code") ? x.ColumnData["Dept Code"]?.ToString() : string.Empty).ToList();
                var duplicateClassList = newDataList.Select(x => x.ColumnData.ContainsKey("Class Code") ? x.ColumnData["Class Code"]?.ToString() : string.Empty).ToList();
                var duplicatesubClassList = newDataList.Select(x => x.ColumnData.ContainsKey("Subclass Code") ? x.ColumnData["Subclass Code"]?.ToString() : string.Empty).ToList();
                var duplicateGroupList = newDataList.Select(x => x.ColumnData.ContainsKey("Group Code") ? x.ColumnData["Group Code"]?.ToString() : string.Empty).ToList();
                var duplicateDivList = newDataList.Select(x => x.ColumnData.ContainsKey("Division Code") ? x.ColumnData["Division Code"]?.ToString() : string.Empty).ToList();

                var parameterList = new List<Dictionary<string, string>>();

                Parallel.For(0, newDataList.Count, i =>
                {
                    var parameters = new Dictionary<string, string>
    {
        { "Uid", uidList[i] },
        { "Dept Code", deptList[i] },
        { "Class Code", duplicateClassList[i] },
        { "Group Code", duplicateGroupList[i] },
        { "Subclass Code", duplicatesubClassList[i] },
        { "Division Code", duplicateDivList[i] }
    };

                    parameterList.Add(parameters);
                });

                int batchSize = 200;
                int totalRecords = parameterList.Count;
                int numberOfBatches = (int)Math.Ceiling((double)totalRecords / batchSize);

                for (int batchNumber = 0; batchNumber < numberOfBatches; batchNumber++)
                {
                    var batch = parameterList
                        .Skip(batchNumber * batchSize)
                        .Take(batchSize)
                        .Select(parameters =>
                        {
                            var serializedParameters = parameters.ToDictionary(
                                kvp => kvp.Key,
                                kvp =>
                                {
                                    if (kvp.Key == "Uid")
                                    {
                                        return $@"""{kvp.Value}""";
                                    }
                                    else
                                    {
                                        return kvp.Value?.ToString();
                                    }
                                });

                            return $"'{{ {string.Join(", ", serializedParameters.Select(kv => $@"""{kv.Key}"": {kv.Value}"))} }}'";
                        }).ToList();

                    var parametersString = $"ARRAY[{string.Join(",", batch)}]::JSONB[]";

                    await connection.ExecuteAsync($"SELECT * FROM update_product_columndata_batch({parametersString})");
                }

                // var jsonList = parameterList.Select(parameters =>
                // {
                //    var serializedParameters = parameters.ToDictionary(
                //        kvp => kvp.Key,
                //        kvp =>
                //        {
                //            if (kvp.Key == "Uid")
                //            {
                //                return $@"""{kvp.Value}""";
                //            }
                //            else
                //            {
                //                return kvp.Value?.ToString();
                //            }
                //        });

                // return $"'{{ {string.Join(", ", serializedParameters.Select(kv => $@"""{kv.Key}"": {kv.Value}"))} }}'";
                // }).ToList();

                // var parametersString = $"ARRAY[{string.Join(",", jsonList)}]::JSONB[]";

                // var result_val = await connection.ExecuteAsync($"SELECT * FROM update_product_columndata_batch({parametersString})");
            }
        }

        BackgroundJob.Enqueue(() => UpdateFile(newDataList, template));

        transaction.Commit();
        BackgroundJob.Enqueue(() => Columndata(newDataList, newList, template, file));
        BackgroundJob.Enqueue(() => Updatestatus(newDataList, newList, template, file));
        await this._cache.Clear();
    }

    public async Task Columndata(List<Product> dataList, List<UploadedFileData> datas, Template template, UploadedFile file)
    {
        using var connection = this.GetConnection();
        if (template.IsUidTemplate)
        {
            foreach (var list in dataList)
            {
                if (file.TemplateId == 2 || file.TemplateId == 3 || file.TemplateId == 17)
                {
                    string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(list.ColumnData);
                    await connection.ExecuteAsync("INSERT INTO productcolumndata (uid, uidcolumndata, buyer1columndata, buyer2columndata) VALUES (@Uid, CAST(@JsonData AS jsonb), NULL, NULL)", new { Uid = list.Uid, JsonData = jsonData });
                    var data = await connection.QueryAsync<ProductColumndata>($"SELECT uid, uidcolumndata FROM productcolumndata WHERE uid = '{list.Uid}'");
                    var productColumndata = data.FirstOrDefault();

                    if (productColumndata != null)
                    {
                        await connection.ExecuteAsync($"UPDATE product SET uidcolumndata = '{JsonConvert.SerializeObject(productColumndata.UidColumndata)}' WHERE uid = '{list.Uid}'");
                    }
                }
            }
        }

        if (!template.IsUidTemplate)
        {
            foreach (var list in datas)
            {
                if (file.TemplateId == 4 || file.TemplateId == 5 || file.TemplateId == 19)
                {
                    string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(list.ColumnData);
                    await connection.ExecuteAsync($"update productcolumndata set buyer1columndata = '{jsonData}' where uid = '{list.ColumnData["Uid"]}'");
                    var data = await connection.QueryAsync<ProductColumndata>($"SELECT uid, uidcolumndata, buyer1columndata FROM productcolumndata WHERE uid = '{list.ColumnData["Uid"]}'");
                    foreach (var item in data)
                    {
                        await connection.ExecuteAsync($"UPDATE product SET buyer1columndata = '{JsonConvert.SerializeObject(item.Buyer1Columndata)}', uidcolumndata = '{JsonConvert.SerializeObject(item.UidColumndata)}' WHERE uid = '{list.ColumnData["Uid"]}'");
                    }
                }
                else if (file.TemplateId == 6 || file.TemplateId == 7 || file.TemplateId == 18)
                {
                    string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(list.ColumnData);
                    await connection.ExecuteAsync($"update productcolumndata set buyer2columndata = '{jsonData}' where uid = '{list.ColumnData["Uid"]}'");
                    var data = await connection.QueryAsync<ProductColumndata>($"SELECT uid, uidcolumndata, buyer1columndata, buyer2columndata FROM productcolumndata WHERE uid = '{list.ColumnData["Uid"]}'");
                    foreach (var item in data)
                    {
                        await connection.ExecuteAsync($"UPDATE product SET buyer2columndata = '{JsonConvert.SerializeObject(item.Buyer2Columndata)}', buyer1columndata = '{JsonConvert.SerializeObject(item.Buyer1Columndata)}', uidcolumndata = '{JsonConvert.SerializeObject(item.UidColumndata)}' WHERE uid = '{list.ColumnData["Uid"]}'");
                    }
                }
                else if (file.TemplateId == 8 || file.TemplateId == 9 || file.TemplateId == 22)
                {
                    string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(list.ColumnData);
                    var care = jsonData.Replace("Care Instruction 3 Caring Don't", "Care Instruction 3 Caring Don''t");
                    await connection.ExecuteAsync($"update productcolumndata set qccolumndata = '{care}' where uid = '{list.ColumnData["Uid"]}'");
                    var data = await connection.QueryAsync<ProductColumndata>($"SELECT uid, uidcolumndata, buyer1columndata, buyer2columndata, qccolumndata FROM productcolumndata WHERE uid = '{list.ColumnData["Uid"]}'");
                    foreach (var item in data)
                    {
                        var careins = Newtonsoft.Json.JsonConvert.SerializeObject(item.QcColumnData);
                        var instruction = careins.Replace("Care Instruction 3 Caring Don't", "Care Instruction 3 Caring Don''t");
                        await connection.ExecuteAsync($"UPDATE product SET qccolumndata = '{instruction}', buyer2columndata = '{JsonConvert.SerializeObject(item.Buyer2Columndata)}', buyer1columndata = '{JsonConvert.SerializeObject(item.Buyer1Columndata)}', uidcolumndata = '{JsonConvert.SerializeObject(item.UidColumndata)}' WHERE uid = '{list.ColumnData["Uid"]}'");
                    }
                }
                else if (file.TemplateId == 10 || file.TemplateId == 11 || file.TemplateId == 21)
                {
                    string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(list.ColumnData);
                    await connection.ExecuteAsync($"update productcolumndata set packagingcolumndata = '{jsonData}' where uid = '{list.ColumnData["Uid"]}'");
                    var data = await connection.QueryAsync<ProductColumndata>($"SELECT uid, uidcolumndata, buyer1columndata, buyer2columndata, qccolumndata, packagingcolumndata FROM productcolumndata WHERE uid = '{list.ColumnData["Uid"]}'");
                    foreach (var item in data)
                    {
                        var careins = Newtonsoft.Json.JsonConvert.SerializeObject(item.QcColumnData);
                        var instruction = careins.Replace("Care Instruction 3 Caring Don't", "Care Instruction 3 Caring Don''t");
                        await connection.ExecuteAsync($"UPDATE product SET packagingcolumndata = '{JsonConvert.SerializeObject(item.PackagingColumnData)}', qccolumndata = '{instruction}', buyer2columndata = '{JsonConvert.SerializeObject(item.Buyer2Columndata)}', buyer1columndata = '{JsonConvert.SerializeObject(item.Buyer1Columndata)}', uidcolumndata = '{JsonConvert.SerializeObject(item.UidColumndata)}' WHERE uid = '{list.ColumnData["Uid"]}'");
                    }
                }
                else if (file.TemplateId == 12 || file.TemplateId == 13)
                {
                    string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(list.ColumnData);
                    await connection.ExecuteAsync($"update productcolumndata set fabcolumndata = '{jsonData}' where uid = '{list.ColumnData["Uid"]}'");
                    var data = await connection.QueryAsync<ProductColumndata>($"SELECT uid, uidcolumndata, buyer1columndata, buyer2columndata, qccolumndata, packagingcolumndata, fabcolumndata FROM productcolumndata WHERE uid = '{list.ColumnData["Uid"]}'");
                    foreach (var item in data)
                    {
                        var careins = Newtonsoft.Json.JsonConvert.SerializeObject(item.QcColumnData);
                        var instruction = careins.Replace("Care Instruction 3 Caring Don't", "Care Instruction 3 Caring Don''t");
                        await connection.ExecuteAsync($"UPDATE product SET fabcolumndata = '{JsonConvert.SerializeObject(item.FabColumnData)}', packagingcolumndata = '{JsonConvert.SerializeObject(item.PackagingColumnData)}', qccolumndata = '{instruction}', buyer2columndata = '{JsonConvert.SerializeObject(item.Buyer2Columndata)}', buyer1columndata = '{JsonConvert.SerializeObject(item.Buyer1Columndata)}', uidcolumndata = '{JsonConvert.SerializeObject(item.UidColumndata)}' WHERE uid = '{list.ColumnData["Uid"]}'");
                    }
                }
            }
        }
    }

    public async Task UpdateFile(List<Product> dataList, Template template)
    {
        using var connection = this.GetConnection();

        if (!template.IsUidTemplate)
        {
            foreach (var productList in dataList)
            {
                long? brandCode = productList.ColumnData.ContainsKey("Brand Code") ? Convert.ToInt64(productList.ColumnData["Brand Code"]) : (long?)null;
                long? uom = productList.ColumnData.ContainsKey("UOM") ? Convert.ToInt64(productList.ColumnData["UOM"]) : (long?)null;
                long? purchaseTypeCode = productList.ColumnData.ContainsKey("Purchase Type") ? Convert.ToInt64(productList.ColumnData["Purchase Type"]) : (long?)null;
                long? supplierCode = productList.ColumnData.ContainsKey("RMS Supplier Code") ? Convert.ToInt64(productList.ColumnData["RMS Supplier Code"]) : (long?)null;
                long? colorCode = productList.ColumnData.ContainsKey("Color Code") ? Convert.ToInt64(productList.ColumnData["Color Code"]) : (long?)null;
                long? innerCartonCode = productList.ColumnData.ContainsKey("Inner Carton Code") &&
                            !string.IsNullOrEmpty(productList.ColumnData["Inner Carton Code"].ToString())
                 ? (long.TryParse(productList.ColumnData["Inner Carton Code"].ToString(), out long innerCode) ? innerCode : (long?)null)
                 : (long?)null;
                long? masterCartonCode = productList.ColumnData.ContainsKey("Master Carton Code") && !string.IsNullOrEmpty(productList.ColumnData["Master Carton Code"].ToString())
                ? (long.TryParse(productList.ColumnData["Master Carton Code"].ToString(), out long masterCode) ? masterCode : (long?)null)
                : (long?)null;
                long? itemTypeCode = productList.ColumnData.ContainsKey("Item Type code") ? Convert.ToInt64(productList.ColumnData["Item Type code"]) : (long?)null;
                long? plcCode = productList.ColumnData.ContainsKey("PLC Code") ? Convert.ToInt64(productList.ColumnData["PLC Code"]) : (long?)null;
                long? productClassificationCode = productList.ColumnData.ContainsKey("Product Classification code") && !string.IsNullOrEmpty(productList.ColumnData["Product Classification code"].ToString())
               ? (long.TryParse(productList.ColumnData["Product Classification code"].ToString(), out long classificationCode) ? classificationCode : (long?)null)
               : (long?)null;
                string uid = productList.ColumnData.ContainsKey("Uid") ? productList.ColumnData["Uid"].ToString() : " ";
                string subRangeCollectionName = await FetchValueFromProductTable("Sub Range Collection Name", uid);
                string specificProductName = await FetchValueFromProductTable("Specific Product Name", uid);
                string colorDesc = await FetchValueFromProductTable("Color Desc", uid);

                string lString = await FetchValueFromProductTable("Master Carton Dimensions L (CM)", uid);
                string wString = await FetchValueFromProductTable("Master Carton Dimensions W (CM)", uid);
                string hString = await FetchValueFromProductTable("Master Carton Dimensions H (CM)", uid);

                long? mastercartonL = !string.IsNullOrEmpty(lString) ? (long?)Convert.ToInt64(lString) : null;
                long? mastercartonW = !string.IsNullOrEmpty(wString) ? (long?)Convert.ToInt64(wString) : null;
                long? mastercartonH = !string.IsNullOrEmpty(hString) ? (long?)Convert.ToInt64(hString) : null;

                string packageL = await FetchValueFromProductTable("Packaging Product size (L)", uid);
                string packageW = await FetchValueFromProductTable("Packaging Product size (W)", uid);
                string pacakgeH = await FetchValueFromProductTable("Packaging Product size (H)", uid);

                long? packageDimensionL = !string.IsNullOrEmpty(packageL) ? (long?)Convert.ToInt64(packageL) : null;
                long? packageDimensionW = !string.IsNullOrEmpty(packageW) ? (long?)Convert.ToInt64(packageW) : null;
                long? packageDimensionH = !string.IsNullOrEmpty(pacakgeH) ? (long?)Convert.ToInt64(pacakgeH) : null;

                string innerL = await FetchValueFromProductTable("Inner Pack Dimensions L (CM)", uid);
                string innerW = await FetchValueFromProductTable("Inner Pack Dimensions W (CM)", uid);
                string innerH = await FetchValueFromProductTable("Inner Pack Dimensions H (CM)", uid);

                long? innerPackL = !string.IsNullOrEmpty(innerL) ? (long?)Convert.ToInt64(innerL) : null;
                long? innerPackW = !string.IsNullOrEmpty(innerW) ? (long?)Convert.ToInt64(innerW) : null;
                long? innerPackH = !string.IsNullOrEmpty(innerH) ? (long?)Convert.ToInt64(innerW) : null;
                long? rmsseason = productList.ColumnData.ContainsKey("RMS Season Code") ? Convert.ToInt64(productList.ColumnData["RMS Season Code"]) : (long?)null;
                if (brandCode.HasValue || uom.HasValue || purchaseTypeCode.HasValue
                    || supplierCode.HasValue || colorCode.HasValue || innerCartonCode.HasValue || masterCartonCode.HasValue || itemTypeCode.HasValue
                    || plcCode.HasValue || productClassificationCode.HasValue || ((!string.IsNullOrEmpty(subRangeCollectionName) && subRangeCollectionName.Any()) || (!string.IsNullOrEmpty(specificProductName) && specificProductName.Any())
                    || (!string.IsNullOrEmpty(colorDesc) && colorDesc.Any()) || (mastercartonL.HasValue && mastercartonW.HasValue && mastercartonH.HasValue))
                    || (packageDimensionL.HasValue && packageDimensionW.HasValue && packageDimensionH.HasValue)
                    || (innerPackL.HasValue && innerPackW.HasValue && innerPackH.HasValue))
                {
                    var updateColumns = productList.ColumnData.ToDictionary(k => k.Key, v => v.Value);
                    if (rmsseason.HasValue)
                    {
                        string rmsSeasonQuery = $"SELECT name FROM mtrrmsseasoncode WHERE code = {rmsseason}";
                        string rmsSeasonName = await connection.QueryFirstOrDefaultAsync<string>(rmsSeasonQuery);
                        if (!string.IsNullOrEmpty(rmsSeasonName))
                        {
                            updateColumns["RMS Season Name"] = rmsSeasonName;
                        }
                    }

                    if (brandCode.HasValue)
                    {
                        string brandNameQuery = "SELECT name FROM brand WHERE brandcode = @brandCode";
                        string brandName = await connection.QueryFirstOrDefaultAsync<string>(brandNameQuery, new { brandCode });

                        if (!string.IsNullOrEmpty(brandName))
                        {
                            updateColumns["Brand Name"] = brandName;
                        }
                    }

                    if (uom.HasValue)
                    {
                        string uomquery = "SELECT name FROM mtrsetpcs WHERE code = @uom";
                        string uomName = await connection.QueryFirstOrDefaultAsync<string>(uomquery, new { uom });

                        if (!string.IsNullOrEmpty(uomName))
                        {
                            updateColumns["UOM Name"] = uomName;
                        }
                    }

                    if (purchaseTypeCode.HasValue)
                    {
                        string purchaseTypeQuery = "SELECT name FROM purchasetype WHERE code = @purchaseTypeCode";
                        string purchaseTypeName = await connection.QueryFirstOrDefaultAsync<string>(purchaseTypeQuery, new { purchaseTypeCode });

                        if (!string.IsNullOrEmpty(purchaseTypeName))
                        {
                            updateColumns["Purchase Type Desc"] = purchaseTypeName;
                        }
                    }

                    if (supplierCode.HasValue)
                    {
                        string supplierCodeQuery = "SELECT name FROM mtrsuppliercode WHERE code = @supplierCode";
                        string supplierName = await connection.QueryFirstOrDefaultAsync<string>(supplierCodeQuery, new { supplierCode });

                        if (!string.IsNullOrEmpty(supplierName))
                        {
                            updateColumns["Supplier Name"] = supplierName;
                        }
                    }

                    if (colorCode.HasValue)
                    {
                        string colorCodeQuery = "SELECT name FROM mtrproductcolour WHERE code = @colorCode";
                        string colorName = await connection.QueryFirstOrDefaultAsync<string>(colorCodeQuery, new { colorCode });

                        if (!string.IsNullOrEmpty(colorName))
                        {
                            updateColumns["Color Desc"] = colorName;
                        }
                    }

                    if (innerCartonCode.HasValue)
                    {
                        string innercartonCodeQuery = "SELECT name FROM mtrinnercartoncode WHERE code = @innerCartonCode";
                        string innerCartonName = await connection.QueryFirstOrDefaultAsync<string>(innercartonCodeQuery, new { innerCartonCode });

                        if (!string.IsNullOrEmpty(innerCartonName))
                        {
                            updateColumns["Inner Carton Desc"] = innerCartonName;
                        }
                    }

                    if (masterCartonCode.HasValue)
                    {
                        string mastercartonCodeQuery = "SELECT name FROM mtrmastercartoncode WHERE code = @masterCartonCode";
                        string masterCartonName = await connection.QueryFirstOrDefaultAsync<string>(mastercartonCodeQuery, new { masterCartonCode });

                        if (!string.IsNullOrEmpty(masterCartonName))
                        {
                            updateColumns["Master Carton Desc"] = masterCartonName;
                        }
                    }

                    if (itemTypeCode.HasValue)
                    {
                        string itemTypeCodeQuery = "SELECT name FROM mtritemtypecode WHERE code = @itemTypeCode";
                        string itemtypeName = await connection.QueryFirstOrDefaultAsync<string>(itemTypeCodeQuery, new { itemTypeCode });

                        if (!string.IsNullOrEmpty(itemtypeName))
                        {
                            updateColumns["Item Type Desc"] = itemtypeName;
                        }
                    }

                    if (plcCode.HasValue)
                    {
                        string plcCodeQuery = "SELECT name FROM mtrplccode WHERE code = @plcCode";
                        string plcName = await connection.QueryFirstOrDefaultAsync<string>(plcCodeQuery, new { plcCode });

                        if (!string.IsNullOrEmpty(plcName))
                        {
                            updateColumns["PLC Desc"] = plcName;
                        }
                    }

                    if (productClassificationCode.HasValue)
                    {
                        string classificationCodeQuery = "SELECT name FROM mtrproductclassificationcode WHERE code = @productClassificationCode";
                        string classificationName = await connection.QueryFirstOrDefaultAsync<string>(classificationCodeQuery, new { productClassificationCode });

                        if (!string.IsNullOrEmpty(classificationName))
                        {
                            updateColumns["Product Classification Desc"] = classificationName;
                        }
                    }

                    if ((!string.IsNullOrEmpty(subRangeCollectionName) && subRangeCollectionName.Any()) || (!string.IsNullOrEmpty(specificProductName) && specificProductName.Any()) || (!string.IsNullOrEmpty(colorDesc) && colorDesc.Any()))
                    {
                        string shelf = $"{subRangeCollectionName} - {specificProductName} - {colorDesc}";
                        updateColumns["Shelf Edge Description"] = shelf;
                    }

                    if (mastercartonL.HasValue && mastercartonW.HasValue && mastercartonH.HasValue)
                    {
                        long mBM = mastercartonL.Value * mastercartonW.Value * mastercartonH.Value * 1000000;
                        updateColumns["MB CBM"] = mBM;
                    }

                    if (packageDimensionL.HasValue && packageDimensionW.HasValue && packageDimensionH.HasValue)
                    {
                        long productCBM = packageDimensionL.Value * packageDimensionW.Value * packageDimensionH.Value * 1000000;
                        updateColumns["Product CBM"] = productCBM;
                    }

                    if (innerPackL.HasValue && innerPackW.HasValue && innerPackH.HasValue)
                    {
                        long packCartonCBM = innerPackL.Value * innerPackW.Value * innerPackH.Value * 1000000;
                        updateColumns["Inner Pack Carton CBM"] = packCartonCBM;
                    }

                    string updateQuery = "UPDATE product SET " +
                        "columndata = @updateColumns " +
                        "WHERE uid = @uid";

                    await connection.ExecuteAsync(updateQuery, new
                    {
                        updateColumns = updateColumns,
                        uid = productList.ColumnData["Uid"].ToString()
                    });
                }
            }
        }
    }

    public async Task<List<UploadedFileData>> ValidateFile(UploadedFile file)
    {
        var dataProcessor = await _dataProcessorFactory.GetDataProcessor(file.TableName);
        using var connection = this.GetConnection();
        var records = (await connection.GetListAsync<UploadedFileData>("where uploadedFileId = @id", new
        {
            id = file.Id
        })).ToList();
        records = await dataProcessor.ValidateData(records, file.TemplateId);

        var errorCount = records.Count(x => x.ErrorData.Values.Count > 0);
        await this.DoUpdate(
            new
            {
                Id = file.Id,
                ErrorCount = errorCount,
                UploadedFileStatusId = 4 // failed
            }, nameof(UploadedFile));
        await this.UpdateRecords(records);
        return records;
    }

    public async Task<string> DownloadFile(long uploadedFileId)
    {
        using var connection = this.GetConnection();
        var records = (await this.SearchRecords(string.Empty, 0, 0, uploadedFileId)).Records;

        var tempFile = Path.GetTempFileName();
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Sheet1");

        var setPcsDropdownValues = await connection.QueryAsync<MtrSetpcs>("select code from mtrsetpcs");
        var setPcsNames = setPcsDropdownValues.Select(item => item.Code.ToString()).ToArray();
        var leadDropdownValues = await connection.QueryAsync<MtrLeadTime>("select name from mtrleadtime");
        var leadNames = leadDropdownValues.Select(item => item.Name).ToArray();
        var itemShapeDropdownValues = await connection.QueryAsync<MtrItemShape>("select name from mtritemshape");
        var itemNames = itemShapeDropdownValues.Select(item => item.Name).ToArray();
        var finishDropDownValues = await connection.QueryAsync<MtrFinish>("select name from mtrfinish");
        var finsihNames = finishDropDownValues.Select(item => item.Name).ToArray();
        var currency = await connection.QueryAsync<MtrCurrency>("select name from mtrcurrency");
        var currencyNames = currency.Select(item => item.Name).ToArray();
        var itemtype = await connection.QueryAsync<MtrItemTypeCode>("select * from mtritemtypecode");
        var itemtypeNames = itemtype.Select(item => item.Code.ToString()).ToArray();

        Dictionary<string, List<string>> columnDropdownValues = new Dictionary<string, List<string>>
        {
            { "Brand Type", new List<string> { "Brand", "Private Label" } },
            { "Currency", new List<string>(currencyNames) },
            { "Item Type code", new List<string>(itemtypeNames) },
            { "Loipl Shoot Required", new List<string> { "Yes", "No" } },
            { "Primary Packaging", new List<string> { "Yes", "No" } },
            { "set___pcs", new List<string>(setPcsNames) },
            { "Lead Time Days", new List<string>(leadNames) },
            {"Item Shape", new List<string>(itemNames) },
            {"finish", new List<string>(finsihNames) }
        };

        if (records.Count > 0)
        {
            List<string> uidkey1 = this.GetTemplateKeys(uploadedFileId).Result;

            var keys = records[0].ColumnData.Keys;

            var r = 1;
            var cH = 1;

            sheet.Cells[r, cH].Value = "id";
            cH++;
            foreach (var key in uidkey1)
            {
                var cell = sheet.Cells[r, cH];
                cell.Value = key;

                var color = await connection.ExecuteScalarAsync<bool?>("SELECT isrequired FROM productattribute WHERE name = @key", new { key });

                if (key == "Brand Type" || key == "Currency" || key == "Item Type code" || key == "Loipl Shoot Required" || key == "Primary Packaging" || key == "Lead Time Days" || key == "Item Shape")
                {
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.Color.SetColor(System.Drawing.Color.White);
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    int darkerRed = (int)(System.Drawing.Color.Yellow.R * 0.8);
                    int darkerGreen = (int)(System.Drawing.Color.Yellow.G * 0.8);
                    int darkerBlue = (int)(System.Drawing.Color.Yellow.B * 0.8);
                    System.Drawing.Color darkerYellow = System.Drawing.Color.FromArgb(darkerRed, darkerGreen, darkerBlue);
                    cell.Style.Fill.BackgroundColor.SetColor(darkerYellow);
                }
                else if (color.HasValue && color.Value)
                {
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.Color.SetColor(System.Drawing.Color.White);
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LimeGreen);
                }
                else
                {
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.Color.SetColor(System.Drawing.Color.White);
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SteelBlue);
                }

                cH++;
            }

            var buyerTemplate1Columns = new List<(string column, int index)>
            {
                ("Brand Type", 6),
                ("Currency", 11),
                ("set___pcs", 39),
                ("Lead Time Days", 40)
            };

            var buyerTemplate2Columns = new List<(string column, int index)>
            {
                ("Item Shape", 20),
                ("Primary Packaging", 52),
                ("Loipl Shoot Required", 27),
                ("Item Type code", 44),
                ("finish", 47)
            };

            var buyerTemplate1 = new List<string> { "Brand Type", "Currency", "product_colour", "set___pcs", "Lead Time Days" };
            var buyerTemplate2 = new List<string> { "Primary Packaging", "Loipl Shoot Required", "Item Type code", "Item Shape", "finish" };
            r++;
            foreach (var record in records)
            {
                var c = 1;
                sheet.Cells[r, c].Value = record.Id.ToString();
                c++;

                int errorColumn = 1;
                sheet.Cells[1, errorColumn].Value = "Error Data";
                sheet.Cells[1, errorColumn].Style.Font.Bold = true;
                sheet.Cells[1, errorColumn].Style.Font.Color.SetColor(System.Drawing.Color.White);
                sheet.Cells[r, errorColumn].Style.Font.Color.SetColor(System.Drawing.Color.Red);
                sheet.Cells[1, errorColumn].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Cells[1, errorColumn].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SteelBlue);
                var errorMessages = new List<string>();
                bool hierarchyErrorsAdded = false;
                foreach (var key in uidkey1)
                {
                    var keyss = new List<string>(record.ColumnData.Keys);

                    // keyss.Sort();
                    sheet.Cells[r, c].Value = record.ColumnData[key];
                    if (record.ErrorData.ContainsKey(key))
                    {
                        // && record.ErrorData[key].Count > 0
                        errorMessages.AddRange(record.ErrorData[key]);
                        sheet.Cells[r, c].Style.Font.Color.SetColor(System.Drawing.Color.Red);
                    }

                    c++;
                }

                if (!hierarchyErrorsAdded && record.ErrorData.ContainsKey("Hierarchy") && record.ErrorData["Hierarchy"].Count > 0)
                {
                    errorMessages.AddRange(record.ErrorData["Hierarchy"]);
                    sheet.Cells[r, 1].Style.Font.Color.SetColor(System.Drawing.Color.Red);
                    hierarchyErrorsAdded = true;
                }

                for (int d = 0; d < 100; d++)
                {
                    int currentRow = r + d;

                    foreach (var (column, columnIndex) in buyerTemplate1Columns)
                    {
                        if (columnIndex != -1 && record.ColumnData.ContainsKey(column))
                        {
                            var cell = sheet.Cells[currentRow, columnIndex];
                            cell.Style.Font.Color.SetColor(System.Drawing.Color.Black);

                            if (column == "Currency" && columnDropdownValues.ContainsKey(column))
                            {
                                ApplyLargeCurrencyDropdown(sheet, currentRow, columnIndex, columnDropdownValues[column]);
                            }
                            else if (columnDropdownValues.ContainsKey(column))
                            {
                                ApplyDropdownValidation(sheet, currentRow, columnIndex, columnDropdownValues[column]);
                            }
                        }
                    }
                }

                for (int e = 0; e < 100; e++)
                {
                    int row = r + e;

                    foreach (var (column, columnIndex) in buyerTemplate2Columns)
                    {
                        if (columnIndex != -1 && record.ColumnData.ContainsKey(column))
                        {
                            var cell = sheet.Cells[row, columnIndex];
                            cell.Style.Font.Color.SetColor(System.Drawing.Color.Black);

                            if (columnDropdownValues.ContainsKey(column))
                            {
                                ApplyDropdownValidation(sheet, row, columnIndex, columnDropdownValues[column]);
                            }
                        }
                    }
                }

                if (errorMessages.Count > 0)
                {
                    string concatenatedErrors = string.Join(" | ", errorMessages);
                    sheet.Cells[r, errorColumn].Value = concatenatedErrors;
                    sheet.Cells[r, errorColumn].Style.Font.Color.SetColor(System.Drawing.Color.Red);
                }
                else
                {
                    sheet.Cells[r, errorColumn].Clear();
                }

                r++;
            }
        }

        if (sheet.Tables.Any())
        {
            var table = sheet.Tables[0];
            table.TableStyle = OfficeOpenXml.Table.TableStyles.Light9;
            table.ShowHeader = false;
            table.ShowFilter = false;
        }

        package.SaveAs(new FileInfo(tempFile));
        return tempFile;
    }

    public async Task<UploadedFile> GetFile(long uploadedFileId)
    {
        return await this.Get(uploadedFileId);
    }

    public async Task AddPicture(UploadPicture model)
    {
        int lastDotIndex = model.Name.LastIndexOf('.');
        var imageName = model.Name.ToLower();
        var uid = lastDotIndex >= 0
            ? model.Name.Substring(0, lastDotIndex)
            : model.Name;
        var uId = uid.ToLower();
        using var connection = this.GetConnection();
        var validationErrors = new List<string>();
        var productId = connection.QueryFirstOrDefault<int>("SELECT id FROM product WHERE lower(uid) = @uid", new { uid = uId });
        if (productId == 0)
        {
            throw new Exception($"Invalid Image file name.");
        }

        var existingRecord = connection.QueryFirstOrDefault<UploadPicture>("SELECT * FROM uploadpicture WHERE lower(Name) = @name", new { name = imageName });

        if (existingRecord != null)
        {
            connection.Execute("DELETE from uploadpicture  WHERE lower(Name) = @name", new { name = imageName });
            ////throw new Exception($"{model.Name} already exists.");
        }

        if (model.ImageUrl != null)
        {
            var queryResult = connection.Execute("INSERT INTO uploadpicture (Name, ImageUrl) SELECT @name, @imageurl WHERE NOT EXISTS (SELECT 1 FROM uploadpicture WHERE Name = @name)", new { name = model.Name, imageurl = model.ImageUrl });
            if (queryResult == null)
            {
                throw new Exception($"{model.Name} not Add.");
            }

            var existingId = connection.QueryFirstOrDefault<UploadPicture>("SELECT * FROM uploadpicture WHERE lower(Name) = @name", new { name = imageName });
            var queryResult1 = connection.Execute("update product set pictureid=@pictureid where lower(uid)=@uid", new { uid = uId, pictureid = existingId.Id });
        }
    }

    public async Task<List<UploadPicture>> GetFileNameAndUrl()
    {
        using var connection = this.GetConnection();
        var query = await connection.QueryAsync<UploadPicture>("select * from uploadpicture");
        return query.ToList();
    }

    public async Task<List<UploadPicture>> GetPSFileNameAndUrl()
    {
        using var connection = this.GetConnection();
        var query = await connection.QueryAsync<UploadPicture>("select * from photoshootuploadpicture");
        return query.ToList();
    }

    public async Task Updatestatus(List<Product> dataList, List<UploadedFileData> datas, Template template, UploadedFile file)
    {
        using var connection = this.GetConnection();
        if (!template.IsUidTemplate)
        {
            foreach (var list in dataList)
            {
                var producttemplateid = await connection.QueryFirstOrDefaultAsync<string>(
     $"SELECT TemplateId FROM product WHERE uid = '{list.Uid}'");

                string ids = string.Empty;

                if (producttemplateid == null)
                {
                    ids = file.TemplateId.ToString();
                }
                else
                {
                    if (!producttemplateid.Split(',').Contains(file.TemplateId.ToString()))
                    {
                        ids = producttemplateid + "," + file.TemplateId.ToString();
                    }
                }

                if (!string.IsNullOrEmpty(ids))
                {
                    await connection.ExecuteAsync($"UPDATE product SET templateid='{ids}' WHERE uid = '{list.Uid}'");
                }

                var query = $"SELECT p.uid, p.columndata " +
                              $"FROM product p " +
                              $"WHERE p.uid = '{list.Uid}' ";
                var products = await connection.QueryAsync<Product>(query);

                foreach (var productdetails in products)
                {
                    var marketplacetempquery = $"SELECT id FROM marketplacetemplate " +
                                           $"WHERE divisionid = {productdetails.ColumnData["Division Code"]} " +
                                           $"AND productdepartmentid = {productdetails.ColumnData["Dept Code"]}";
                    var marketplacetempIds = await connection.QueryAsync<int>(marketplacetempquery);
                    var marketplaceAttributeData = $"SELECT DISTINCT id, name " +
                                                    $"FROM productattribute " +
                                                    $"WHERE id IN (SELECT DISTINCT productattributeid FROM marketplacetemplateattribute " +
                                                    $"WHERE datatype IS NOT NULL AND marketplaceattribute IS NOT NULL " +
                                                    $"AND marketplacetemplateid = @Ids) " +
                                                    $"ORDER BY id ASC";
                    foreach (var id in marketplacetempIds)
                    {
                        var marketplaceAttributeNames = new List<string>();
                        var result = await connection.QueryAsync<ProductAttribute>(
                            marketplaceAttributeData, new { Ids = id });
                        marketplaceAttributeNames.AddRange(result.Select(attr => attr.Name));
                        var columnData = productdetails.ColumnData;
                        var keysToSearch = columnData.Keys.ToList();
                        var allKeysFound = marketplaceAttributeNames.All(attrName =>
                        {
                            if (!keysToSearch.Contains(attrName))
                            {
                                return false;
                            }

                            var value = columnData[attrName];
                            return !string.IsNullOrEmpty(value?.ToString());
                        });
                        long statusFilter = allKeysFound ? 1 : 0;
                        var itemcode = await connection.QueryFirstOrDefaultAsync<ItemCodeStaging>(
                            "SELECT * FROM itemcodestaging WHERE Uidcode = @Uid", new { Uid = productdetails.Uid });
                        if (itemcode != null)
                        {
                            await connection.ExecuteAsync(
                        $"UPDATE product SET statusfilter = '{statusFilter}', itemcodeid = {itemcode.Id} " +
                        $"WHERE uid = '{list.Uid}' ");
                        }
                        else
                        {
                            await connection.ExecuteAsync(
                        $"UPDATE product SET statusfilter = '{statusFilter}' " +
                        $"WHERE uid = '{list.Uid}' ");
                        }
                    }
                }
            }
        }
        else if (template.IsUidTemplate)
        {
            foreach (var list in dataList)
            {
                var producttemplateid = await connection.QueryFirstOrDefaultAsync<string>(
     $"SELECT TemplateId FROM product WHERE uid = '{list.Uid}'");

                string ids = string.Empty;

                if (producttemplateid == null)
                {
                    ids = file.TemplateId.ToString();
                }
                else
                {
                    if (!producttemplateid.Split(',').Contains(file.TemplateId.ToString()))
                    {
                        ids = producttemplateid + "," + file.TemplateId.ToString();
                    }
                }

                if (!string.IsNullOrEmpty(ids))
                {
                    await connection.ExecuteAsync($"UPDATE product SET templateid='{ids}' WHERE uid = '{list.Uid}'");
                }
            }
        }
    }

    public async Task UpdateInputTemplateData(long id, string? value)
    {
        try
        {
            using var connection = this.GetConnection();
            if (value != null)
            {
                var query = await connection.ExecuteAsync("update UploadedFile set templatefilestatus = @value where id=@id", new { id, value });
            }
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }

    public async Task<List<UploadedFile>> GetInputTemplateData(long id)
    {
        try
        {
            using var connection = this.GetConnection();
            var data = await connection.QueryAsync<UploadedFile>("select u.id , u.templatefilestatus as  TableName from  UploadedFile u where id=@id", new { id });
            return data.ToList();
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }

    // HouseHold Brand
    public async Task<List<ItemCodeCreation>> ItemCodeAttributeJsonExtraction()
    {
        try
        {
            var hireValidationAttributesJson = await _cache.FindAppSettings("ItemCodeCreationAttributesHB");
            var configJson = JArray.Parse(hireValidationAttributesJson.JsonValue);
            var listOfCodeJson = new List<ItemCodeCreation>();
            foreach (var item in configJson.Children<JObject>())
            {
                var itemCodeJson = new ItemCodeCreation();
                itemCodeJson.Attribute = item.GetValue("attribute").ToString();
                itemCodeJson.AttributeName = item.GetValue("attributeName").ToString();
                itemCodeJson.Color = item.GetValue("color").ToString();
                itemCodeJson.Value = item.GetValue("value").ToString();
                listOfCodeJson.Add(itemCodeJson);
            }

            return listOfCodeJson;
        }
        catch (Exception ex)
        {
            throw new Exception($"{ex.Message} in ItemCodeAttributeJsonExtraction method");
        }
    }

    // Added by Avinash HouseHold Private Domestic
    public async Task<List<ItemCodeCreation>> ItemCodeAttributeJsonExtractionHPD()
    {
        try
        {
            var hireValidationAttributesJson = await _cache.FindAppSettings("ItemCodeCreationAttributesHPD");
            var configJson = JArray.Parse(hireValidationAttributesJson.JsonValue);
            var listOfCodeJson = new List<ItemCodeCreation>();
            foreach (var item in configJson.Children<JObject>())
            {
                var itemCodeJson = new ItemCodeCreation();
                itemCodeJson.Attribute = item.GetValue("attribute").ToString();
                itemCodeJson.AttributeName = item.GetValue("attributeName").ToString();
                itemCodeJson.Color = item.GetValue("color").ToString();
                itemCodeJson.Value = item.GetValue("value").ToString();
                listOfCodeJson.Add(itemCodeJson);
            }

            return listOfCodeJson;
        }
        catch (Exception ex)
        {
            throw new Exception($"{ex.Message} in ItemCodeAttributeJsonExtractionHPD method");
        }
    }

    // HouseHold Private Import
    public async Task<List<ItemCodeCreation>> ItemCodeAttributeJsonExtractionHPI()
    {
        try
        {
            var hireValidationAttributesJson = await _cache.FindAppSettings("ItemCodeCreationAttributesHPI");
            var configJson = JArray.Parse(hireValidationAttributesJson.JsonValue);
            var listOfCodeJson = new List<ItemCodeCreation>();
            foreach (var item in configJson.Children<JObject>())
            {
                var itemCodeJson = new ItemCodeCreation();
                itemCodeJson.Attribute = item.GetValue("attribute").ToString();
                itemCodeJson.AttributeName = item.GetValue("attributeName").ToString();
                itemCodeJson.Color = item.GetValue("color").ToString();
                itemCodeJson.Value = item.GetValue("value").ToString();
                listOfCodeJson.Add(itemCodeJson);
            }

            return listOfCodeJson;
        }
        catch (Exception ex)
        {
            throw new Exception($"{ex.Message} in ItemCodeAttributeJsonExtractionHPI method");
        }
    }

    // Furniture Private Domestic
    public async Task<List<ItemCodeCreation>> ItemCodeAttributeJsonExtractionFPD()
    {
        try
        {
            var hireValidationAttributesJson = await _cache.FindAppSettings("ItemCodeCreationAttributesFPD");
            var configJson = JArray.Parse(hireValidationAttributesJson.JsonValue);
            var listOfCodeJson = new List<ItemCodeCreation>();
            foreach (var item in configJson.Children<JObject>())
            {
                var itemCodeJson = new ItemCodeCreation();
                itemCodeJson.Attribute = item.GetValue("attribute").ToString();
                itemCodeJson.AttributeName = item.GetValue("attributeName").ToString();
                itemCodeJson.Color = item.GetValue("color").ToString();
                itemCodeJson.Value = item.GetValue("value").ToString();
                listOfCodeJson.Add(itemCodeJson);
            }

            return listOfCodeJson;
        }
        catch (Exception ex)
        {
            throw new Exception($"{ex.Message} in ItemCodeAttributeJsonExtractionFPD method");
        }
    }

    // Furniture Private Import
    public async Task<List<ItemCodeCreation>> ItemCodeAttributeJsonExtractionFPI()
    {
        try
        {
            var hireValidationAttributesJson = await _cache.FindAppSettings("ItemCodeCreationAttributesFPI");
            var configJson = JArray.Parse(hireValidationAttributesJson.JsonValue);
            var listOfCodeJson = new List<ItemCodeCreation>();
            foreach (var item in configJson.Children<JObject>())
            {
                var itemCodeJson = new ItemCodeCreation();
                itemCodeJson.Attribute = item.GetValue("attribute").ToString();
                itemCodeJson.AttributeName = item.GetValue("attributeName").ToString();
                itemCodeJson.Color = item.GetValue("color").ToString();
                itemCodeJson.Value = item.GetValue("value").ToString();
                listOfCodeJson.Add(itemCodeJson);
            }

            return listOfCodeJson;
        }
        catch (Exception ex)
        {
            throw new Exception($"{ex.Message} in ItemCodeAttributeJsonExtractionFPI method");
        }
    }

    public async Task Deleteproductimage(long id)
    {
        using var connection = this.GetConnection();
        string query = $"Delete from uploadpicture where id = '{id}'";
        await connection.ExecuteAsync(query);
    }

    private async Task ApplyDropdownValidation(ExcelWorksheet sheet, int row, int columnIndex, IEnumerable<string> dropdownValues)
    {
        if (columnIndex != -1)
        {
            var dropdownRange = $"{sheet.Cells[row, columnIndex].Address}:{sheet.Cells[sheet.Dimension.End.Row, columnIndex].Address}";
            var dataValidation = sheet.DataValidations.AddListValidation(dropdownRange);
            dataValidation.ErrorTitle = "Invalid Value";
            dataValidation.Error = "Please enter a valid value from the dropdown list";
            dataValidation.ShowErrorMessage = true;
            dataValidation.ErrorStyle = ExcelDataValidationWarningStyle.stop;
            dataValidation.Formula.ExcelFormula = $"\"{string.Join(",", dropdownValues)}\"";
        }
    }

    private async Task ApplyLargeCurrencyDropdown(ExcelWorksheet sheet, int currentRow, int columnIndex, List<string> values)
    {
        var hiddenWorksheet = sheet.Workbook.Worksheets.Add("HiddenSheet");
        hiddenWorksheet.Hidden = eWorkSheetHidden.VeryHidden;

        for (int i = 0; i < values.Count; i++)
        {
            hiddenWorksheet.Cells[i + 2, 1].Value = values[i];
        }

        var namedRange = sheet.Names.Add("CurrencyList", hiddenWorksheet.Cells[2, 1, values.Count + 1, 1]);

        var cell = sheet.Cells[currentRow, columnIndex];
        var cellAddress = cell.Address;
        var validation = sheet.DataValidations.AddListValidation(cellAddress);
        validation.Formula.ExcelFormula = "CurrencyList";
        validation.ShowErrorMessage = true;
        validation.ErrorTitle = "Invalid Value";
        validation.Error = "Please enter a valid value from the dropdown list";
    }

    private async Task<string> FetchValueFromProductTable(string columnName, string uid)
    {
        using var connection = this.GetConnection();
        string query = $"SELECT columndata->>'{columnName}' FROM product WHERE uid = @uid";
        return await connection.QueryFirstOrDefaultAsync<string>(query, new { uid });
    }
}