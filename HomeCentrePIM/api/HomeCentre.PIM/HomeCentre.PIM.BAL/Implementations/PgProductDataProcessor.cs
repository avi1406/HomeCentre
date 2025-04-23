using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Office.CoverPageProps;
using DocumentFormat.OpenXml.Office2021.Excel.RichDataWebImage;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentMigrator.Infrastructure;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.Master;
using HomeCentre.PIM.DAL.Staging;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.Formula.Functions;
using Org.BouncyCastle.Asn1.X509;
using static NPOI.SS.Formula.PTG.ArrayPtg;

namespace HomeCentre.PIM.BAL.Implementations;

public class PgProductDataProcessor : PgEntityAction<Product>, IDataProcessor
{
    private readonly IUserContext _userContext;
    private readonly IDbCache _cache;
    private bool isCombination;
    private IDbConnection _connection;

    public PgProductDataProcessor(IUserContext userContext, IDbConnectionFactory connectionFactory,
        IConfiguration configuration,
        IDbCache cache)
        : base(userContext, connectionFactory, configuration)
    {
        _userContext = userContext;
        _cache = cache;
        _connection = this.GetConnection();
    }

    public async Task<List<UploadedFileData>> ValidateData(List<UploadedFileData> data, long templateId)
    {
        try
        {
            isCombination = false;
            using var connection = _connection;
            var template = (await _cache.GetTemplates()).FirstOrDefault(x => x.Id == templateId);
            var templateColumns = (await _cache.GetTemplateAttributes(templateId)).Select(x => x.AttributeId).ToList();
            var validColumns = (await _cache.GetAttributes(template.DivisionId.GetValueOrDefault()))
                .Where(x => templateColumns.Contains(x.Id)).ToList();
            var validUids = await _cache.GetUids();

            // var hMasters = await this._cache.GetMaster<HierarchicalMaster>();
            // var nameIdList = await connection.QueryAsync<NameIdCollection>("select * from vw_nameidcollection");
            if (!template.IsUidTemplate)
            {
                var query = data.Select(x => x.ColumnData["Uid"]?.ToString()).GroupBy(x => x)
                    .Where(g => g.Count() > 1)
                    .Select(y => y.Key)
                    .ToList();
                if (query.Any())
                {
                    throw new DuplicateNameException($"({string.Join(",", query)}) uids are duplicated");
                }
            }

            var results = await connection.QueryAsync<Product>("SELECT itemcodeid, uid, id, pictureid, columndata, isactive FROM product");
            var columndata = await connection.QueryAsync<AllAttributesData>("select * from vw_columndata where uid IS NOT NULL ORDER BY datecreated DESC");
            var mtrdivision = await connection.QueryAsync<MasterDivision>("select name,code,divisionid from masterdivision");
            var appsettings = await connection.QueryAsync<dynamic>("select jsonvalue from appsettings where name = 'MasterDivision'");
            var hierarchy = await connection.QueryAsync<Merchandise_hierarchy>("select distinct dept,class,subclass,division,group_no from merchandise_hierarchy");
            var typehierarchy = await connection.QueryAsync<MTRproductype_subtype>("select distinct dept_code,div_code,class_code,subclass_code,producttype1,producttype2,productsubtype1,productsubtype2 from mtrproductype_subtype");
            var season = await connection.QueryAsync<Season>("select code,divisionid from season");
            var priceGrouping = await connection.QueryAsync<PriceGrouping>("select code,divisionid from pricegrouping");
            var productPurpose = await connection.QueryAsync<ProductPurpose>("select code,divisionid from productpurpose");
            var planoOrFamilyNames = await connection.QueryAsync<UidInitiation>("SELECT planocode, status, planogram, familyname, divisionid FROM uidinitiation");
            var picture = await connection.QueryAsync<UploadPicture>("SELECT * FROM uploadpicture");

            var productType1 = await connection.QueryAsync<MtrProducttype1>("select name from mtrproducttype1");
            var productType2 = await connection.QueryAsync<MtrProducttype2>("select name from mtrproducttype2");
            var productSubType1 = await connection.QueryAsync<MtrProductSubType1>("select name from mtrproductsubtype1");
            var produSubType2 = await connection.QueryAsync<MtrProductSubType2>("select name from mtrproductsubtype2");
            var hsnCode = await connection.QueryAsync<HsnCodeModel>("select code from mtrhsncode");

            var purchasetype = await connection.QueryAsync<Purchasetype>("select code,divisionid from purchasetype");
            var productClassification = await connection.QueryAsync<ProductClassificationCodeData>("select code,divisionid from mtrproductclassificationcode");
            var plcCode = await connection.QueryAsync<PLCCodeData>("select code,divisionid from mtrplccode");
            var productGrouping = await connection.QueryAsync<ProductGroupingData>("select code,divisionid from mtrproductgrouping");
            var packagingtype = await connection.QueryAsync<PackagingTypeData>("select code as PackagingTypeCode,divisionid from mtrpackagingtype");
            var countryOfOrigin = await connection.QueryAsync<CountryOfOrigin>("select name from mtrcountryoforigin");
            var taxCategory = await connection.QueryAsync<TaxCategoryData>("select code from mtrtaxcategory");
            var innerCarton = await connection.QueryAsync<InnerCartonCodeData>("select code,divisionid from mtrinnercartoncode");
            var masterCarton = await connection.QueryAsync<MasterCartonCodeData>("select code,divisionid from mtrmastercartoncode");
            var madeOfValue = await connection.QueryAsync<MadeOfValueData>("select code from mtrmadeofvalue");
            var supplierCode = await connection.QueryAsync<SupplierCodeData>("select code,divisionid from mtrsuppliercode");
            var diffgroup = await connection.QueryAsync<DiffGroupData>("select code from mtrdiffgroup2val");
            var color = await connection.QueryAsync<MtrProductColour>("select code,divisionid from mtrproductcolour");
            var brand = await connection.QueryAsync<Brand>("select brandcode from brand");

            var setpcs = await connection.QueryAsync<MtrSetpcs>("select code from mtrsetpcs");
            var storeCategory = await connection.QueryAsync<PrimarySecondarySKUData>("select code from mtrprimarysecondarysku");

            var mtrcurrency = await connection.QueryAsync<MtrCurrency>("select name from mtrcurrency");
            var mtrrmsseasoncode = await connection.QueryAsync<MtrRMSSeasonCode>("select name,code from mtrrmsseasoncode");
            var mtrleadtime = await connection.QueryAsync<MtrLeadTime>("select name from mtrleadtime");
            var mtritemtypecode = await connection.QueryAsync<MtrItemTypeCode>("select code,divisionid from mtritemtypecode");
            var mtritemShape = await connection.QueryAsync<MtrItemShape>("select name from mtritemshape");
            var mtrbrandType = await connection.QueryAsync<MtrBrandType>("select name from mtrbrandtype");

            var templateDivisionId = template.DivisionId.ToString();

            var uidList = data.Select(x => x.ColumnData.ContainsKey("Uid") ? x.ColumnData["Uid"]?.ToString() : string.Empty).Where(x => !string.IsNullOrEmpty(x)).ToList();
            if (uidList.Count == 0)
            {
                if (template.IsUidTemplate)
                {
                    var deptList = data.Select(x => x.ColumnData.ContainsKey("Dept Code") ? x.ColumnData["Dept Code"]?.ToString() : string.Empty).ToList();
                    var duplicateClassList = data.Select(x => x.ColumnData.ContainsKey("Class Code") ? x.ColumnData["Class Code"]?.ToString() : string.Empty).ToList();
                    var duplicatesubClassList = data.Select(x => x.ColumnData.ContainsKey("Subclass Code") ? x.ColumnData["Subclass Code"]?.ToString() : string.Empty).ToList();
                    var planoList = data.Select(x => x.ColumnData.ContainsKey("Planogram/Family Code") ? x.ColumnData["Planogram/Family Code"]?.ToString() : string.Empty).ToList();
                    var duplicateGroupList = data.Select(x => x.ColumnData.ContainsKey("Group Code") ? x.ColumnData["Group Code"]?.ToString() : string.Empty).ToList();
                    var productDescList = data.Select(x => x.ColumnData.ContainsKey("Product Desc") ? x.ColumnData["Product Desc"]?.ToString() : string.Empty).ToList();

                    var combinations = new List<string>();
                    var duplicateCombinations = new HashSet<string>();

                    var combinationString = string.Join(",", combinations);

                    var batchSize = 200;
                    var errorKeys = new List<string> { "Dept Code", "Class Code", "Subclass Code", "Planogram/Family Code", "Group Code", "Product Desc" };

                    for (int i = 0; i < data.Count; i += batchSize)
                    {
                        var batch = new List<Dictionary<string, string>>();

                        Parallel.For(i, Math.Min(i + batchSize, data.Count), j =>
                        {
                            var parameters = new Dictionary<string, string>
        {
            { "Dept Code", deptList[j] },
            { "Class Code", duplicateClassList[j] },
            { "Subclass Code", duplicatesubClassList[j] },
            { "Planogram/Family Code", planoList[j] },
            { "Group Code", duplicateGroupList[j] },
            { "Product Desc", productDescList[j] }
        };

                            lock (batch)
                            {
                                batch.Add(parameters);
                            }
                        });

                        var parametersString = JsonConvert.SerializeObject(batch);

                        // Execute the query for the current batch
                        var result_val = await connection.QueryAsync(
                            "SELECT * FROM public.fn_insertbulkrecords(@parameters);",
                            new { parameters = parametersString });

                        // Process the result of the current batch
                        Parallel.ForEach(batch, (param, state, index) =>
                        {
                            var combination = $"{param["Planogram/Family Code"]}-{param["Group Code"]}-{param["Dept Code"]}-{param["Class Code"]}-{param["Subclass Code"]}-{param["Product Desc"]}";

                            var currentIndex = i + (int)index;
                            var currentItem = data[currentIndex];

                            if (result_val.Any(r =>
                                string.Equals(Convert.ToString(r.dept_code), param["Dept Code"], StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(Convert.ToString(r.class_code), param["Class Code"], StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(Convert.ToString(r.subclass_code), param["Subclass Code"], StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(Convert.ToString(r.planogram_family_code), param["Planogram/Family Code"], StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(Convert.ToString(r.group_code), param["Group Code"], StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(Convert.ToString(r.product_desc), param["Product Desc"], StringComparison.OrdinalIgnoreCase)))
                            {
                                lock (currentItem.ErrorData)
                                {
                                    foreach (var key in errorKeys)
                                    {
                                        if (!currentItem.ErrorData.ContainsKey(key))
                                        {
                                            currentItem.ErrorData[key] = new List<string>();
                                        }
                                    }

                                    foreach (var key in errorKeys)
                                    {
                                        if (!currentItem.ErrorData[key].Any())
                                        {
                                            currentItem.ErrorData[key].Add($"Combination {combination} already exists.");
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                lock (duplicateCombinations)
                                {
                                    if (!duplicateCombinations.Add(combination))
                                    {
                                        lock (currentItem.ErrorData)
                                        {
                                            foreach (var key in errorKeys)
                                            {
                                                if (!currentItem.ErrorData.ContainsKey(key))
                                                {
                                                    currentItem.ErrorData[key] = new List<string>();
                                                }
                                            }

                                            bool anyKeyAdded = errorKeys.Any(key => !currentItem.ErrorData[key].Any());

                                            if (anyKeyAdded)
                                            {
                                                currentItem.ErrorData[errorKeys.First()].Add($"Combination {combination} is duplicated");
                                            }
                                        }
                                    }
                                }
                            }
                        });
                    }
                }
            }

            if (template.IsUidTemplate)
            {
                var seasonList = data.Select(x => x.ColumnData.ContainsKey("Season Code") ? x.ColumnData["Season Code"]?.ToString() : string.Empty).ToList();
                if (seasonList.Any())
                {
                    Parallel.ForEach(data, item =>
                    {
                        if (item.ColumnData.ContainsKey("Season Code"))
                        {
                            var seasonCode = item.ColumnData["Season Code"];

                            if (string.IsNullOrWhiteSpace(seasonCode.ToString()))
                            {
                                if (!item.ErrorData.ContainsKey("Season Code"))
                                {
                                    item.ErrorData.Add("Season Code", new List<string>());
                                }

                                item.ErrorData["Season Code"].Add("Season Code Field is required");
                            }
                            else
                            {
                                if (seasonCode is string)
                                {
                                    if (!item.ErrorData.ContainsKey("Season Code"))
                                    {
                                        item.ErrorData.Add("Season Code", new List<string>());
                                    }

                                    item.ErrorData["Season Code"].Add($"Special characters are present in Season Code '{seasonCode}'");
                                }
                                else
                                {
                                    var seasonCheck = season.Where(a => a.Code == seasonCode.ToString()).ToList();

                                    if (!seasonCheck.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("Season Code"))
                                        {
                                            item.ErrorData.Add("Season Code", new List<string>());
                                        }

                                        item.ErrorData["Season Code"].Add($"Invalid Season Code '{seasonCode}'");
                                    }
                                    else
                                    {
                                        var mtrseason = season.Where(a => a.Code == seasonCode.ToString() && a.DivisionId.Split(',').Select(d => d.Trim()).Contains(templateDivisionId)).ToList();
                                        if (!mtrseason.Any())
                                        {
                                            if (!item.ErrorData.ContainsKey("Season Code"))
                                            {
                                                item.ErrorData.Add("Season Code", new List<string>());
                                            }

                                            item.ErrorData["Season Code"].Add($"Division Mismatched for Season Code {seasonCode}");
                                        }
                                    }
                                }
                            }
                        }
                    });
                }

                var priceList = data.Select(x => x.ColumnData.ContainsKey("Price Grouping") ? x.ColumnData["Price Grouping"]?.ToString() : string.Empty).ToList();
                if (priceList.Any())
                {
                    Parallel.ForEach(data, item =>
                    {
                        if (item.ColumnData.ContainsKey("Price Grouping"))
                        {
                            var priceCode = item.ColumnData["Price Grouping"];

                            if (string.IsNullOrWhiteSpace(priceCode.ToString()))
                            {
                                if (!item.ErrorData.ContainsKey("Price Grouping"))
                                {
                                    item.ErrorData.Add("Price Grouping", new List<string>());
                                }

                                item.ErrorData["Price Grouping"].Add("Price Grouping Field is required");
                            }
                            else
                            {
                                if (priceCode is string)
                                {
                                    if (!item.ErrorData.ContainsKey("Price Grouping"))
                                    {
                                        item.ErrorData.Add("Price Grouping", new List<string>());
                                    }

                                    item.ErrorData["Price Grouping"].Add($"Special characters are present in Price Grouping '{priceCode}'");
                                }
                                else
                                {
                                    var priceCheck = priceGrouping.Where(a => a.Code == priceCode.ToString()).ToList();

                                    if (!priceCheck.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("Price Grouping"))
                                        {
                                            item.ErrorData.Add("Price Grouping", new List<string>());
                                        }

                                        item.ErrorData["Price Grouping"].Add($"Invalid Price Grouping '{priceCode}'");
                                    }
                                    else
                                    {
                                        var mtrpricegrouping = priceGrouping.Where(a => a.Code.ToString() == priceCode.ToString() && a.DivisionId.Split(',').Select(d => d.Trim()).Contains(templateDivisionId)).ToList();
                                        if (!mtrpricegrouping.Any())
                                        {
                                            if (!item.ErrorData.ContainsKey("Price Grouping"))
                                            {
                                                item.ErrorData.Add("Price Grouping", new List<string>());
                                            }

                                            item.ErrorData["Price Grouping"].Add($"Division Mismatched for Price Grouping {priceCode}");
                                        }
                                    }
                                }
                            }
                        }
                    });
                }

                var purposeList = data.Select(x => x.ColumnData.ContainsKey("Product Purpose") ? x.ColumnData["Product Purpose"]?.ToString() : string.Empty).ToList();
                if (purposeList.Any())
                {
                    Parallel.ForEach(data, item =>
                    {
                        if (item.ColumnData.ContainsKey("Product Purpose"))
                        {
                            var productCode = item.ColumnData["Product Purpose"];
                            if (string.IsNullOrWhiteSpace(productCode.ToString()))
                            {
                                if (!item.ErrorData.ContainsKey("Product Purpose"))
                                {
                                    item.ErrorData.Add("Product Purpose", new List<string>());
                                }

                                item.ErrorData["Product Purpose"].Add("Product Purpose Field is required");
                            }
                            else
                            {
                                if (productCode is string)
                                {
                                    if (!item.ErrorData.ContainsKey("Product Purpose"))
                                    {
                                        item.ErrorData.Add("Product Purpose", new List<string>());
                                    }

                                    item.ErrorData["Product Purpose"].Add($"Special characters are present in Product Purpose '{productCode}'");
                                }
                                else
                                {
                                    var productCheck = productPurpose.Where(a => a.Code == productCode.ToString()).ToList();

                                    if (!productCheck.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("Product Purpose"))
                                        {
                                            item.ErrorData.Add("Product Purpose", new List<string>());
                                        }

                                        item.ErrorData["Product Purpose"].Add($"Invalid Product Purpose '{productCode}'");
                                    }
                                    else
                                    {
                                        var mtrproductpurpose = productPurpose.Where(a => a.Code == productCode.ToString() && a.DivisionId.Split(',').Select(d => d.Trim()).Contains(templateDivisionId)).ToList();
                                        if (!mtrproductpurpose.Any())
                                        {
                                            if (!item.ErrorData.ContainsKey("Product Purpose"))
                                            {
                                                item.ErrorData.Add("Product Purpose", new List<string>());
                                            }

                                            item.ErrorData["Product Purpose"].Add($"Division Mismatched for Product Purpose {productCode}");
                                        }
                                    }
                                }
                            }
                        }
                    });
                }

                Parallel.ForEach(data, item =>
                {
                    if (item.ColumnData.ContainsKey("Target MRP"))
                    {
                        var target = item.ColumnData["Target MRP"];

                        if (target != null && decimal.TryParse(target.ToString(), out decimal targetValue))
                        {
                            if (targetValue <= 0 || targetValue % 1 != 0)
                            {
                                if (!item.ErrorData.ContainsKey("Target MRP"))
                                {
                                    item.ErrorData.Add("Target MRP", new List<string>());
                                }

                                item.ErrorData["Target MRP"].Add("Target MRP Field is expected to be a positive integer");
                            }
                        }

                        if (string.IsNullOrWhiteSpace(target.ToString()))
                        {
                            if (!item.ErrorData.ContainsKey("Target MRP"))
                            {
                                item.ErrorData.Add("Target MRP", new List<string>());
                            }

                            item.ErrorData["Target MRP"].Add("Target MRP Field is Required");
                        }
                        else if (!decimal.TryParse(target.ToString(), out _))
                        {
                            if (!item.ErrorData.ContainsKey("Target MRP"))
                            {
                                item.ErrorData.Add("Target MRP", new List<string>());
                            }

                            item.ErrorData["Target MRP"].Add("Target MRP Field is expected to be a number");
                        }
                    }

                    if (item.ColumnData.ContainsKey("Product Desc"))
                    {
                        var productDesc = item.ColumnData["Product Desc"].ToString();
                        if (string.IsNullOrWhiteSpace(productDesc))
                        {
                            if (!item.ErrorData.ContainsKey("Product Desc"))
                            {
                                item.ErrorData.Add("Product Desc", new List<string>());
                            }

                            item.ErrorData["Product Desc"].Add("Product Desc Field is Required");
                        }
                    }
                });

                var planoList = data.Select(x => x.ColumnData.ContainsKey("Planogram/Family Code") ? x.ColumnData["Planogram/Family Code"]?.ToString() : string.Empty).ToList();
                if (planoList.Any())
                {
                    Parallel.ForEach(data, item =>
                    {
                        if (templateDivisionId == "61")
                        {
                            if (item.ColumnData.ContainsKey("Planogram/Family Code"))
                            {
                                var planocode = item.ColumnData["Planogram/Family Code"];

                                // select divisionid from uidinitiation where planocode = '{planocode}'
                                var matchingDivisionId = planoOrFamilyNames.Where(a => a.PlanoCode == planocode.ToString()).Select(a => a.DivisionId).FirstOrDefault();

                                if (string.IsNullOrWhiteSpace(planocode.ToString()))
                                {
                                    if (!item.ErrorData.ContainsKey("Planogram/Family Code"))
                                    {
                                        item.ErrorData.Add("Planogram/Family Code", new List<string>());
                                    }

                                    item.ErrorData["Planogram/Family Code"].Add(" Planogram/Family Code Field is required");
                                }
                                else
                                {
                                    if (!int.TryParse(planocode.ToString(), out _))
                                    {
                                        if (!item.ErrorData.ContainsKey("Planogram/Family Code"))
                                        {
                                            item.ErrorData.Add("Planogram/Family Code", new List<string>());
                                        }

                                        item.ErrorData["Planogram/Family Code"].Add(" Planogram/Family Code Field is expected to be a number");
                                    }
                                    else if (matchingDivisionId.ToString() != templateDivisionId)
                                    {
                                        if (!item.ErrorData.ContainsKey("Planogram/Family Code"))
                                        {
                                            item.ErrorData.Add("Planogram/Family Code", new List<string>());
                                        }

                                        item.ErrorData["Planogram/Family Code"].Add($"Invalid Planogram/Family Code '{planocode}'");
                                    }
                                    else
                                    {
                                        if (planocode is string)
                                        {
                                            if (!item.ErrorData.ContainsKey("Planogram/Family Code"))
                                            {
                                                item.ErrorData.Add("Planogram/Family Code", new List<string>());
                                            }

                                            item.ErrorData["Planogram/Family Code"].Add($"Special characters are present in Planogram/Family Code '{planocode}'");
                                        }
                                        else
                                        {
                                            var planoOrFamilyName = planoOrFamilyNames
                                           .Where(a => a.PlanoCode == planocode.ToString() && a.Status == "Approved")
                                           .ToList();

                                            if (!planoOrFamilyName.Any())
                                            {
                                                if (!item.ErrorData.ContainsKey("Planogram/Family Code"))
                                                {
                                                    item.ErrorData.Add("Planogram/Family Code", new List<string>());
                                                }

                                                item.ErrorData["Planogram/Family Code"].Add($"Invalid Planogram/Family Code '{planocode}'");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else if (templateDivisionId == "4" || templateDivisionId == "65")
                        {
                            if (item.ColumnData.ContainsKey("Planogram/Family Code"))
                            {
                                var planocode = item.ColumnData["Planogram/Family Code"];

                                // select divisionid from uidinitiation where planocode = '{planocode}'
                                var matchingDivisionId = planoOrFamilyNames.Where(a => a.PlanoCode == planocode.ToString()).Select(a => a.DivisionId).FirstOrDefault();

                                if (string.IsNullOrWhiteSpace(planocode.ToString()))
                                {
                                    if (!item.ErrorData.ContainsKey("Planogram/Family Code"))
                                    {
                                        item.ErrorData.Add("Planogram/Family Code", new List<string>());
                                    }

                                    item.ErrorData["Planogram/Family Code"].Add(" Planogram/Family Code Field is required");
                                }
                                else
                                {
                                    if (!int.TryParse(planocode.ToString(), out _))
                                    {
                                        if (!item.ErrorData.ContainsKey("Planogram/Family Code"))
                                        {
                                            item.ErrorData.Add("Planogram/Family Code", new List<string>());
                                        }

                                        item.ErrorData["Planogram/Family Code"].Add(" Planogram/Family Code Field is expected to be a number");
                                    }
                                    else if (matchingDivisionId.ToString() != templateDivisionId)
                                    {
                                        if (!item.ErrorData.ContainsKey("Planogram/Family Code"))
                                        {
                                            item.ErrorData.Add("Planogram/Family Code", new List<string>());
                                        }

                                        item.ErrorData["Planogram/Family Code"].Add($"Invalid Planogram/Family Code '{planocode}'");
                                    }
                                    else
                                    {
                                        if (planocode is string)
                                        {
                                            if (!item.ErrorData.ContainsKey("Planogram/Family Code"))
                                            {
                                                item.ErrorData.Add("Planogram/Family Code", new List<string>());
                                            }

                                            item.ErrorData["Planogram/Family Code"].Add($"Special characters are present in Planogram/Family Code '{planocode}'");
                                        }
                                        else
                                        {
                                            var planoOrFamilyName = planoOrFamilyNames
                                           .Where(a => a.PlanoCode == planocode.ToString() && a.Status == "Approved")
                                           .ToList();

                                            if (!planoOrFamilyName.Any())
                                            {
                                                if (!item.ErrorData.ContainsKey("Planogram/Family Code"))
                                                {
                                                    item.ErrorData.Add("Planogram/Family Code", new List<string>());
                                                }

                                                item.ErrorData["Planogram/Family Code"].Add($"Invalid Planogram/Family Code '{planocode}'");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    });
                }

                if (templateDivisionId == "65")
                {
                    Parallel.ForEach(data, item =>
                    {
                        var uids = item.ColumnData["Uid"].ToString();
                        var count1 = item?.ColumnData.Count();
                        var div = item.ColumnData["Division Code"];
                        var divError = item.ErrorData.ContainsKey("Division Code");
                        var group = item.ColumnData["Group Code"];
                        var groupError = item.ErrorData.ContainsKey("Group Code");
                        var deptCode = item.ColumnData["Dept Code"];
                        var classCode = item.ColumnData["Class Code"];
                        var subClassCode = item.ColumnData["Subclass Code"];

                        if (string.IsNullOrEmpty(div.ToString()))
                        {
                            if (!divError)
                            {
                                item.ErrorData.Add("Division Code", new List<string>());
                            }

                            item.ErrorData["Division Code"].Add("Division Code Field is required");
                        }
                        else if (div is string)
                        {
                            if (!divError)
                            {
                                item.ErrorData.Add("Division Code", new List<string>());
                            }

                            item.ErrorData["Division Code"].Add($"Special characters are present in Division Code '{div}'.");
                        }
                        else if (!string.IsNullOrEmpty(div.ToString()) && div.ToString() != templateDivisionId)
                        {
                            if (!divError)
                            {
                                item.ErrorData.Add("Division Code", new List<string>());
                            }

                            item.ErrorData["Division Code"].Add($"Template div ({templateDivisionId}) does not match with record div_code ({div}).");
                        }

                        if (string.IsNullOrEmpty(group.ToString()))
                        {
                            if (!groupError)
                            {
                                item.ErrorData.Add("Group Code", new List<string>());
                            }

                            item.ErrorData["Group Code"].Add("Group Code Field is required");
                        }
                        else if (group is string)
                        {
                            if (!groupError)
                            {
                                item.ErrorData.Add("Group Code", new List<string>());
                            }

                            item.ErrorData["Group Code"].Add($"Special characters are present in Group Code '{group}'.");
                        }
                        else
                        {
                            long divisionCodeValue;
                            long groupCodeValue;
                            if (item.ColumnData["Division Code"] != null && long.TryParse(item.ColumnData["Division Code"].ToString(), out divisionCodeValue) &&
                                item.ColumnData["Group Code"] != null && long.TryParse(item.ColumnData["Group Code"].ToString(), out groupCodeValue))
                            {
                                var matchingHierarchyinvalied = hierarchy.Where(a => a.Group_no != null && (long)a.Group_no == groupCodeValue);
                                if (!matchingHierarchyinvalied.Any())
                                {
                                    if (!item.ErrorData.ContainsKey("Group Code"))
                                    {
                                        item.ErrorData.Add("Group Code", new List<string>());
                                        item.ErrorData["Group Code"].Add($"Invalid Group Code {group}");
                                    }
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(uids) || count1 == 12)
                        {
                            if (item.ColumnData.ContainsKey("Dept Code"))
                            {
                                if (string.IsNullOrWhiteSpace(deptCode.ToString()))
                                {
                                    if (!item.ErrorData.ContainsKey("Dept Code"))
                                    {
                                        item.ErrorData.Add("Dept Code", new List<string>());
                                    }

                                    item.ErrorData["Dept Code"].Add("Dept Code Field is required");
                                }
                                else
                                {
                                    if (deptCode is string)
                                    {
                                        if (!item.ErrorData.ContainsKey("Dept Code"))
                                        {
                                            item.ErrorData.Add("Dept Code", new List<string>());
                                        }

                                        item.ErrorData["Dept Code"].Add($"Special characters are present in Dept Code '{deptCode}'");
                                    }
                                    else
                                    {
                                        long divisionCodeValue;
                                        long groupCodeValue;
                                        long deptCodeValue;
                                        if (item.ColumnData["Division Code"] != null && long.TryParse(item.ColumnData["Division Code"].ToString(), out divisionCodeValue) &&
                                            item.ColumnData["Dept Code"] != null && long.TryParse(item.ColumnData["Dept Code"].ToString(), out deptCodeValue) &&
                                            item.ColumnData["Group Code"] != null && long.TryParse(item.ColumnData["Group Code"].ToString(), out groupCodeValue))
                                        {
                                            var matchingHierarchyinvalied = hierarchy.Where(a => a.Dept != null && (long)a.Dept == deptCodeValue);
                                            if (!matchingHierarchyinvalied.Any())
                                            {
                                                if (!item.ErrorData.ContainsKey("Dept Code"))
                                                {
                                                    item.ErrorData.Add("Dept Code", new List<string>());
                                                    item.ErrorData["Dept Code"].Add($"Invalid Dept Code {deptCode}");
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (item.ColumnData.ContainsKey("Class Code"))
                            {
                                if (string.IsNullOrWhiteSpace(classCode.ToString()))
                                {
                                    if (!item.ErrorData.ContainsKey("Class Code"))
                                    {
                                        item.ErrorData.Add("Class Code", new List<string>());
                                    }

                                    item.ErrorData["Class Code"].Add("Class Code Field is required");
                                }
                                else
                                {
                                    if (classCode is string)
                                    {
                                        if (!item.ErrorData.ContainsKey("Class Code"))
                                        {
                                            item.ErrorData.Add("Class Code", new List<string>());
                                        }

                                        item.ErrorData["Class Code"].Add($"Special characters are present in Class Code '{classCode}'");
                                    }
                                    else
                                    {
                                        long deptCodeValue;
                                        long groupCodeValue;
                                        long classCodeValue;
                                        long divisionCodeValue;
                                        if (item.ColumnData["Dept Code"] != null && long.TryParse(item.ColumnData["Dept Code"].ToString(), out deptCodeValue) &&
                                            item.ColumnData["Class Code"] != null && long.TryParse(item.ColumnData["Class Code"].ToString(), out classCodeValue) &&
                                            item.ColumnData["Division Code"] != null && long.TryParse(item.ColumnData["Division Code"].ToString(), out divisionCodeValue) &&
                                            item.ColumnData["Group Code"] != null && long.TryParse(item.ColumnData["Group Code"].ToString(), out groupCodeValue))
                                        {
                                            var matchingHierarchyinvalied = hierarchy.Where(a => a.Class != null && (long)a.Class == classCodeValue);
                                            if (!matchingHierarchyinvalied.Any())
                                            {
                                                if (!item.ErrorData.ContainsKey("Class Code"))
                                                {
                                                    item.ErrorData.Add("Class Code", new List<string>());
                                                    item.ErrorData["Class Code"].Add($"Invalid Class Code {classCode}");
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (item.ColumnData.ContainsKey("Subclass Code"))
                            {
                                if (string.IsNullOrWhiteSpace(subClassCode.ToString()))
                                {
                                    if (!item.ErrorData.ContainsKey("Subclass Code"))
                                    {
                                        item.ErrorData.Add("Subclass Code", new List<string>());
                                    }

                                    item.ErrorData["Subclass Code"].Add("Subclass Code Field is required");
                                }
                                else
                                {
                                    if (subClassCode is string)
                                    {
                                        if (!item.ErrorData.ContainsKey("Subclass Code"))
                                        {
                                            item.ErrorData.Add("Subclass Code", new List<string>());
                                        }

                                        item.ErrorData["Subclass Code"].Add($"Special characters are present in Subclass Code '{subClassCode}'");
                                    }
                                    else
                                    {
                                        long deptCodeValue;
                                        long classCodeValue;
                                        long subClassCodeValue;
                                        long groupCodeValue;
                                        long divisionCodeValue;
                                        if (item.ColumnData["Dept Code"] != null && long.TryParse(item.ColumnData["Dept Code"].ToString(), out deptCodeValue) &&
                                            item.ColumnData["Class Code"] != null && long.TryParse(item.ColumnData["Class Code"].ToString(), out classCodeValue) &&
                                            item.ColumnData["Division Code"] != null && long.TryParse(item.ColumnData["Division Code"].ToString(), out divisionCodeValue) &&
                                            item.ColumnData["Subclass Code"] != null && long.TryParse(item.ColumnData["Subclass Code"].ToString(), out subClassCodeValue) &&
                                            item.ColumnData["Group Code"] != null && long.TryParse(item.ColumnData["Group Code"].ToString(), out groupCodeValue))
                                        {
                                            var matchingHierarchyinvalied = hierarchy.Where(a => a.Subclass != null && (long)a.Subclass == subClassCodeValue);
                                            if (!matchingHierarchyinvalied.Any())
                                            {
                                                if (!item.ErrorData.ContainsKey("Subclass Code"))
                                                {
                                                    item.ErrorData.Add("Subclass Code", new List<string>());
                                                    item.ErrorData["Subclass Code"].Add($"Invalid Subclass Code '{subClassCode}'");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(uids) || count1 == 12)
                        {
                            if (item.ColumnData.ContainsKey("Dept Code") && item.ColumnData.ContainsKey("Class Code") && item.ColumnData.ContainsKey("Subclass Code"))
                            {
                                long deptCodeValue;
                                long classCodeValue;
                                long subClassCodeValue;
                                long groupCodeValue;
                                long divisionCodeValue;
                                if (item.ColumnData["Dept Code"] != null && long.TryParse(item.ColumnData["Dept Code"].ToString(), out deptCodeValue) &&
                                    item.ColumnData["Class Code"] != null && long.TryParse(item.ColumnData["Class Code"].ToString(), out classCodeValue) &&
                                    item.ColumnData["Division Code"] != null && long.TryParse(item.ColumnData["Division Code"].ToString(), out divisionCodeValue) &&
                                    item.ColumnData["Subclass Code"] != null && long.TryParse(item.ColumnData["Subclass Code"].ToString(), out subClassCodeValue) &&
                                    item.ColumnData["Group Code"] != null && long.TryParse(item.ColumnData["Group Code"].ToString(), out groupCodeValue))
                                {
                                    var matchingHierarchy = hierarchy.Where(a =>
                                             a.Division != null && (long)a.Division == divisionCodeValue && a.Group_no != null && (long)a.Group_no == groupCodeValue &&
                                            a.Dept != null && (long)a.Dept == deptCodeValue && a.Class != null && (long)a.Class == classCodeValue &&
                                            a.Subclass != null && (long)a.Subclass == subClassCodeValue);
                                    if (!matchingHierarchy.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("Group Code"))
                                        {
                                            item.ErrorData.Add("Group Code", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Dept Code"))
                                        {
                                            item.ErrorData.Add("Dept Code", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Class Code"))
                                        {
                                            item.ErrorData.Add("Class Code", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Subclass Code"))
                                        {
                                            item.ErrorData.Add("Subclass Code", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Hierarchy"))
                                        {
                                            item.ErrorData.Add("Hierarchy", new List<string>());
                                        }

                                        if (item.ErrorData["Hierarchy"].Count == 0)
                                        {
                                            item.ErrorData["Hierarchy"].Add($"Hierarchy {div}-{group}-{deptCode}-{classCode}-{subClassCode} is mismatched");
                                        }

                                        if (!item.ErrorData.ContainsKey("Division Code"))
                                        {
                                            item.ErrorData.Add("Division Code", new List<string>());
                                        }
                                    }
                                }
                            }
                        }
                    });
                }

                if (templateDivisionId == "61" || templateDivisionId == "4")
                {
                    Parallel.ForEach(data, item =>
                    {
                        var uids = item.ColumnData["Uid"].ToString();
                        var count1 = item?.ColumnData.Count();
                        var div = item.ColumnData["Division Code"];
                        var divError = item.ErrorData.ContainsKey("Division Code");
                        var group = item.ColumnData["Group Code"];
                        var groupError = item.ErrorData.ContainsKey("Group Code");
                        var deptCode = item.ColumnData["Dept Code"];
                        var classCode = item.ColumnData["Class Code"];
                        var subClassCode = item.ColumnData["Subclass Code"];
                        var result = results.FirstOrDefault(a => a.Uid.ToLower() == uids.ToLower());

                        if (result != null && result.ItemcodeId.HasValue)
                        {
                            if (!item.ErrorData.ContainsKey("Uid"))
                            {
                                item.ErrorData.Add("Uid", new List<string>());
                            }

                            item.ErrorData["Uid"].Add("Cannot change library values as UID contains an Item code.");
                        }

                        if (string.IsNullOrEmpty(div.ToString()))
                        {
                            if (!divError)
                            {
                                item.ErrorData.Add("Division Code", new List<string>());
                            }

                            item.ErrorData["Division Code"].Add("Division Code Field is required");
                        }
                        else if (div is string)
                        {
                            if (!divError)
                            {
                                item.ErrorData.Add("Division Code", new List<string>());
                            }

                            item.ErrorData["Division Code"].Add($"Special characters are present in Division Code '{div}'.");
                        }
                        else if (!string.IsNullOrEmpty(div.ToString()) && div.ToString() != templateDivisionId)
                        {
                            if (!divError)
                            {
                                item.ErrorData.Add("Division Code", new List<string>());
                            }

                            item.ErrorData["Division Code"].Add($"Template div ({templateDivisionId}) does not match with record div_code ({div}).");
                        }

                        if (string.IsNullOrEmpty(group.ToString()))
                        {
                            if (!groupError)
                            {
                                item.ErrorData.Add("Group Code", new List<string>());
                            }

                            item.ErrorData["Group Code"].Add("Group Code Field is required");
                        }
                        else if (group is string)
                        {
                            if (!groupError)
                            {
                                item.ErrorData.Add("Group Code", new List<string>());
                            }

                            item.ErrorData["Group Code"].Add($"Special characters are present in Group Code '{group}'.");
                        }
                        else if (!string.IsNullOrEmpty(group.ToString()) && group.ToString() != templateDivisionId)
                        {
                            if (!groupError)
                            {
                                item.ErrorData.Add("Group Code", new List<string>());
                            }

                            ////if (!item.ErrorData.ContainsKey("Dept Code"))
                            ////{
                            ////    item.ErrorData.Add("Dept Code", new List<string>());
                            ////}

                            ////if (!item.ErrorData.ContainsKey("Class Code"))
                            ////{
                            ////    item.ErrorData.Add("Class Code", new List<string>());
                            ////}

                            ////if (!item.ErrorData.ContainsKey("Subclass Code"))
                            ////{
                            ////    item.ErrorData.Add("Subclass Code", new List<string>());
                            ////}

                            ////if (!item.ErrorData.ContainsKey("Hierarchy"))
                            ////{
                            ////    item.ErrorData.Add("Hierarchy", new List<string>());
                            ////}

                            ////if (item.ErrorData["Hierarchy"].Count == 0)
                            ////{
                            ////    item.ErrorData["Hierarchy"].Add($"Hierarchy {div}-{group}-{deptCode}-{classCode}-{subClassCode} is mismatched");
                            ////}

                            if (!item.ErrorData.ContainsKey("Division Code"))
                            {
                                item.ErrorData.Add("Division Code", new List<string>());
                            }

                            item.ErrorData["Group Code"].Add($"Invalid Group Code {group}");

                            // item.ErrorData["Group Code"].Add($"Invalid Group Code {group}, Hierarchy Group Code {group}, Dept Code {deptCode}, Class Code {classCode}, Subclass Code {subClassCode}, Group Code {group} are not Matching");
                        }

                        if (string.IsNullOrEmpty(uids) || count1 == 12)
                        {
                            if (item.ColumnData.ContainsKey("Dept Code"))
                            {
                                if (string.IsNullOrWhiteSpace(deptCode.ToString()))
                                {
                                    if (!item.ErrorData.ContainsKey("Dept Code"))
                                    {
                                        item.ErrorData.Add("Dept Code", new List<string>());
                                    }

                                    item.ErrorData["Dept Code"].Add("Dept Code Field is required");
                                }
                                else
                                {
                                    if (deptCode is string)
                                    {
                                        if (!item.ErrorData.ContainsKey("Dept Code"))
                                        {
                                            item.ErrorData.Add("Dept Code", new List<string>());
                                        }

                                        item.ErrorData["Dept Code"].Add($"Special characters are present in Dept Code '{deptCode}'");
                                    }
                                    else
                                    {
                                        long divisionCodeValue;
                                        long groupCodeValue;
                                        long deptCodeValue;
                                        if (item.ColumnData["Division Code"] != null && long.TryParse(item.ColumnData["Division Code"].ToString(), out divisionCodeValue) &&
                                            item.ColumnData["Dept Code"] != null && long.TryParse(item.ColumnData["Dept Code"].ToString(), out deptCodeValue) &&
                                            item.ColumnData["Group Code"] != null && long.TryParse(item.ColumnData["Group Code"].ToString(), out groupCodeValue))
                                        {
                                            var matchingHierarchyinvalied = hierarchy.Where(a => a.Dept != null && (long)a.Dept == deptCodeValue);
                                            if (!matchingHierarchyinvalied.Any())
                                            {
                                                if (!item.ErrorData.ContainsKey("Dept Code"))
                                                {
                                                    item.ErrorData.Add("Dept Code", new List<string>());
                                                    item.ErrorData["Dept Code"].Add($"Invalid Dept Code {deptCode}");
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (item.ColumnData.ContainsKey("Class Code"))
                            {
                                if (string.IsNullOrWhiteSpace(classCode.ToString()))
                                {
                                    if (!item.ErrorData.ContainsKey("Class Code"))
                                    {
                                        item.ErrorData.Add("Class Code", new List<string>());
                                    }

                                    item.ErrorData["Class Code"].Add("Class Code Field is required");
                                }
                                else
                                {
                                    if (classCode is string)
                                    {
                                        if (!item.ErrorData.ContainsKey("Class Code"))
                                        {
                                            item.ErrorData.Add("Class Code", new List<string>());
                                        }

                                        item.ErrorData["Class Code"].Add($"Special characters are present in Class Code '{classCode}'");
                                    }
                                    else
                                    {
                                        long deptCodeValue;
                                        long classCodeValue;
                                        long groupCodeValue;
                                        long divisionCodeValue;
                                        if (item.ColumnData["Dept Code"] != null && long.TryParse(item.ColumnData["Dept Code"].ToString(), out deptCodeValue) &&
                                            item.ColumnData["Class Code"] != null && long.TryParse(item.ColumnData["Class Code"].ToString(), out classCodeValue) &&
                                            item.ColumnData["Division Code"] != null && long.TryParse(item.ColumnData["Division Code"].ToString(), out divisionCodeValue) &&
                                            item.ColumnData["Group Code"] != null && long.TryParse(item.ColumnData["Group Code"].ToString(), out groupCodeValue))
                                        {
                                            var matchingHierarchyinvalied = hierarchy.Where(a => a.Class != null && (long)a.Class == classCodeValue);
                                            if (!matchingHierarchyinvalied.Any())
                                            {
                                                if (!item.ErrorData.ContainsKey("Class Code"))
                                                {
                                                    item.ErrorData.Add("Class Code", new List<string>());
                                                    item.ErrorData["Class Code"].Add($"Invalid Class Code {classCode}");
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (item.ColumnData.ContainsKey("Subclass Code"))
                            {
                                if (string.IsNullOrWhiteSpace(subClassCode.ToString()))
                                {
                                    if (!item.ErrorData.ContainsKey("Subclass Code"))
                                    {
                                        item.ErrorData.Add("Subclass Code", new List<string>());
                                    }

                                    item.ErrorData["Subclass Code"].Add("Subclass Code Field is required");
                                }
                                else
                                {
                                    if (subClassCode is string)
                                    {
                                        if (!item.ErrorData.ContainsKey("Subclass Code"))
                                        {
                                            item.ErrorData.Add("Subclass Code", new List<string>());
                                        }

                                        item.ErrorData["Subclass Code"].Add($"Special characters are present in Subclass Code '{subClassCode}'");
                                    }
                                    else
                                    {
                                        long deptCodeValue;
                                        long groupCodeValue;
                                        long classCodeValue;
                                        long subClassCodeValue;
                                        long divisionCodeValue;
                                        if (item.ColumnData["Dept Code"] != null && long.TryParse(item.ColumnData["Dept Code"].ToString(), out deptCodeValue) &&
                                            item.ColumnData["Class Code"] != null && long.TryParse(item.ColumnData["Class Code"].ToString(), out classCodeValue) &&
                                            item.ColumnData["Division Code"] != null && long.TryParse(item.ColumnData["Division Code"].ToString(), out divisionCodeValue) &&
                                            item.ColumnData["Subclass Code"] != null && long.TryParse(item.ColumnData["Subclass Code"].ToString(), out subClassCodeValue) &&
                                            item.ColumnData["Group Code"] != null && long.TryParse(item.ColumnData["Group Code"].ToString(), out groupCodeValue))
                                        {
                                            var matchingHierarchyinvalied = hierarchy.Where(a => a.Subclass != null && (long)a.Subclass == subClassCodeValue);
                                            if (!matchingHierarchyinvalied.Any())
                                            {
                                                if (!item.ErrorData.ContainsKey("Subclass Code"))
                                                {
                                                    item.ErrorData.Add("Subclass Code", new List<string>());
                                                    item.ErrorData["Subclass Code"].Add($"Invalid Subclass Code '{subClassCode}'");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(uids) || count1 == 12)
                        {
                            if (item.ColumnData.ContainsKey("Dept Code") && item.ColumnData.ContainsKey("Class Code") && item.ColumnData.ContainsKey("Subclass Code"))
                            {
                                long deptCodeValue;
                                long classCodeValue;
                                long subClassCodeValue;
                                long groupCodeValue;
                                long divisionCodeValue;
                                if (item.ColumnData["Dept Code"] != null && long.TryParse(item.ColumnData["Dept Code"].ToString(), out deptCodeValue) &&
                                    item.ColumnData["Class Code"] != null && long.TryParse(item.ColumnData["Class Code"].ToString(), out classCodeValue) &&
                                    item.ColumnData["Division Code"] != null && long.TryParse(item.ColumnData["Division Code"].ToString(), out divisionCodeValue) &&
                                    item.ColumnData["Subclass Code"] != null && long.TryParse(item.ColumnData["Subclass Code"].ToString(), out subClassCodeValue) &&
                                    item.ColumnData["Group Code"] != null && long.TryParse(item.ColumnData["Group Code"].ToString(), out groupCodeValue))
                                {
                                    var matchingHierarchy = hierarchy.Where(a =>
                                             a.Division != null && (long)a.Division == divisionCodeValue && a.Group_no != null && (long)a.Group_no == groupCodeValue &&
                                            a.Dept != null && (long)a.Dept == deptCodeValue && a.Class != null && (long)a.Class == classCodeValue &&
                                            a.Subclass != null && (long)a.Subclass == subClassCodeValue);
                                    if (!matchingHierarchy.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("Group Code"))
                                        {
                                            item.ErrorData.Add("Group Code", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Dept Code"))
                                        {
                                            item.ErrorData.Add("Dept Code", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Class Code"))
                                        {
                                            item.ErrorData.Add("Class Code", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Subclass Code"))
                                        {
                                            item.ErrorData.Add("Subclass Code", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Hierarchy"))
                                        {
                                            item.ErrorData.Add("Hierarchy", new List<string>());
                                        }

                                        if (item.ErrorData["Hierarchy"].Count == 0)
                                        {
                                            item.ErrorData["Hierarchy"].Add($"Hierarchy {div}-{group}-{deptCode}-{classCode}-{subClassCode} is mismatched");
                                        }

                                        if (!item.ErrorData.ContainsKey("Division Code"))
                                        {
                                            item.ErrorData.Add("Division Code", new List<string>());
                                        }
                                    }
                                }
                            }
                        }
                    });
                }
            }

            if (!template.IsUidTemplate)
            {
                Parallel.ForEach(data, item =>
                {
                    var uidData = item.ColumnData["Uid"];
                    if (string.IsNullOrWhiteSpace(uidData.ToString()))
                    {
                        if (!item.ErrorData.ContainsKey("Uid"))
                        {
                            item.ErrorData.Add("Uid", new List<string>());
                        }

                        item.ErrorData["Uid"].Add("Uid Field is required");
                    }
                });
            }

            // Buyer template 2
            if (!template.IsUidTemplate)
            {
                Parallel.ForEach(data, item =>
                {
                    if (templateDivisionId == "61")
                    {
                        using var connection1 = this.GetConnection();
                        var getUiddetails = connection1.QueryFirstOrDefault<Product>($"select Columndata from product where lower(uid)=lower('{item.ColumnData["Uid"]}') limit 1");
                        if (getUiddetails != null)
                        {
                            var div = Convert.ToInt64(getUiddetails.ColumnData["Division Code"]);
                            var deptCode = Convert.ToInt64(getUiddetails.ColumnData["Dept Code"]);
                            var classCode = Convert.ToInt64(getUiddetails.ColumnData["Class Code"]);
                            var subClassCode = Convert.ToInt64(getUiddetails.ColumnData["Subclass Code"]);
                            if (item.ColumnData.ContainsKey("Product Type 1"))
                            {
                                var productType_2 = item.ColumnData["Product Type 2"];
                                var productSubType_2 = item.ColumnData["Product Sub Type 2"];
                                var productSubType_1 = item.ColumnData["Product Sub Type 1"];
                                var productType_1 = item.ColumnData["Product Type 1"];
                                if (string.IsNullOrWhiteSpace(productType_1.ToString()))
                                {
                                    if (!item.ErrorData.ContainsKey("Product Type 1"))
                                    {
                                        item.ErrorData.Add("Product Type 1", new List<string>());
                                    }

                                    item.ErrorData["Product Type 1"].Add("Product Type 1 Field is required");
                                }
                                else
                                {
                                    var product_Type_1 = typehierarchy.Where(a => string.Equals(a.Producttype1, productType_1.ToString().Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
                                    if (!product_Type_1.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("Product Type 1"))
                                        {
                                            item.ErrorData.Add("Product Type 1", new List<string>());
                                        }

                                        item.ErrorData["Product Type 1"].Add($"Invalid Product Type 1 '{productType_1}'");
                                    }
                                }

                                var matchingHierarchy = typehierarchy.Where(a =>
                                        a.Div_code == div && a.Subclass_code == subClassCode && a.Class_code == classCode && a.Dept_code == deptCode &&
                                        a.Producttype1 == productType_1.ToString().Trim());
                                if (!matchingHierarchy.Any())
                                {
                                    if (!item.ErrorData.ContainsKey("Product Type 1"))
                                    {
                                        item.ErrorData.Add("Product Type 1", new List<string>());
                                    }

                                    if (!item.ErrorData.ContainsKey("Product Sub Type 1"))
                                    {
                                        item.ErrorData.Add("Product Sub Type 1", new List<string>());
                                    }

                                    if (!item.ErrorData.ContainsKey("Product Type 2"))
                                    {
                                        item.ErrorData.Add("Product Type 2", new List<string>());
                                    }

                                    if (!item.ErrorData.ContainsKey("Product Sub Type 2"))
                                    {
                                        item.ErrorData.Add("Product Sub Type 2", new List<string>());
                                    }

                                    if (!item.ErrorData.ContainsKey("Hierarchy"))
                                    {
                                        item.ErrorData.Add("Hierarchy", new List<string>());
                                    }

                                    if (item.ErrorData["Hierarchy"].Count == 0)
                                    {
                                        item.ErrorData["Hierarchy"].Add($"Hierarchy {productType_1}-{productSubType_1}-{productType_2}-{productSubType_2} is mismatched");
                                    }
                                }
                            }

                            if (item.ColumnData.ContainsKey("Product Sub Type 1"))
                            {
                                var productType_2 = item.ColumnData["Product Type 2"];
                                var productSubType_2 = item.ColumnData["Product Sub Type 2"];
                                var productSubType_1 = item.ColumnData["Product Sub Type 1"];
                                var productType_1 = item.ColumnData["Product Type 1"];
                                if (!string.IsNullOrWhiteSpace(productSubType_1.ToString()))
                                {
                                    var product_Sub_type_1 = typehierarchy.Where(a => string.Equals(a.Productsubtype1, productSubType_1.ToString().Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
                                    if (!product_Sub_type_1.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("Product Sub Type 1"))
                                        {
                                            item.ErrorData.Add("Product Sub Type 1", new List<string>());
                                        }

                                        item.ErrorData["Product Sub Type 1"].Add($"Invalid Product Sub Type 1 '{productSubType_1}'");
                                    }

                                    var matchingHierarchy = typehierarchy.Where(a =>
                                       a.Div_code == div && a.Subclass_code == subClassCode && a.Class_code == classCode && a.Dept_code == deptCode &&
                                       a.Producttype1 == productType_1.ToString().Trim() && a.Productsubtype1 == productSubType_1.ToString().Trim());

                                    if (!matchingHierarchy.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("Product Type 1"))
                                        {
                                            item.ErrorData.Add("Product Type 1", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Product Sub Type 1"))
                                        {
                                            item.ErrorData.Add("Product Sub Type 1", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Product Type 2"))
                                        {
                                            item.ErrorData.Add("Product Type 2", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Product Sub Type 2"))
                                        {
                                            item.ErrorData.Add("Product Sub Type 2", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Hierarchy"))
                                        {
                                            item.ErrorData.Add("Hierarchy", new List<string>());
                                        }

                                        if (item.ErrorData["Hierarchy"].Count == 0)
                                        {
                                            item.ErrorData["Hierarchy"].Add($"Hierarchy {productType_1}-{productSubType_1}-{productType_2}-{productSubType_2} is mismatched");
                                        }
                                    }
                                }
                            }

                            if (item.ColumnData.ContainsKey("Product Type 2"))
                            {
                                var productType_2 = item.ColumnData["Product Type 2"];
                                var productSubType_2 = item.ColumnData["Product Sub Type 2"];
                                var productSubType_1 = item.ColumnData["Product Sub Type 1"];
                                var productType_1 = item.ColumnData["Product Type 1"];
                                if (!string.IsNullOrWhiteSpace(productType_2.ToString()))
                                {
                                    var product_Type_2 = typehierarchy.Where(a => string.Equals(a.Producttype2, productType_2.ToString().Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
                                    if (!product_Type_2.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("Product Type 2"))
                                        {
                                            item.ErrorData.Add("Product Type 2", new List<string>());
                                        }

                                        item.ErrorData["Product Type 2"].Add($"Invalid Product Type 2 '{productType_2}'");
                                    }

                                    var matchingHierarchy = typehierarchy.Where(a =>
        a.Div_code == div &&
        a.Subclass_code == subClassCode &&
        a.Class_code == classCode &&
        a.Dept_code == deptCode &&
        a.Producttype1 == productType_1.ToString().Trim() &&
        (string.IsNullOrEmpty(productSubType_1?.ToString().Trim()) || a.Productsubtype1 == productSubType_1.ToString().Trim()) &&
        (string.IsNullOrEmpty(productType_2?.ToString().Trim()) || a.Producttype2 == productType_2.ToString().Trim()));

                                    if (!matchingHierarchy.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("Product Type 1"))
                                        {
                                            item.ErrorData.Add("Product Type 1", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Product Sub Type 1"))
                                        {
                                            item.ErrorData.Add("Product Sub Type 1", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Product Type 2"))
                                        {
                                            item.ErrorData.Add("Product Type 2", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Product Sub Type 2"))
                                        {
                                            item.ErrorData.Add("Product Sub Type 2", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Hierarchy"))
                                        {
                                            item.ErrorData.Add("Hierarchy", new List<string>());
                                        }

                                        if (item.ErrorData["Hierarchy"].Count == 0)
                                        {
                                            item.ErrorData["Hierarchy"].Add($"Hierarchy {productType_1}-{productSubType_1}-{productType_2}-{productSubType_2} is mismatched");
                                        }
                                    }
                                }
                            }

                            if (item.ColumnData.ContainsKey("Product Sub Type 2"))
                            {
                                var productType_2 = item.ColumnData["Product Type 2"];
                                var productSubType_2 = item.ColumnData["Product Sub Type 2"];
                                var productSubType_1 = item.ColumnData["Product Sub Type 1"];
                                var productType_1 = item.ColumnData["Product Type 1"];
                                if (!string.IsNullOrWhiteSpace(productSubType_2.ToString()))
                                {
                                    var product_Sub_type_2 = typehierarchy.Where(a => string.Equals(a.Productsubtype2, productSubType_2.ToString().Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
                                    if (!product_Sub_type_2.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("Product Sub Type 2"))
                                        {
                                            item.ErrorData.Add("Product Sub Type 2", new List<string>());
                                        }

                                        item.ErrorData["Product Sub Type 2"].Add($"Invalid Product Sub Type 2 '{productSubType_2}'");
                                    }

                                    var matchingHierarchy = typehierarchy.Where(a =>
         a.Div_code == div &&
         a.Subclass_code == subClassCode &&
         a.Class_code == classCode &&
         a.Dept_code == deptCode &&
         a.Producttype1 == productType_1.ToString().Trim() &&
         (string.IsNullOrEmpty(productSubType_1?.ToString().Trim()) || a.Productsubtype1 == productSubType_1.ToString().Trim()) &&
         (string.IsNullOrEmpty(productType_2?.ToString().Trim()) || a.Producttype2 == productType_2.ToString().Trim()) &&
         (string.IsNullOrEmpty(productSubType_2?.ToString().Trim()) || a.Productsubtype2 == productSubType_2.ToString().Trim()));

                                    if (!matchingHierarchy.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("Product Type 1"))
                                        {
                                            item.ErrorData.Add("Product Type 1", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Product Sub Type 1"))
                                        {
                                            item.ErrorData.Add("Product Sub Type 1", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Product Type 2"))
                                        {
                                            item.ErrorData.Add("Product Type 2", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Product Sub Type 2"))
                                        {
                                            item.ErrorData.Add("Product Sub Type 2", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Hierarchy"))
                                        {
                                            item.ErrorData.Add("Hierarchy", new List<string>());
                                        }

                                        if (item.ErrorData["Hierarchy"].Count == 0)
                                        {
                                            item.ErrorData["Hierarchy"].Add($"Hierarchy {productType_1}-{productSubType_1}-{productType_2}-{productSubType_2} is mismatched");
                                        }
                                    }
                                }
                            }

                            if (item.ColumnData.ContainsKey("Product Type 1"))
                            {
                                var productType_2 = item.ColumnData["Product Type 2"];
                                var productSubType_2 = item.ColumnData["Product Sub Type 2"];
                                var productSubType_1 = item.ColumnData["Product Sub Type 1"];
                                var productType_1 = item.ColumnData["Product Type 1"];

                                if (productType_1 != string.Empty || productType_2 != string.Empty || productSubType_2 != string.Empty || productSubType_1 != string.Empty)
                                {
                                    var matchingHierarchy = typehierarchy.Where(a =>
        a.Div_code == div &&
        a.Subclass_code == subClassCode &&
        a.Class_code == classCode &&
        a.Dept_code == deptCode &&
        (string.IsNullOrEmpty(productType_1?.ToString().Trim()) || a.Producttype1 == productType_1.ToString().Trim()) &&
        (string.IsNullOrEmpty(productSubType_1?.ToString().Trim()) || a.Productsubtype1 == productSubType_1.ToString().Trim()) &&
        (string.IsNullOrEmpty(productType_2?.ToString().Trim()) ? string.IsNullOrEmpty(a.Producttype2) : a.Producttype2 == productType_2.ToString().Trim()) &&
        (string.IsNullOrEmpty(productSubType_2?.ToString().Trim()) ? string.IsNullOrEmpty(a.Productsubtype2) : a.Productsubtype2 == productSubType_2.ToString().Trim()))
        .ToList();
                                    if (!matchingHierarchy.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("Product Type 1"))
                                        {
                                            item.ErrorData.Add("Product Type 1", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Product Sub Type 1"))
                                        {
                                            item.ErrorData.Add("Product Sub Type 1", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Product Type 2"))
                                        {
                                            item.ErrorData.Add("Product Type 2", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Product Sub Type 2"))
                                        {
                                            item.ErrorData.Add("Product Sub Type 2", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Hierarchy"))
                                        {
                                            item.ErrorData.Add("Hierarchy", new List<string>());
                                        }

                                        if (item.ErrorData["Hierarchy"].Count == 0)
                                        {
                                            item.ErrorData["Hierarchy"].Add($"Hierarchy {productType_1}-{productSubType_1}-{productType_2}-{productSubType_2} is mismatched");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (templateDivisionId == "4" || templateDivisionId == "65")
                    {
                        using var connection1 = this.GetConnection();
                        var getUiddetails = connection1.QueryFirstOrDefault<Product>($"select Columndata from product where lower(uid)=lower('{item.ColumnData["Uid"]}') limit 1");
                        if (getUiddetails != null)
                        {
                            var div = Convert.ToInt64(getUiddetails.ColumnData["Division Code"]);
                            var deptCode = Convert.ToInt64(getUiddetails.ColumnData["Dept Code"]);
                            var classCode = Convert.ToInt64(getUiddetails.ColumnData["Class Code"]);
                            var subClassCode = Convert.ToInt64(getUiddetails.ColumnData["Subclass Code"]);
                            if (item.ColumnData.ContainsKey("Product Type 1"))
                            {
                                var productType_1 = item.ColumnData["Product Type 1"];
                                var productType_2 = item.ColumnData["Product Type 2"];
                                var productSubType_2 = item.ColumnData["Product Sub Type 2"];
                                var productSubType_1 = item.ColumnData["Product Sub Type 1"];
                                if (!string.IsNullOrWhiteSpace(productType_1.ToString()))
                                {
                                    var product_Type_1 = typehierarchy.Where(a => string.Equals(a.Producttype1, productType_1.ToString().Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
                                    if (!product_Type_1.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("Product Type 1"))
                                        {
                                            item.ErrorData.Add("Product Type 1", new List<string>());
                                        }

                                        item.ErrorData["Product Type 1"].Add($"Invalid Product Type 1 '{productType_1}'");
                                    }

                                    var matchingHierarchy = typehierarchy.Where(a =>
                                        a.Div_code == div && a.Subclass_code == subClassCode && a.Class_code == classCode && a.Dept_code == deptCode &&
                                        a.Producttype1 == productType_1.ToString().Trim());
                                    if (!matchingHierarchy.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("Product Type 1"))
                                        {
                                            item.ErrorData.Add("Product Type 1", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Product Sub Type 1"))
                                        {
                                            item.ErrorData.Add("Product Sub Type 1", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Product Type 2"))
                                        {
                                            item.ErrorData.Add("Product Type 2", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Product Sub Type 2"))
                                        {
                                            item.ErrorData.Add("Product Sub Type 2", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Hierarchy"))
                                        {
                                            item.ErrorData.Add("Hierarchy", new List<string>());
                                        }

                                        if (item.ErrorData["Hierarchy"].Count == 0)
                                        {
                                            item.ErrorData["Hierarchy"].Add($"Hierarchy {productType_1}-{productSubType_1}-{productType_2}-{productSubType_2} is mismatched");
                                        }
                                    }
                                }
                            }

                            if (item.ColumnData.ContainsKey("Product Sub Type 1"))
                            {
                                var productType_1 = item.ColumnData["Product Type 1"];
                                var productType_2 = item.ColumnData["Product Type 2"];
                                var productSubType_2 = item.ColumnData["Product Sub Type 2"];
                                var productSubType_1 = item.ColumnData["Product Sub Type 1"];
                                if (!string.IsNullOrWhiteSpace(productSubType_1.ToString()))
                                {
                                    var product_Sub_type_1 = typehierarchy.Where(a => string.Equals(a.Productsubtype1, productSubType_1.ToString().Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
                                    if (!product_Sub_type_1.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("Product Sub Type 1"))
                                        {
                                            item.ErrorData.Add("Product Sub Type 1", new List<string>());
                                        }

                                        item.ErrorData["Product Sub Type 1"].Add($"Invalid Product Sub Type 1 '{productSubType_1}'");
                                    }

                                    var matchingHierarchy = typehierarchy.Where(a =>
                                       a.Div_code == div && a.Subclass_code == subClassCode && a.Class_code == classCode && a.Dept_code == deptCode &&
                                       (string.IsNullOrEmpty(productType_1?.ToString().Trim()) || a.Producttype1 == productType_1.ToString().Trim()) &&
                                       a.Producttype1 == productType_1.ToString().Trim() && a.Productsubtype1 == productSubType_1.ToString().Trim());

                                    if (!matchingHierarchy.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("Product Type 1"))
                                        {
                                            item.ErrorData.Add("Product Type 1", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Product Sub Type 1"))
                                        {
                                            item.ErrorData.Add("Product Sub Type 1", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Product Type 2"))
                                        {
                                            item.ErrorData.Add("Product Type 2", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Product Sub Type 2"))
                                        {
                                            item.ErrorData.Add("Product Sub Type 2", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Hierarchy"))
                                        {
                                            item.ErrorData.Add("Hierarchy", new List<string>());
                                        }

                                        if (item.ErrorData["Hierarchy"].Count == 0)
                                        {
                                            item.ErrorData["Hierarchy"].Add($"Hierarchy {productType_1}-{productSubType_1}-{productType_2}-{productSubType_2} is mismatched");
                                        }
                                    }
                                }
                            }

                            if (item.ColumnData.ContainsKey("Product Type 2"))
                            {
                                var productType_1 = item.ColumnData["Product Type 1"];
                                var productType_2 = item.ColumnData["Product Type 2"];
                                var productSubType_2 = item.ColumnData["Product Sub Type 2"];
                                var productSubType_1 = item.ColumnData["Product Sub Type 1"];
                                if (!string.IsNullOrWhiteSpace(productType_2.ToString()))
                                {
                                    var product_Type_2 = typehierarchy.Where(a => string.Equals(a.Producttype2, productType_2.ToString().Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
                                    if (!product_Type_2.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("Product Type 2"))
                                        {
                                            item.ErrorData.Add("Product Type 2", new List<string>());
                                        }

                                        item.ErrorData["Product Type 2"].Add($"Invalid Product Type 2 '{productType_2}'");
                                    }

                                    var matchingHierarchy = typehierarchy.Where(a =>
        a.Div_code == div &&
        a.Subclass_code == subClassCode &&
        a.Class_code == classCode &&
        a.Dept_code == deptCode &&
        (string.IsNullOrEmpty(productType_1?.ToString().Trim()) || a.Producttype1 == productType_1.ToString().Trim()) &&
        (string.IsNullOrEmpty(productSubType_1?.ToString().Trim()) || a.Productsubtype1 == productSubType_1.ToString().Trim()) &&
        (string.IsNullOrEmpty(productType_2?.ToString().Trim()) || a.Producttype2 == productType_2.ToString().Trim()));
                                    if (!matchingHierarchy.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("Product Type 1"))
                                        {
                                            item.ErrorData.Add("Product Type 1", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Product Sub Type 1"))
                                        {
                                            item.ErrorData.Add("Product Sub Type 1", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Product Type 2"))
                                        {
                                            item.ErrorData.Add("Product Type 2", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Product Sub Type 2"))
                                        {
                                            item.ErrorData.Add("Product Sub Type 2", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Hierarchy"))
                                        {
                                            item.ErrorData.Add("Hierarchy", new List<string>());
                                        }

                                        if (item.ErrorData["Hierarchy"].Count == 0)
                                        {
                                            item.ErrorData["Hierarchy"].Add($"Hierarchy {productType_1}-{productSubType_1}-{productType_2}-{productSubType_2} is mismatched");
                                        }
                                    }
                                }
                            }

                            if (item.ColumnData.ContainsKey("Product Sub Type 2"))
                            {
                                var productType_1 = item.ColumnData["Product Type 1"];
                                var productType_2 = item.ColumnData["Product Type 2"];
                                var productSubType_2 = item.ColumnData["Product Sub Type 2"];
                                var productSubType_1 = item.ColumnData["Product Sub Type 1"];
                                if (!string.IsNullOrWhiteSpace(productSubType_2.ToString()))
                                {
                                    var product_Sub_type_2 = typehierarchy.Where(a => string.Equals(a.Productsubtype2, productSubType_2.ToString().Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
                                    if (!product_Sub_type_2.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("Product Sub Type 2"))
                                        {
                                            item.ErrorData.Add("Product Sub Type 2", new List<string>());
                                        }

                                        item.ErrorData["Product Sub Type 2"].Add($"Invalid Product Sub Type 2 '{productSubType_2}'");
                                    }

                                    var matchingHierarchy = typehierarchy.Where(a =>
         a.Div_code == div &&
         a.Subclass_code == subClassCode &&
         a.Class_code == classCode &&
         a.Dept_code == deptCode &&
         (string.IsNullOrEmpty(productType_1?.ToString().Trim()) || a.Producttype1 == productType_1.ToString().Trim()) &&
         (string.IsNullOrEmpty(productSubType_1?.ToString().Trim()) || a.Productsubtype1 == productSubType_1.ToString().Trim()) &&
         (string.IsNullOrEmpty(productType_2?.ToString().Trim()) || a.Producttype2 == productType_2.ToString().Trim()) &&
         (string.IsNullOrEmpty(productSubType_2?.ToString().Trim()) || a.Productsubtype2 == productSubType_2.ToString().Trim()));

                                    if (!matchingHierarchy.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("Product Type 1"))
                                        {
                                            item.ErrorData.Add("Product Type 1", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Product Sub Type 1"))
                                        {
                                            item.ErrorData.Add("Product Sub Type 1", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Product Type 2"))
                                        {
                                            item.ErrorData.Add("Product Type 2", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Product Sub Type 2"))
                                        {
                                            item.ErrorData.Add("Product Sub Type 2", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Hierarchy"))
                                        {
                                            item.ErrorData.Add("Hierarchy", new List<string>());
                                        }

                                        if (item.ErrorData["Hierarchy"].Count == 0)
                                        {
                                            item.ErrorData["Hierarchy"].Add($"Hierarchy {productType_1}-{productSubType_1}-{productType_2}-{productSubType_2} is mismatched");
                                        }
                                    }
                                }
                            }

                            if (item.ColumnData.ContainsKey("Product Type 1"))
                            {
                                var productType_1 = item.ColumnData["Product Type 1"];
                                var productType_2 = item.ColumnData["Product Type 2"];
                                var productSubType_2 = item.ColumnData["Product Sub Type 2"];
                                var productSubType_1 = item.ColumnData["Product Sub Type 1"];
                                if (productType_1 != string.Empty || productType_2 != string.Empty || productSubType_2 != string.Empty || productSubType_1 != string.Empty)
                                {
                                    var matchingHierarchy = typehierarchy.Where(a =>
             a.Div_code == div &&
             a.Subclass_code == subClassCode &&
             a.Class_code == classCode &&
             a.Dept_code == deptCode &&
             (string.IsNullOrEmpty(productType_1?.ToString().Trim()) || a.Producttype1 == productType_1.ToString().Trim()) &&
             (string.IsNullOrEmpty(productSubType_1?.ToString().Trim()) || a.Productsubtype1 == productSubType_1.ToString().Trim()) &&
             (string.IsNullOrEmpty(productType_2?.ToString().Trim()) || a.Producttype2 == productType_2.ToString().Trim()) &&
             (string.IsNullOrEmpty(productSubType_2?.ToString().Trim()) || a.Productsubtype2 == productSubType_2.ToString().Trim()));

                                    if (!matchingHierarchy.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("Product Type 1"))
                                        {
                                            item.ErrorData.Add("Product Type 1", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Product Sub Type 1"))
                                        {
                                            item.ErrorData.Add("Product Sub Type 1", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Product Type 2"))
                                        {
                                            item.ErrorData.Add("Product Type 2", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Product Sub Type 2"))
                                        {
                                            item.ErrorData.Add("Product Sub Type 2", new List<string>());
                                        }

                                        if (!item.ErrorData.ContainsKey("Hierarchy"))
                                        {
                                            item.ErrorData.Add("Hierarchy", new List<string>());
                                        }

                                        if (item.ErrorData["Hierarchy"].Count == 0)
                                        {
                                            item.ErrorData["Hierarchy"].Add($"Hierarchy {productType_1}-{productSubType_1}-{productType_2}-{productSubType_2} is mismatched");
                                        }
                                    }
                                }
                            }
                        }
                    }
                });
            }

            Parallel.ForEach(data, item =>
            {
                if (item.ColumnData.ContainsKey("Uid"))
                {
                    var uids = item.ColumnData["Uid"].ToString();
                    var uidIsActiveCheck = results.FirstOrDefault(data => data.Uid == uids);
                    if (!string.IsNullOrEmpty(uids) && !validUids.Select(uid => uid.ToLower()).Contains(uids.ToLower()))
                    {
                        if (!item.ErrorData.ContainsKey("Uid"))
                        {
                            item.ErrorData.Add("Uid", new List<string>());
                        }

                        item.ErrorData["Uid"].Add($" Invalid UID : '{uids}' ");
                    }
                    else if (!string.IsNullOrEmpty(uids) && uidIsActiveCheck.IsActive == false)
                    {
                        if (!item.ErrorData.ContainsKey("Uid"))
                        {
                            item.ErrorData.Add("Uid", new List<string>());
                        }

                        item.ErrorData["Uid"].Add($" '{uids}' is not active");
                    }
                }

                if (item.ColumnData.ContainsKey("Uid"))
                {
                    // var queryResult = await connection.QueryFirstOrDefaultAsync<string>($@"SELECT columndata ->> 'Division Code' FROM product WHERE uid ILIKE '{uid}'");
                    var uid = item.ColumnData["Uid"];
                    if (!string.IsNullOrEmpty(uid.ToString()))
                    {
                        var queryResult = columndata.Where(a => string.Equals(a.Uid, uid.ToString(), StringComparison.OrdinalIgnoreCase)).Select(a => a.Division_Code).FirstOrDefault();
                        if (queryResult != null)
                        {
                            if (queryResult != template.DivisionId.ToString())
                            {
                                if (!item.ErrorData.ContainsKey("Uid"))
                                {
                                    item.ErrorData.Add("Uid", new List<string>());
                                }

                                item.ErrorData["Uid"].Add($"Uid '{uid}' Must be in Same Division. ");
                            }
                        }
                    }
                }
            });

            if (!template.IsUidTemplate)
            {
                Parallel.ForEach(data, async item =>
                {
                    var uid = item.ColumnData["Uid"];
                    var planogram = item.ColumnData["Planogram Family Name"]?.ToString();
                    if (item.ColumnData.ContainsKey("Planogram Family Name"))
                    {
                        if (string.IsNullOrWhiteSpace(planogram))
                        {
                            if (!item.ErrorData.ContainsKey("Planogram Family Name"))
                            {
                                item.ErrorData.Add("Planogram Family Name", new List<string>());
                            }

                            item.ErrorData["Planogram Family Name"].Add("Planogram Family Name Field is required");
                        }
                        else
                        {
                            // var dbplanogram = await connection.QueryFirstOrDefaultAsync<long?>("SELECT CAST(NULLIF(columndata->>'Planogram/Family Code', '') AS BIGINT) FROM product WHERE lower(uid) = lower(@uid)",
                            //                  new { uid });
                            var pro = columndata.Where(a => string.Equals(a.Uid, uid.ToString(), StringComparison.OrdinalIgnoreCase)).Select(a => long.TryParse(a.Planogram_Family_Code, out var code) ? (long?)code : null).ToList();
                            var proAsStringList = pro.Where(value => value.HasValue).Select(value => value.ToString()).ToList();

                            var dbplanoname = planoOrFamilyNames
                                .Where(a => proAsStringList.Contains(a.PlanoCode))
                                .Select(a => new
                                {
                                    ConcatenatedColumn = a.Planogram + a.FamilyName
                                })
                                .ToList();
                            if (!dbplanoname.Any(dbValue => dbValue.ConcatenatedColumn.Trim().ToLower() == planogram.ToLower()))
                            {
                                if (!item.ErrorData.ContainsKey("Planogram Family Name"))
                                {
                                    if (!item.ErrorData.ContainsKey("Planogram Family Name"))
                                    {
                                        item.ErrorData.Add("Planogram Family Name", new List<string>());
                                    }

                                    item.ErrorData["Planogram Family Name"].Add($"Invalid Planogram Family Name '{planogram}' ");
                                }
                            }
                        }
                    }
                });
            }

            if (!template.IsUidTemplate)
            {
                Parallel.ForEach(data, async item =>
                {
                    var uids = item.ColumnData["Uid"].ToString();
                    if (item.ColumnData.ContainsKey("Product Picture"))
                    {
                        var product_picture = item.ColumnData["Product Picture"].ToString().ToLower();
                        if (string.IsNullOrWhiteSpace(item.ColumnData["Product Picture"].ToString()))
                        {
                            if (!item.ErrorData.ContainsKey("Product Picture"))
                            {
                                item.ErrorData.Add("Product Picture", new List<string>());
                            }

                            item.ErrorData["Product Picture"].Add($"Product picture field is required");
                        }
                        else if (item.ColumnData.TryGetValue("Product Picture", out var product_picture1) &&
                            product_picture1.ToString().ToLower() != "y")
                        {
                            if (!item.ErrorData.ContainsKey("Product Picture"))
                            {
                                item.ErrorData.Add("Product Picture", new List<string>());
                            }

                            item.ErrorData["Product Picture"].Add($"Invalid Product picture '{product_picture}'");
                        }
                        else
                        {
                            if (item.ColumnData.ContainsKey("Uid"))
                            {
                                // var productss = await connection.QueryFirstOrDefaultAsync<Product>(
                                //    "SELECT id FROM Product WHERE lower(uid) = @UID", new { UID = uids.ToLower() });
                                var productss = results.Where(a => string.Equals(a.Uid, uids, StringComparison.OrdinalIgnoreCase)).Select(a => a.Id).FirstOrDefault();

                                if (productss != null)
                                {
                                    // var productpicture1 = await connection.QueryFirstOrDefaultAsync<Product>(
                                    // "SELECT pictureid FROM Product WHERE lower(uid) = @UID", new { UID = uids.ToLower() });
                                    var productpicture1 = results.Where(a => string.Equals(a.Uid, uids, StringComparison.OrdinalIgnoreCase)).Select(a => a.PictureId).FirstOrDefault();

                                    // var pictureId = await connection.QueryFirstOrDefaultAsync<int>(
                                    //    "SELECT id FROM uploadpicture WHERE id = @id", new { id = productpicture1.PictureId });
                                    var pictureId = picture.Where(a => a.Id == productpicture1).Select(a => a.Id).FirstOrDefault();

                                    if (pictureId != 0)
                                    {
                                        // var duplicateProduct = await connection.QueryFirstOrDefaultAsync<Product>(
                                        // "SELECT * FROM Product WHERE pictureid = @PictureId", new { PictureId = pictureId });
                                        var duplicateProduct = results.Where(a => a.PictureId == pictureId).FirstOrDefault();

                                        if (duplicateProduct != null && productss != null && duplicateProduct.Id != productss)
                                        {
                                            if (!item.ErrorData.ContainsKey("Product Picture"))
                                            {
                                                item.ErrorData.Add("Product Picture", new List<string>());
                                            }

                                            item.ErrorData["Product Picture"].Add($"Product picture '{product_picture}' is already used.");
                                        }
                                    }
                                    else
                                    {
                                        if (!item.ErrorData.ContainsKey("Product Picture"))
                                        {
                                            item.ErrorData.Add("Product Picture", new List<string>());
                                        }

                                        item.ErrorData["Product Picture"].Add($"Product picture Not Found");
                                    }
                                }
                            }
                        }
                    }
                });
            }

            // Buyer Template 1
            if (!template.IsUidTemplate)
            {
                Parallel.ForEach(data, item =>
                {
                    // if (item.ColumnData.ContainsKey("Uid"))
                    // {
                    //    var uids = item.ColumnData["Uid"].ToString();
                    //    var uidIsActiveCheck = results.FirstOrDefault(data => data.Uid == uids);
                    //    if (!string.IsNullOrEmpty(uids) && uidIsActiveCheck.IsActive == false)
                    //    {
                    //        if (!item.ErrorData.ContainsKey("Uid"))
                    //        {
                    //            item.ErrorData.Add("Uid", new List<string>());
                    //        }

                    ////        item.ErrorData["Uid"].Add($"'{uids}' is not active ");
                    //    }
                    //    else if (!string.IsNullOrEmpty(uids) && !validUids.Select(uid => uid.ToLower()).Contains(uids.ToLower()))
                    //    {
                    //        if (!item.ErrorData.ContainsKey("Uid"))
                    //        {
                    //            item.ErrorData.Add("Uid", new List<string>());
                    //        }

                    ////        item.ErrorData["Uid"].Add($" Invalid UID : '{uids}' ");
                    //    }
                    // }
                    if (item.ColumnData.ContainsKey("RMS Season Code"))
                    {
                        var rmsseason = item.ColumnData["RMS Season Code"];
                        if (string.IsNullOrWhiteSpace(rmsseason.ToString()))
                        {
                            if (!item.ErrorData.ContainsKey("RMS Season Code"))
                            {
                                item.ErrorData.Add("RMS Season Code", new List<string>());
                            }

                            item.ErrorData["RMS Season Code"].Add("RMS Season Code Field is required");
                        }
                        else
                        {
                            if (rmsseason is string)
                            {
                                if (!item.ErrorData.ContainsKey("RMS Season Code"))
                                {
                                    item.ErrorData.Add("RMS Season Code", new List<string>());
                                }

                                item.ErrorData["RMS Season Code"].Add($"Special characters are present in RMS Season Code '{rmsseason}'");
                            }
                            else
                            {
                                var rms_season = mtrrmsseasoncode.Where(a => a.Code.ToString() == rmsseason.ToString()).ToList();

                                if (!rms_season.Any())
                                {
                                    if (!item.ErrorData.ContainsKey("RMS Season Code"))
                                    {
                                        item.ErrorData.Add("RMS Season Code", new List<string>());
                                    }

                                    item.ErrorData["RMS Season Code"].Add($"Invalid RMS Season Code '{rmsseason}'");
                                }
                            }
                        }
                    }

                    if (item.ColumnData.ContainsKey("Brand Code"))
                    {
                        var brandCode = item.ColumnData["Brand Code"];
                        if (string.IsNullOrWhiteSpace(brandCode.ToString()))
                        {
                            if (!item.ErrorData.ContainsKey("Brand Code"))
                            {
                                item.ErrorData.Add("Brand Code", new List<string>());
                            }

                            item.ErrorData["Brand Code"].Add("Brand Code Field is required");
                        }
                        else
                        {
                            if (brandCode is string)
                            {
                                if (!item.ErrorData.ContainsKey("Brand Code"))
                                {
                                    item.ErrorData.Add("Brand Code", new List<string>());
                                }

                                item.ErrorData["Brand Code"].Add($"Special characters are present in Brand Code '{brandCode}'");
                            }
                            else
                            {
                                var brand_code = brand.Where(a => a.BrandCode.ToString() == brandCode.ToString()).ToList();

                                if (!brand_code.Any())
                                {
                                    if (!item.ErrorData.ContainsKey("Brand Code"))
                                    {
                                        item.ErrorData.Add("Brand Code", new List<string>());
                                    }

                                    item.ErrorData["Brand Code"].Add($"Invalid Brand Code '{brandCode}'");
                                }
                            }
                        }
                    }

                    if (item.ColumnData.ContainsKey("Brand Type"))
                    {
                        var brand_type = item.ColumnData["Brand Type"];

                        if (string.IsNullOrWhiteSpace(brand_type.ToString()))
                        {
                            if (!item.ErrorData.ContainsKey("Brand Type"))
                            {
                                item.ErrorData.Add("Brand Type", new List<string>());
                            }

                            item.ErrorData["Brand Type"].Add("Brand Type Field is Required");
                        }
                        else
                        {
                            var brandTypeVal = mtrbrandType.Where(a => string.Equals(a.Name, brand_type.ToString(), StringComparison.OrdinalIgnoreCase)).ToList();
                            if (!brandTypeVal.Any())
                            {
                                if (!item.ErrorData.ContainsKey("Brand Type"))
                                {
                                    item.ErrorData.Add("Brand Type", new List<string>());
                                }

                                item.ErrorData["Brand Type"].Add($"Invalid Brand Type '{brand_type}'");
                            }
                        }
                    }

                    if (item.ColumnData.ContainsKey("Country Of Origin"))
                    {
                        var countryOfOriginObject = item.ColumnData.ContainsKey("Country Of Origin")
                         ? item.ColumnData["Country Of Origin"]
                         : null;

                        var countryOfOriginName = countryOfOriginObject != null
                                                   ? countryOfOriginObject.ToString()?.ToUpperInvariant()
                                                   : null;

                        if (string.IsNullOrWhiteSpace(countryOfOriginName))
                        {
                            if (!item.ErrorData.ContainsKey("Country Of Origin"))
                            {
                                item.ErrorData.Add("Country Of Origin", new List<string>());
                            }

                            item.ErrorData["Country Of Origin"].Add("Country Of Origin Field is required");
                        }
                        else
                        {
                            var country_of_origin = countryOfOrigin.Where(a => a.Name == countryOfOriginName).ToList();
                            if (!country_of_origin.Any())
                            {
                                if (!item.ErrorData.ContainsKey("Country Of Origin"))
                                {
                                    item.ErrorData.Add("Country Of Origin", new List<string>());
                                }

                                item.ErrorData["Country Of Origin"].Add($"Invalid Country Of Origin '{countryOfOriginName}'");
                            }
                        }
                    }

                    if (item.ColumnData.ContainsKey("Purchase Type"))
                    {
                        var purchaseTypeStr = item.ColumnData["Purchase Type"];

                        if (string.IsNullOrWhiteSpace(purchaseTypeStr.ToString()))
                        {
                            if (!item.ErrorData.ContainsKey("Purchase Type"))
                            {
                                item.ErrorData.Add("Purchase Type", new List<string>());
                            }

                            item.ErrorData["Purchase Type"].Add("Purchase Type Field is required");
                        }
                        else
                        {
                            if (purchaseTypeStr is string)
                            {
                                if (!item.ErrorData.ContainsKey("Purchase Type"))
                                {
                                    item.ErrorData.Add("Purchase Type", new List<string>());
                                }

                                item.ErrorData["Purchase Type"].Add($"Special characters are present in Purchase Type '{purchaseTypeStr}'");
                            }
                            else
                            {
                                var purchase_type = purchasetype.Where(a => a.Code.ToString() == purchaseTypeStr.ToString()).ToList();

                                if (!purchase_type.Any())
                                {
                                    if (!item.ErrorData.ContainsKey("Purchase Type"))
                                    {
                                        item.ErrorData.Add("Purchase Type", new List<string>());
                                    }

                                    item.ErrorData["Purchase Type"].Add($"Invalid Purchase Type '{purchaseTypeStr}'");
                                }
                                else
                                {
                                    var mtrpurchasetype = purchasetype.Where(a => a.Code.ToString() == purchaseTypeStr.ToString() && a.DivisionId.Split(',').Select(d => d.Trim()).Contains(templateDivisionId)).ToList();
                                    if (!mtrpurchasetype.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("Purchase Type"))
                                        {
                                            item.ErrorData.Add("Purchase Type", new List<string>());
                                        }

                                        item.ErrorData["Purchase Type"].Add($"Division Mismatched for Purchase Type {purchaseTypeStr}");
                                    }
                                }
                            }
                        }
                    }

                    if (item.ColumnData.ContainsKey("Product Classification code"))
                    {
                        var productClassificationCodeStr = item.ColumnData["Product Classification code"];

                        if (productClassificationCodeStr != null && decimal.TryParse(productClassificationCodeStr.ToString(), out decimal productValue))
                        {
                            if (productValue <= 0 || productValue % 1 != 0)
                            {
                                if (!item.ErrorData.ContainsKey("Product Classification code"))
                                {
                                    item.ErrorData.Add("Product Classification code", new List<string>());
                                }

                                item.ErrorData["Product Classification code"].Add("Product Classification code Field is expected to be a number");
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(productClassificationCodeStr.ToString()))
                        {
                            if (productClassificationCodeStr is string)
                            {
                                if (!item.ErrorData.ContainsKey("Product Classification code"))
                                {
                                    item.ErrorData.Add("Product Classification code", new List<string>());
                                }

                                item.ErrorData["Product Classification code"].Add($"Special characters are present in Product Classification code '{productClassificationCodeStr}'");
                            }
                            else
                            {
                                var purchase_classification = productClassification.Where(a => a.Code.ToString() == productClassificationCodeStr.ToString()).ToList();

                                if (!purchase_classification.Any())
                                {
                                    if (!item.ErrorData.ContainsKey("Product Classification code"))
                                    {
                                        item.ErrorData.Add("Product Classification code", new List<string>());
                                    }

                                    item.ErrorData["Product Classification code"].Add($"Invalid Product Classification code '{productClassificationCodeStr}'");
                                }
                                else
                                {
                                    var mtrproductclassification = productClassification.Where(a => a.Code.ToString() == productClassificationCodeStr.ToString() && a.DivisionId.Split(',').Select(d => d.Trim()).Contains(templateDivisionId)).ToList();
                                    if (!mtrproductclassification.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("Product Classification code"))
                                        {
                                            item.ErrorData.Add("Product Classification code", new List<string>());
                                        }

                                        item.ErrorData["Product Classification code"].Add($"Division Mismatched for Product Classification code {productClassificationCodeStr}");
                                    }
                                }
                            }
                        }
                    }

                    if (item.ColumnData.ContainsKey("PLC Code"))
                    {
                        var pLCCodeStr = item.ColumnData["PLC Code"];

                        if (pLCCodeStr != null && decimal.TryParse(pLCCodeStr.ToString(), out decimal plcValue))
                        {
                            if (plcValue <= 0 || plcValue % 1 != 0)
                            {
                                if (!item.ErrorData.ContainsKey("PLC Code"))
                                {
                                    item.ErrorData.Add("PLC Code", new List<string>());
                                }

                                item.ErrorData["PLC Code"].Add("PLC Code Field is expected to be a number");
                            }
                        }

                        if (string.IsNullOrWhiteSpace(pLCCodeStr.ToString()))
                        {
                            if (!item.ErrorData.ContainsKey("PLC Code"))
                            {
                                item.ErrorData.Add("PLC Code", new List<string>());
                            }

                            item.ErrorData["PLC Code"].Add("PLC Code Field is required");
                        }
                        else
                        {
                            if (pLCCodeStr is string)
                            {
                                if (!item.ErrorData.ContainsKey("PLC Code"))
                                {
                                    item.ErrorData.Add("PLC Code", new List<string>());
                                }

                                item.ErrorData["PLC Code"].Add($"Special characters are present in PLC Code '{pLCCodeStr}'");
                            }
                            else
                            {
                                var plc_code = plcCode.Where(a => a.Code.ToString() == pLCCodeStr.ToString()).ToList();
                                if (!plc_code.Any())
                                {
                                    if (!item.ErrorData.ContainsKey("PLC Code"))
                                    {
                                        item.ErrorData.Add("PLC Code", new List<string>());
                                    }

                                    item.ErrorData["PLC Code"].Add($"Invalid PLC Code '{pLCCodeStr}'");
                                }
                                else
                                {
                                    var mtrplccode = plcCode.Where(a => a.Code.ToString() == pLCCodeStr.ToString() && a.DivisionId.Split(',').Select(d => d.Trim()).Contains(templateDivisionId)).ToList();
                                    if (!mtrplccode.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("PLC Code"))
                                        {
                                            item.ErrorData.Add("PLC Code", new List<string>());
                                        }

                                        item.ErrorData["PLC Code"].Add($"Division Mismatched for PLC Code {pLCCodeStr}");
                                    }
                                }
                            }
                        }
                    }

                    if (item.ColumnData.ContainsKey("Product Grouping"))
                    {
                        var productGroupingCodeStr = item.ColumnData["Product Grouping"];

                        if (string.IsNullOrWhiteSpace(productGroupingCodeStr.ToString()))
                        {
                            if (!item.ErrorData.ContainsKey("Product Grouping"))
                            {
                                item.ErrorData.Add("Product Grouping", new List<string>());
                            }

                            item.ErrorData["Product Grouping"].Add("Product Grouping Field is required");
                        }
                        else
                        {
                            if (productGroupingCodeStr is string)
                            {
                                if (!item.ErrorData.ContainsKey("Product Grouping"))
                                {
                                    item.ErrorData.Add("Product Grouping", new List<string>());
                                }

                                item.ErrorData["Product Grouping"].Add($"Special characters are present in Product Grouping '{productGroupingCodeStr}'");
                            }
                            else if (long.TryParse(productGroupingCodeStr.ToString(), out var productGroupingCode))
                            {
                                var product_grouping = productGrouping.Where(a => a.Code == productGroupingCode).ToList();

                                if (!product_grouping.Any())
                                {
                                    if (!item.ErrorData.ContainsKey("Product Grouping"))
                                    {
                                        item.ErrorData.Add("Product Grouping", new List<string>());
                                    }

                                    item.ErrorData["Product Grouping"].Add($"Invalid Product Grouping '{productGroupingCode}'");
                                }
                                else
                                {
                                    var mtrproductgrouping = productGrouping.Where(a => a.Code.ToString() == productGroupingCodeStr.ToString() && a.DivisionId.Split(',').Select(d => d.Trim()).Contains(templateDivisionId)).ToList();
                                    if (!mtrproductgrouping.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("Product Grouping"))
                                        {
                                            item.ErrorData.Add("Product Grouping", new List<string>());
                                        }

                                        item.ErrorData["Product Grouping"].Add($"Division Mismatched for Product Grouping {productGroupingCodeStr}");
                                    }
                                }
                            }
                        }
                    }

                    if (templateDivisionId == "4" || templateDivisionId == "65")
                    {
                        if (item.ColumnData.ContainsKey("Packaging Product size (H)"))
                        {
                            var packagingdimensionL = item.ColumnData["Packaging Product size (H)"];

                            // if (packagingdimensionL != null && decimal.TryParse(packagingdimensionL.ToString(), out decimal dimensionsLValue))
                            // {
                            //    if (dimensionsLValue <= 0 || dimensionsLValue % 1 != 0)
                            //    {
                            //        if (!item.ErrorData.ContainsKey("Packaging Product size (H)"))
                            //        {
                            //            item.ErrorData.Add("Packaging Product size (H)", new List<string>());
                            //        }
                            //         item.ErrorData["Packaging Product size (H)"].Add("Packaging Product size (H) Field is expected to be a number");
                            //    }
                            // }
                            if (!string.IsNullOrWhiteSpace(packagingdimensionL.ToString()))
                            {
                                if (packagingdimensionL is string)
                                {
                                    if (!item.ErrorData.ContainsKey("Packaging Product size (H)"))
                                    {
                                        item.ErrorData.Add("Packaging Product size (H)", new List<string>());
                                    }

                                    item.ErrorData["Packaging Product size (H)"].Add($"Special characters are present in Packaging Product size (H) '{packagingdimensionL}'");
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Packaging Product size (W)"))
                        {
                            var packagingdimensionW = item.ColumnData["Packaging Product size (W)"];

                            // if (packagingdimensionW != null && decimal.TryParse(packagingdimensionW.ToString(), out decimal dimensionsWValue))
                            // {
                            //    if (dimensionsWValue <= 0 || dimensionsWValue % 1 != 0)
                            //    {
                            //        if (!item.ErrorData.ContainsKey("Packaging Product size (W)"))
                            //        {
                            //            item.ErrorData.Add("Packaging Product size (W)", new List<string>());
                            //        }
                            //        item.ErrorData["Packaging Product size (W)"].Add("Packaging Product size (W) Field is expected to be a number");
                            //    }
                            // }
                            if (!string.IsNullOrWhiteSpace(packagingdimensionW.ToString()))
                            {
                                if (packagingdimensionW is string)
                                {
                                    if (!item.ErrorData.ContainsKey("Packaging Product size (W)"))
                                    {
                                        item.ErrorData.Add("Packaging Product size (W)", new List<string>());
                                    }

                                    item.ErrorData["Packaging Product size (W)"].Add($"Special characters are present in Packaging Product size (W) '{packagingdimensionW}'");
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Packaging Product size (L)"))
                        {
                            var packagingdimensionH = item.ColumnData["Packaging Product size (L)"];

                            // if (packagingdimensionH != null && decimal.TryParse(packagingdimensionH.ToString(), out decimal dimensionsHValue))
                            // {
                            //    if (dimensionsHValue <= 0 || dimensionsHValue % 1 != 0)
                            //    {
                            //        if (!item.ErrorData.ContainsKey("Packaging Product size (L)"))
                            //        {
                            //            item.ErrorData.Add("Packaging Product size (L)", new List<string>());
                            //        }
                            //         item.ErrorData["Packaging Product size (L)"].Add("Packaging Product size (L) Field is expected to be a number");
                            //    }
                            // }
                            if (!string.IsNullOrWhiteSpace(packagingdimensionH.ToString()))
                            {
                                if (packagingdimensionH is string)
                                {
                                    if (!item.ErrorData.ContainsKey("Packaging Product size (L)"))
                                    {
                                        item.ErrorData.Add("Packaging Product size (L)", new List<string>());
                                    }

                                    item.ErrorData["Packaging Product size (L)"].Add($"Special characters are present in Packaging Product size (L) '{packagingdimensionH}'");
                                }
                            }
                        }
                    }

                    if (templateDivisionId == "61")
                    {
                        if (item.ColumnData.ContainsKey("Packaging Type"))
                        {
                            var packagingTypeCodeObj = item.ColumnData["Packaging Type"];

                            if (string.IsNullOrWhiteSpace(packagingTypeCodeObj.ToString()))
                            {
                                if (!item.ErrorData.ContainsKey("Packaging Type"))
                                {
                                    item.ErrorData.Add("Packaging Type", new List<string>());
                                }

                                item.ErrorData["Packaging Type"].Add("Packaging Type Field is required");
                            }
                            else
                            {
                                if (packagingTypeCodeObj is string)
                                {
                                    if (!item.ErrorData.ContainsKey("Packaging Type"))
                                    {
                                        item.ErrorData.Add("Packaging Type", new List<string>());
                                    }

                                    item.ErrorData["Packaging Type"].Add($"Special characters are present in Packaging Type '{packagingTypeCodeObj}'");
                                }
                                else
                                {
                                    var packaging_type = packagingtype.Where(a => a.PackagingTypeCode.ToString() == packagingTypeCodeObj.ToString()).ToList();

                                    if (!packaging_type.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("Packaging Type"))
                                        {
                                            item.ErrorData.Add("Packaging Type", new List<string>());
                                        }

                                        item.ErrorData["Packaging Type"].Add($"Invalid Packaging Type '{packagingTypeCodeObj}'");
                                    }
                                    else
                                    {
                                        var mtrpackaging = packagingtype.Where(a => a.PackagingTypeCode.ToString() == packagingTypeCodeObj.ToString() && a.DivisionId.Split(',').Select(d => d.Trim()).Contains(templateDivisionId)).ToList();
                                        if (!mtrpackaging.Any())
                                        {
                                            if (!item.ErrorData.ContainsKey("Packaging Type"))
                                            {
                                                item.ErrorData.Add("Packaging Type", new List<string>());
                                            }

                                            item.ErrorData["Packaging Type"].Add($"Division Mismatched for Packaging Type {packagingTypeCodeObj}");
                                        }
                                    }
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Product Gross Weight (GMS)"))
                        {
                            var grossWeightGMS = item.ColumnData["Product Gross Weight (GMS)"];

                            // if (grossWeightGMS != null && decimal.TryParse(grossWeightGMS.ToString(), out decimal grossValue))
                            // {
                            //    if (grossValue <= 0 || grossValue % 1 != 0)
                            //    {
                            //        if (!item.ErrorData.ContainsKey("Product Gross Weight (GMS)"))
                            //        {
                            //            item.ErrorData.Add("Product Gross Weight (GMS)", new List<string>());
                            //        }
                            //        item.ErrorData["Product Gross Weight (GMS)"].Add("Product Gross Weight (GMS) Field is expected to be a number");
                            //    }
                            // }
                            if (string.IsNullOrWhiteSpace(grossWeightGMS.ToString()))
                            {
                                if (!item.ErrorData.ContainsKey("Product Gross Weight (GMS)"))
                                {
                                    item.ErrorData.Add("Product Gross Weight (GMS)", new List<string>());
                                }

                                item.ErrorData["Product Gross Weight (GMS)"].Add("Product Gross Weight (GMS) Field is required");
                            }
                            else
                            {
                                if (grossWeightGMS is string)
                                {
                                    if (!item.ErrorData.ContainsKey("Product Gross Weight (GMS)"))
                                    {
                                        item.ErrorData.Add("Product Gross Weight (GMS)", new List<string>());
                                    }

                                    item.ErrorData["Product Gross Weight (GMS)"].Add($"Special characters are present in Product Gross Weight (GMS) '{grossWeightGMS}'");
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Packaging Product size (H)"))
                        {
                            var packagingdimensionL = item.ColumnData["Packaging Product size (H)"];

                            // if (packagingdimensionL != null && decimal.TryParse(packagingdimensionL.ToString(), out decimal dimensionsLValue))
                            // {
                            //    if (dimensionsLValue <= 0 || dimensionsLValue % 1 != 0)
                            //    {
                            //        if (!item.ErrorData.ContainsKey("Packaging Product size (H)"))
                            //        {
                            //            item.ErrorData.Add("Packaging Product size (H)", new List<string>());
                            //        }
                            //        item.ErrorData["Packaging Product size (H)"].Add("Packaging Product size (H) Field is expected to be a number");
                            //    }
                            // }
                            if (string.IsNullOrWhiteSpace(packagingdimensionL.ToString()))
                            {
                                if (!item.ErrorData.ContainsKey("Packaging Product size (H)"))
                                {
                                    item.ErrorData.Add("Packaging Product size (H)", new List<string>());
                                }

                                item.ErrorData["Packaging Product size (H)"].Add("Packaging Product size (H) Field is required");
                            }
                            else
                            {
                                if (packagingdimensionL is string)
                                {
                                    if (!item.ErrorData.ContainsKey("Packaging Product size (H)"))
                                    {
                                        item.ErrorData.Add("Packaging Product size (H)", new List<string>());
                                    }

                                    item.ErrorData["Packaging Product size (H)"].Add($"Special characters are present in Packaging Product size (H) '{packagingdimensionL}'");
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Packaging Product size (W)"))
                        {
                            var packagingdimensionW = item.ColumnData["Packaging Product size (W)"];

                            // if (packagingdimensionW != null && decimal.TryParse(packagingdimensionW.ToString(), out decimal dimensionsWValue))
                            // {
                            //    if (dimensionsWValue <= 0 || dimensionsWValue % 1 != 0)
                            //    {
                            //        if (!item.ErrorData.ContainsKey("Packaging Product size (W)"))
                            //        {
                            //            item.ErrorData.Add("Packaging Product size (W)", new List<string>());
                            //        }
                            //        item.ErrorData["Packaging Product size (W)"].Add("Packaging Product size (W) Field is expected to be a number");
                            //    }
                            // }
                            if (string.IsNullOrWhiteSpace(packagingdimensionW.ToString()))
                            {
                                if (!item.ErrorData.ContainsKey("Packaging Product size (W)"))
                                {
                                    item.ErrorData.Add("Packaging Product size (W)", new List<string>());
                                }

                                item.ErrorData["Packaging Product size (W)"].Add("Packaging Product size (W) Field is required");
                            }
                            else
                            {
                                if (packagingdimensionW is string)
                                {
                                    if (!item.ErrorData.ContainsKey("Packaging Product size (W)"))
                                    {
                                        item.ErrorData.Add("Packaging Product size (W)", new List<string>());
                                    }

                                    item.ErrorData["Packaging Product size (W)"].Add($"Special characters are present in Packaging Product size (W) '{packagingdimensionW}'");
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Packaging Product size (L)"))
                        {
                            var packagingdimensionH = item.ColumnData["Packaging Product size (L)"];

                            // if (packagingdimensionH != null && decimal.TryParse(packagingdimensionH.ToString(), out decimal dimensionsHValue))
                            // {
                            //    if (dimensionsHValue <= 0 || dimensionsHValue % 1 != 0)
                            //    {
                            //        if (!item.ErrorData.ContainsKey("Packaging Product size (L)"))
                            //        {
                            //            item.ErrorData.Add("Packaging Product size (L)", new List<string>());
                            //        }
                            //        item.ErrorData["Packaging Product size (L)"].Add("Packaging Product size (L) Field is expected to be a number");
                            //    }
                            // }
                            if (string.IsNullOrWhiteSpace(packagingdimensionH.ToString()))
                            {
                                if (!item.ErrorData.ContainsKey("Packaging Product size (L)"))
                                {
                                    item.ErrorData.Add("Packaging Product size (L)", new List<string>());
                                }

                                item.ErrorData["Packaging Product size (L)"].Add("Packaging Product size (L) Field is required");
                            }
                            else
                            {
                                if (packagingdimensionH is string)
                                {
                                    if (!item.ErrorData.ContainsKey("Packaging Product size (L)"))
                                    {
                                        item.ErrorData.Add("Packaging Product size (L)", new List<string>());
                                    }

                                    item.ErrorData["Packaging Product size (L)"].Add($"Special characters are present in Packaging Product size (L) '{packagingdimensionH}'");
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Inner Pack Dimensions L (CM)"))
                        {
                            var innerPackL = item.ColumnData["Inner Pack Dimensions L (CM)"];

                            // if (innerPackL != null && decimal.TryParse(innerPackL.ToString(), out decimal innerpackLValue))
                            // {
                            //    if (innerpackLValue <= 0 || innerpackLValue % 1 != 0)
                            //    {
                            //        if (!item.ErrorData.ContainsKey("Inner Pack Dimensions L (CM)"))
                            //        {
                            //            item.ErrorData.Add("Inner Pack Dimensions L (CM)", new List<string>());
                            //        }
                            //         item.ErrorData["Inner Pack Dimensions L (CM)"].Add("Inner Pack Dimensions L (CM) Field is expected to be a number");
                            //    }
                            // }
                            if (string.IsNullOrWhiteSpace(innerPackL.ToString()))
                            {
                                if (!item.ErrorData.ContainsKey("Inner Pack Dimensions L (CM)"))
                                {
                                    item.ErrorData.Add("Inner Pack Dimensions L (CM)", new List<string>());
                                }

                                item.ErrorData["Inner Pack Dimensions L (CM)"].Add("Inner Pack Dimensions L (CM) Field is required");
                            }
                            else
                            {
                                if (innerPackL is string)
                                {
                                    if (!item.ErrorData.ContainsKey("Inner Pack Dimensions L (CM)"))
                                    {
                                        item.ErrorData.Add("Inner Pack Dimensions L (CM)", new List<string>());
                                    }

                                    item.ErrorData["Inner Pack Dimensions L (CM)"].Add($"Special characters are present in Inner Pack Dimensions L (CM) '{innerPackL}'");
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Inner Pack Dimensions W (CM)"))
                        {
                            var innerPackW = item.ColumnData["Inner Pack Dimensions W (CM)"];

                            // if (innerPackW != null && decimal.TryParse(innerPackW.ToString(), out decimal innerpackWValue))
                            // {
                            //    if (innerpackWValue <= 0 || innerpackWValue % 1 != 0)
                            //    {
                            //        if (!item.ErrorData.ContainsKey("Inner Pack Dimensions W (CM)"))
                            //        {
                            //            item.ErrorData.Add("Inner Pack Dimensions W (CM)", new List<string>());
                            //        }
                            //        item.ErrorData["Inner Pack Dimensions W (CM)"].Add("Inner Pack Dimensions W (CM) Field is expected to be a number");
                            //    }
                            // }
                            if (string.IsNullOrWhiteSpace(innerPackW.ToString()))
                            {
                                if (!item.ErrorData.ContainsKey("Inner Pack Dimensions W (CM)"))
                                {
                                    item.ErrorData.Add("Inner Pack Dimensions W (CM)", new List<string>());
                                }

                                item.ErrorData["Inner Pack Dimensions W (CM)"].Add("Inner Pack Dimensions W (CM) Field is required");
                            }
                            else
                            {
                                if (innerPackW is string)
                                {
                                    if (!item.ErrorData.ContainsKey("Inner Pack Dimensions W (CM)"))
                                    {
                                        item.ErrorData.Add("Inner Pack Dimensions W (CM)", new List<string>());
                                    }

                                    item.ErrorData["Inner Pack Dimensions W (CM)"].Add($"Special characters are present in Inner Pack Dimensions W (CM) '{innerPackW}'");
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Inner Pack Dimensions H (CM)"))
                        {
                            var innerPackH = item.ColumnData["Inner Pack Dimensions H (CM)"];

                            // if (innerPackH != null && decimal.TryParse(innerPackH.ToString(), out decimal innerpackHValue))
                            // {
                            //    if (innerpackHValue <= 0 || innerpackHValue % 1 != 0)
                            //    {
                            //        if (!item.ErrorData.ContainsKey("Inner Pack Dimensions H (CM)"))
                            //        {
                            //            item.ErrorData.Add("Inner Pack Dimensions H (CM)", new List<string>());
                            //        }
                            //        item.ErrorData["Inner Pack Dimensions H (CM)"].Add("Inner Pack Dimensions H (CM) Field is expected to be a number");
                            //    }
                            // }
                            if (string.IsNullOrWhiteSpace(innerPackH.ToString()))
                            {
                                if (!item.ErrorData.ContainsKey("Inner Pack Dimensions H (CM)"))
                                {
                                    item.ErrorData.Add("Inner Pack Dimensions H (CM)", new List<string>());
                                }

                                item.ErrorData["Inner Pack Dimensions H (CM)"].Add("Inner Pack Dimensions H (CM) Field is required");
                            }
                            else
                            {
                                if (innerPackH is string)
                                {
                                    if (!item.ErrorData.ContainsKey("Inner Pack Dimensions H (CM)"))
                                    {
                                        item.ErrorData.Add("Inner Pack Dimensions H (CM)", new List<string>());
                                    }

                                    item.ErrorData["Inner Pack Dimensions H (CM)"].Add($"Special characters are present in Inner Pack Dimensions H (CM) '{innerPackH}'");
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Master Carton Dimensions L (CM)"))
                        {
                            var masterCartonL = item.ColumnData["Master Carton Dimensions L (CM)"];

                            // if (masterCartonL != null && decimal.TryParse(masterCartonL.ToString(), out decimal masterLValue))
                            // {
                            //    if (masterLValue <= 0 || masterLValue % 1 != 0)
                            //    {
                            //        if (!item.ErrorData.ContainsKey("Master Carton Dimensions L (CM)"))
                            //        {
                            //            item.ErrorData.Add("Master Carton Dimensions L (CM)", new List<string>());
                            //        }
                            //        item.ErrorData["Master Carton Dimensions L (CM)"].Add("Master Carton Dimensions L (CM) Field is expected to be a number");
                            //    }
                            // }
                            if (string.IsNullOrWhiteSpace(masterCartonL.ToString()))
                            {
                                if (!item.ErrorData.ContainsKey("Master Carton Dimensions L (CM)"))
                                {
                                    item.ErrorData.Add("Master Carton Dimensions L (CM)", new List<string>());
                                }

                                item.ErrorData["Master Carton Dimensions L (CM)"].Add("Master Carton Dimensions L (CM) Field is required");
                            }
                            else
                            {
                                if (masterCartonL is string)
                                {
                                    if (!item.ErrorData.ContainsKey("Master Carton Dimensions L (CM)"))
                                    {
                                        item.ErrorData.Add("Master Carton Dimensions L (CM)", new List<string>());
                                    }

                                    item.ErrorData["Master Carton Dimensions L (CM)"].Add($"Special characters are present in Master Carton Dimensions L (CM) '{masterCartonL}'");
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Master Carton Dimensions W (CM)"))
                        {
                            var masterCartonW = item.ColumnData["Master Carton Dimensions W (CM)"];

                            // if (masterCartonW != null && decimal.TryParse(masterCartonW.ToString(), out decimal masterWValue))
                            // {
                            //    if (masterWValue <= 0 || masterWValue % 1 != 0)
                            //    {
                            //        if (!item.ErrorData.ContainsKey("Master Carton Dimensions W (CM)"))
                            //        {
                            //            item.ErrorData.Add("Master Carton Dimensions W (CM)", new List<string>());
                            //        }
                            //        item.ErrorData["Master Carton Dimensions W (CM)"].Add("Master Carton Dimensions W (CM) Field is expected to be a number");
                            //    }
                            // }
                            if (string.IsNullOrWhiteSpace(masterCartonW.ToString()))
                            {
                                if (!item.ErrorData.ContainsKey("Master Carton Dimensions W (CM)"))
                                {
                                    item.ErrorData.Add("Master Carton Dimensions W (CM)", new List<string>());
                                }

                                item.ErrorData["Master Carton Dimensions W (CM)"].Add("Master Carton Dimensions W (CM) Field is required");
                            }
                            else
                            {
                                if (masterCartonW is string)
                                {
                                    if (!item.ErrorData.ContainsKey("Master Carton Dimensions W (CM)"))
                                    {
                                        item.ErrorData.Add("Master Carton Dimensions W (CM)", new List<string>());
                                    }

                                    item.ErrorData["Master Carton Dimensions W (CM)"].Add($"Special characters are present in Master Carton Dimensions W (CM) '{masterCartonW}'");
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Master Carton Dimensions H (CM)"))
                        {
                            var masterCartonH = item.ColumnData["Master Carton Dimensions H (CM)"];

                            // if (masterCartonH != null && decimal.TryParse(masterCartonH.ToString(), out decimal masterHValue))
                            // {
                            //    if (masterHValue <= 0 || masterHValue % 1 != 0)
                            //    {
                            //        if (!item.ErrorData.ContainsKey("Master Carton Dimensions H (CM)"))
                            //        {
                            //            item.ErrorData.Add("Master Carton Dimensions H (CM)", new List<string>());
                            //        }
                            //        item.ErrorData["Master Carton Dimensions H (CM)"].Add("Master Carton Dimensions H (CM) Field is expected to be a number");
                            //    }
                            // }
                            if (string.IsNullOrWhiteSpace(masterCartonH.ToString()))
                            {
                                if (!item.ErrorData.ContainsKey("Master Carton Dimensions H (CM)"))
                                {
                                    item.ErrorData.Add("Master Carton Dimensions H (CM)", new List<string>());
                                }

                                item.ErrorData["Master Carton Dimensions H (CM)"].Add("Master Carton Dimensions H (CM) Field is required");
                            }
                            else
                            {
                                if (masterCartonH is string)
                                {
                                    if (!item.ErrorData.ContainsKey("Master Carton Dimensions H (CM)"))
                                    {
                                        item.ErrorData.Add("Master Carton Dimensions H (CM)", new List<string>());
                                    }

                                    item.ErrorData["Master Carton Dimensions H (CM)"].Add($"Special characters are present in Master Carton Dimensions H (CM) '{masterCartonH}'");
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Net Weight Inner Carton"))
                        {
                            var netWeight = item.ColumnData["Net Weight Inner Carton"];
                            if (netWeight != null && decimal.TryParse(netWeight.ToString(), out decimal netWeightInnerValue))
                            {
                                if (netWeightInnerValue <= 0 || netWeightInnerValue % 1 != 0)
                                {
                                    if (!item.ErrorData.ContainsKey("Net Weight Inner Carton"))
                                    {
                                        item.ErrorData.Add("Net Weight Inner Carton", new List<string>());
                                    }

                                    item.ErrorData["Net Weight Inner Carton"].Add("Net Weight Inner Carton Field is expected to be a number");
                                }
                            }

                            if (string.IsNullOrWhiteSpace(netWeight.ToString()))
                            {
                                if (!item.ErrorData.ContainsKey("Net Weight Inner Carton"))
                                {
                                    item.ErrorData.Add("Net Weight Inner Carton", new List<string>());
                                }

                                item.ErrorData["Net Weight Inner Carton"].Add("Net Weight Inner Carton Field is required");
                            }
                            else
                            {
                                if (netWeight is string)
                                {
                                    if (!item.ErrorData.ContainsKey("Net Weight Inner Carton"))
                                    {
                                        item.ErrorData.Add("Net Weight Inner Carton", new List<string>());
                                    }

                                    item.ErrorData["Net Weight Inner Carton"].Add($"Special characters are present in Net Weight Inner Carton '{netWeight}'");
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Inner Pack Gross Weight"))
                        {
                            var innerPackGross = item.ColumnData["Inner Pack Gross Weight"];
                            if (innerPackGross != null && decimal.TryParse(innerPackGross.ToString(), out decimal innerGrossValue))
                            {
                                if (innerGrossValue <= 0 || innerGrossValue % 1 != 0)
                                {
                                    if (!item.ErrorData.ContainsKey("Inner Pack Gross Weight"))
                                    {
                                        item.ErrorData.Add("Inner Pack Gross Weight", new List<string>());
                                    }

                                    item.ErrorData["Inner Pack Gross Weight"].Add("Inner Pack Gross Weight Field is expected to be a number");
                                }
                            }

                            if (string.IsNullOrWhiteSpace(innerPackGross.ToString()))
                            {
                                if (!item.ErrorData.ContainsKey("Inner Pack Gross Weight"))
                                {
                                    item.ErrorData.Add("Inner Pack Gross Weight", new List<string>());
                                }

                                item.ErrorData["Inner Pack Gross Weight"].Add("Inner Pack Gross Weight Field is required");
                            }
                            else
                            {
                                if (innerPackGross is string)
                                {
                                    if (!item.ErrorData.ContainsKey("Inner Pack Gross Weight"))
                                    {
                                        item.ErrorData.Add("Inner Pack Gross Weight", new List<string>());
                                    }

                                    item.ErrorData["Inner Pack Gross Weight"].Add($"Special characters are present in Inner Pack Gross Weight '{innerPackGross}'");
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Master Carton Gross Weight"))
                        {
                            var masterCartonGross = item.ColumnData["Master Carton Gross Weight"];

                            if (masterCartonGross != null && decimal.TryParse(masterCartonGross.ToString(), out decimal masterGrossValue))
                            {
                                if (masterGrossValue <= 0 || masterGrossValue % 1 != 0)
                                {
                                    if (!item.ErrorData.ContainsKey("Master Carton Gross Weight"))
                                    {
                                        item.ErrorData.Add("Master Carton Gross Weight", new List<string>());
                                    }

                                    item.ErrorData["Master Carton Gross Weight"].Add("Master Carton Gross Weight Field is expected to be a number");
                                }
                            }

                            if (string.IsNullOrWhiteSpace(masterCartonGross.ToString()))
                            {
                                if (!item.ErrorData.ContainsKey("Master Carton Gross Weight"))
                                {
                                    item.ErrorData.Add("Master Carton Gross Weight", new List<string>());
                                }

                                item.ErrorData["Master Carton Gross Weight"].Add("Master Carton Gross Weight Field is required");
                            }
                            else
                            {
                                if (masterCartonGross is string)
                                {
                                    if (!item.ErrorData.ContainsKey("Master Carton Gross Weight"))
                                    {
                                        item.ErrorData.Add("Master Carton Gross Weight", new List<string>());
                                    }

                                    item.ErrorData["Master Carton Gross Weight"].Add($"Special characters are present in Master Carton Gross Weight '{masterCartonGross}'");
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Store Category"))
                        {
                            var store = item.ColumnData["Store Category"];

                            if (string.IsNullOrWhiteSpace(store.ToString()))
                            {
                                if (!item.ErrorData.ContainsKey("Store Category"))
                                {
                                    item.ErrorData.Add("Store Category", new List<string>());
                                }

                                item.ErrorData["Store Category"].Add("Store Category Field is Required");
                            }
                            else
                            {
                                var storecheck = storeCategory.Where(a => a.Code.ToString() == store.ToString()).ToList();

                                if (!storecheck.Any())
                                {
                                    if (!item.ErrorData.ContainsKey("Store Category"))
                                    {
                                        item.ErrorData.Add("Store Category", new List<string>());
                                    }

                                    item.ErrorData["Store Category"].Add($"Invalid Store Category '{store}'");
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Primary Packaging"))
                        {
                            var primaryPackaging = item.ColumnData["Primary Packaging"].ToString();
                            string[] packaging = { "Yes", "No" };
                            if (string.IsNullOrWhiteSpace(primaryPackaging))
                            {
                                if (!item.ErrorData.ContainsKey("Primary Packaging"))
                                {
                                    item.ErrorData.Add("Primary Packaging", new List<string>());
                                }

                                item.ErrorData["Primary Packaging"].Add("Primary Packaging Field is required");
                            }
                            else
                            {
                                if (!packaging.Contains(primaryPackaging))
                                {
                                    if (!item.ErrorData.ContainsKey("Primary Packaging"))
                                    {
                                        item.ErrorData.Add("Primary Packaging", new List<string>());
                                    }

                                    item.ErrorData["Primary Packaging"].Add($"Invalid Primary Packaging '{primaryPackaging}'");
                                }
                            }
                        }
                    }
                    else if (templateDivisionId == "4" || templateDivisionId == "65")
                    {
                        if (item.ColumnData.ContainsKey("Packaging Type"))
                        {
                            var packagingTypeCodeObj = item.ColumnData["Packaging Type"];

                            if (!string.IsNullOrWhiteSpace(packagingTypeCodeObj.ToString()))
                            {
                                if (packagingTypeCodeObj is string)
                                {
                                    if (!item.ErrorData.ContainsKey("Packaging Type"))
                                    {
                                        item.ErrorData.Add("Packaging Type", new List<string>());
                                    }

                                    item.ErrorData["Packaging Type"].Add($"Special characters are present in Packaging Type '{packagingTypeCodeObj}'");
                                }
                                else
                                {
                                    var packaging_type = packagingtype.Where(a => a.PackagingTypeCode.ToString() == packagingTypeCodeObj.ToString()).ToList();

                                    if (!packaging_type.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("Packaging Type"))
                                        {
                                            item.ErrorData.Add("Packaging Type", new List<string>());
                                        }

                                        item.ErrorData["Packaging Type"].Add($"Invalid Packaging Type '{packagingTypeCodeObj}'");
                                    }
                                    else
                                    {
                                        var mtrpackaging = packagingtype.Where(a => a.PackagingTypeCode.ToString() == packagingTypeCodeObj.ToString() && a.DivisionId.Split(',').Select(d => d.Trim()).Contains(templateDivisionId)).ToList();
                                        if (!mtrpackaging.Any())
                                        {
                                            if (!item.ErrorData.ContainsKey("Packaging Type"))
                                            {
                                                item.ErrorData.Add("Packaging Type", new List<string>());
                                            }

                                            item.ErrorData["Packaging Type"].Add($"Division Mismatched for Packaging Type {packagingTypeCodeObj}");
                                        }
                                    }
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Store Category"))
                        {
                            var store = item.ColumnData["Store Category"];

                            if (!string.IsNullOrWhiteSpace(store.ToString()))
                            {
                                var storecheck = storeCategory.Where(a => a.Code.ToString() == store.ToString()).ToList();

                                if (!storecheck.Any())
                                {
                                    if (!item.ErrorData.ContainsKey("Store Category"))
                                    {
                                        item.ErrorData.Add("Store Category", new List<string>());
                                    }

                                    item.ErrorData["Store Category"].Add($"Invalid Store Category '{store}'");
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Primary Packaging"))
                        {
                            var primaryPackaging = item.ColumnData["Primary Packaging"].ToString();
                            string[] packaging = { "Yes", "No" };
                            if (!string.IsNullOrWhiteSpace(primaryPackaging))
                            {
                                if (!packaging.Contains(primaryPackaging))
                                {
                                    if (!item.ErrorData.ContainsKey("Primary Packaging"))
                                    {
                                        item.ErrorData.Add("Primary Packaging", new List<string>());
                                    }

                                    item.ErrorData["Primary Packaging"].Add($"Invalid Primary Packaging '{primaryPackaging}'");
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Product Gross Weight (GMS)"))
                        {
                            var grossWeightGMS = item.ColumnData["Product Gross Weight (GMS)"];
                            if (!string.IsNullOrWhiteSpace(grossWeightGMS.ToString()))
                            {
                                // if (grossWeightGMS != null && decimal.TryParse(grossWeightGMS.ToString(), out decimal grossValue))
                                // {
                                //    if (grossValue <= 0 || grossValue % 1 != 0)
                                //    {
                                //        if (!item.ErrorData.ContainsKey("Product Gross Weight (GMS)"))
                                //        {
                                //            item.ErrorData.Add("Product Gross Weight (GMS)", new List<string>());
                                //        }
                                //        item.ErrorData["Product Gross Weight (GMS)"].Add("Product Gross Weight (GMS) Field is expected to be a number");
                                //    }
                                // }
                                // else
                                // {
                                if (grossWeightGMS is string)
                                {
                                    if (!item.ErrorData.ContainsKey("Product Gross Weight (GMS)"))
                                    {
                                        item.ErrorData.Add("Product Gross Weight (GMS)", new List<string>());
                                    }

                                    item.ErrorData["Product Gross Weight (GMS)"].Add($"Special characters are present in Product Gross Weight (GMS) '{grossWeightGMS}'");
                                }

                                // }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Packaging Dimensions L (CM)"))
                        {
                            var packagingdimensionL = item.ColumnData["Packaging Dimensions L (CM)"];
                            if (!string.IsNullOrWhiteSpace(packagingdimensionL.ToString()))
                            {
                                if (packagingdimensionL != null && decimal.TryParse(packagingdimensionL.ToString(), out decimal dimensionsLValue))
                                {
                                    if (dimensionsLValue <= 0 || dimensionsLValue % 1 != 0)
                                    {
                                        if (!item.ErrorData.ContainsKey("Packaging Dimensions L (CM)"))
                                        {
                                            item.ErrorData.Add("Packaging Dimensions L (CM)", new List<string>());
                                        }

                                        item.ErrorData["Packaging Dimensions L (CM)"].Add("Packaging Dimensions L (CM) Field is expected to be a number");
                                    }
                                }
                                else
                                {
                                    if (packagingdimensionL is string)
                                    {
                                        if (!item.ErrorData.ContainsKey("Packaging Dimensions L (CM)"))
                                        {
                                            item.ErrorData.Add("Packaging Dimensions L (CM)", new List<string>());
                                        }

                                        item.ErrorData["Packaging Dimensions L (CM)"].Add($"Special characters are present in Packaging Dimensions L (CM) '{packagingdimensionL}'");
                                    }
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Packaging Dimensions W (CM)"))
                        {
                            var packagingdimensionW = item.ColumnData["Packaging Dimensions W (CM)"];
                            if (!string.IsNullOrWhiteSpace(packagingdimensionW.ToString()))
                            {
                                if (packagingdimensionW != null && decimal.TryParse(packagingdimensionW.ToString(), out decimal dimensionsWValue))
                                {
                                    if (dimensionsWValue <= 0 || dimensionsWValue % 1 != 0)
                                    {
                                        if (!item.ErrorData.ContainsKey("Packaging Dimensions W (CM)"))
                                        {
                                            item.ErrorData.Add("Packaging Dimensions W (CM)", new List<string>());
                                        }

                                        item.ErrorData["Packaging Dimensions W (CM)"].Add("Packaging Dimensions W (CM) Field is expected to be a number");
                                    }
                                }
                                else
                                {
                                    if (packagingdimensionW is string)
                                    {
                                        if (!item.ErrorData.ContainsKey("Packaging Dimensions W (CM)"))
                                        {
                                            item.ErrorData.Add("Packaging Dimensions W (CM)", new List<string>());
                                        }

                                        item.ErrorData["Packaging Dimensions W (CM)"].Add($"Special characters are present in Packaging Dimensions W (CM) '{packagingdimensionW}'");
                                    }
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Packaging Dimensions H (CM)"))
                        {
                            var packagingdimensionH = item.ColumnData["Packaging Dimensions H (CM)"];
                            if (!string.IsNullOrWhiteSpace(packagingdimensionH.ToString()))
                            {
                                if (packagingdimensionH != null && decimal.TryParse(packagingdimensionH.ToString(), out decimal dimensionsHValue))
                                {
                                    if (dimensionsHValue <= 0 || dimensionsHValue % 1 != 0)
                                    {
                                        if (!item.ErrorData.ContainsKey("Packaging Dimensions H (CM)"))
                                        {
                                            item.ErrorData.Add("Packaging Dimensions H (CM)", new List<string>());
                                        }

                                        item.ErrorData["Packaging Dimensions H (CM)"].Add("Packaging Dimensions H (CM) Field is expected to be a number");
                                    }
                                }
                                else
                                {
                                    if (packagingdimensionH is string)
                                    {
                                        if (!item.ErrorData.ContainsKey("Packaging Dimensions H (CM)"))
                                        {
                                            item.ErrorData.Add("Packaging Dimensions H (CM)", new List<string>());
                                        }

                                        item.ErrorData["Packaging Dimensions H (CM)"].Add($"Special characters are present in Packaging Dimensions H (CM) '{packagingdimensionH}'");
                                    }
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Inner Pack Dimensions L (CM)"))
                        {
                            var innerPackL = item.ColumnData["Inner Pack Dimensions L (CM)"];
                            if (!string.IsNullOrWhiteSpace(innerPackL.ToString()))
                            {
                                // if (innerPackL != null && decimal.TryParse(innerPackL.ToString(), out decimal innerpackLValue))
                                // {
                                //    if (innerpackLValue <= 0 || innerpackLValue % 1 != 0)
                                //    {
                                //        if (!item.ErrorData.ContainsKey("Inner Pack Dimensions L (CM)"))
                                //        {
                                //            item.ErrorData.Add("Inner Pack Dimensions L (CM)", new List<string>());
                                //        }
                                //        item.ErrorData["Inner Pack Dimensions L (CM)"].Add("Inner Pack Dimensions L (CM) Field is expected to be a number");
                                //    }
                                // }
                                // else
                                // {
                                if (innerPackL is string)
                                {
                                    if (!item.ErrorData.ContainsKey("Inner Pack Dimensions L (CM)"))
                                    {
                                        item.ErrorData.Add("Inner Pack Dimensions L (CM)", new List<string>());
                                    }

                                    item.ErrorData["Inner Pack Dimensions L (CM)"].Add($"Special characters are present in Inner Pack Dimensions L (CM) '{innerPackL}'");
                                }

                                // }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Inner Pack Dimensions W (CM)"))
                        {
                            var innerPackW = item.ColumnData["Inner Pack Dimensions W (CM)"];
                            if (!string.IsNullOrWhiteSpace(innerPackW.ToString()))
                            {
                                // if (innerPackW != null && decimal.TryParse(innerPackW.ToString(), out decimal innerpackWValue))
                                // {
                                //    if (innerpackWValue <= 0 || innerpackWValue % 1 != 0)
                                //    {
                                //        if (!item.ErrorData.ContainsKey("Inner Pack Dimensions W (CM)"))
                                //        {
                                //            item.ErrorData.Add("Inner Pack Dimensions W (CM)", new List<string>());
                                //        }
                                //        item.ErrorData["Inner Pack Dimensions W (CM)"].Add("Inner Pack Dimensions W (CM) Field is expected to be a number");
                                //    }
                                // }
                                // else
                                // {
                                if (innerPackW is string)
                                {
                                    if (!item.ErrorData.ContainsKey("Inner Pack Dimensions W (CM)"))
                                    {
                                        item.ErrorData.Add("Inner Pack Dimensions W (CM)", new List<string>());
                                    }

                                    item.ErrorData["Inner Pack Dimensions W (CM)"].Add($"Special characters are present in Inner Pack Dimensions W (CM) '{innerPackW}'");
                                }

                                // }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Inner Pack Dimensions H (CM)"))
                        {
                            var innerPackH = item.ColumnData["Inner Pack Dimensions H (CM)"];
                            if (!string.IsNullOrWhiteSpace(innerPackH.ToString()))
                            {
                                // if (innerPackH != null && decimal.TryParse(innerPackH.ToString(), out decimal innerpackHValue))
                                // {
                                //    if (innerpackHValue <= 0 || innerpackHValue % 1 != 0)
                                //    {
                                //        if (!item.ErrorData.ContainsKey("Inner Pack Dimensions H (CM)"))
                                //        {
                                //            item.ErrorData.Add("Inner Pack Dimensions H (CM)", new List<string>());
                                //        }
                                //        item.ErrorData["Inner Pack Dimensions H (CM)"].Add("Inner Pack Dimensions H (CM) Field is expected to be a number");
                                //    }
                                // }
                                // else
                                // {
                                if (innerPackH is string)
                                {
                                    if (!item.ErrorData.ContainsKey("Inner Pack Dimensions H (CM)"))
                                    {
                                        item.ErrorData.Add("Inner Pack Dimensions H (CM)", new List<string>());
                                    }

                                    item.ErrorData["Inner Pack Dimensions H (CM)"].Add($"Special characters are present in Inner Pack Dimensions H (CM) '{innerPackH}'");
                                }

                                // }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Master Carton Dimensions L (CM)"))
                        {
                            var masterCartonL = item.ColumnData["Master Carton Dimensions L (CM)"];
                            if (!string.IsNullOrWhiteSpace(masterCartonL.ToString()))
                            {
                                if (masterCartonL != null && decimal.TryParse(masterCartonL.ToString(), out decimal masterLValue))
                                {
                                    if (masterLValue <= 0 || masterLValue % 1 != 0)
                                    {
                                        if (!item.ErrorData.ContainsKey("Master Carton Dimensions L (CM)"))
                                        {
                                            item.ErrorData.Add("Master Carton Dimensions L (CM)", new List<string>());
                                        }

                                        item.ErrorData["Master Carton Dimensions L (CM)"].Add("Master Carton Dimensions L (CM) Field is expected to be a number");
                                    }
                                }
                                else
                                {
                                    if (masterCartonL is string)
                                    {
                                        if (!item.ErrorData.ContainsKey("Master Carton Dimensions L (CM)"))
                                        {
                                            item.ErrorData.Add("Master Carton Dimensions L (CM)", new List<string>());
                                        }

                                        item.ErrorData["Master Carton Dimensions L (CM)"].Add($"Special characters are present in Master Carton Dimensions L (CM) '{masterCartonL}'");
                                    }
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Master Carton Dimensions W (CM)"))
                        {
                            var masterCartonW = item.ColumnData["Master Carton Dimensions W (CM)"];
                            if (!string.IsNullOrWhiteSpace(masterCartonW.ToString()))
                            {
                                if (masterCartonW != null && decimal.TryParse(masterCartonW.ToString(), out decimal masterWValue))
                                {
                                    if (masterWValue <= 0 || masterWValue % 1 != 0)
                                    {
                                        if (!item.ErrorData.ContainsKey("Master Carton Dimensions W (CM)"))
                                        {
                                            item.ErrorData.Add("Master Carton Dimensions W (CM)", new List<string>());
                                        }

                                        item.ErrorData["Master Carton Dimensions W (CM)"].Add("Master Carton Dimensions W (CM) Field is expected to be a number");
                                    }
                                }
                                else
                                {
                                    if (masterCartonW is string)
                                    {
                                        if (!item.ErrorData.ContainsKey("Master Carton Dimensions W (CM)"))
                                        {
                                            item.ErrorData.Add("Master Carton Dimensions W (CM)", new List<string>());
                                        }

                                        item.ErrorData["Master Carton Dimensions W (CM)"].Add($"Special characters are present in Master Carton Dimensions W (CM) '{masterCartonW}'");
                                    }
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Master Carton Dimensions H (CM)"))
                        {
                            var masterCartonH = item.ColumnData["Master Carton Dimensions H (CM)"];
                            if (!string.IsNullOrWhiteSpace(masterCartonH.ToString()))
                            {
                                if (masterCartonH != null && decimal.TryParse(masterCartonH.ToString(), out decimal masterHValue))
                                {
                                    if (masterHValue <= 0 || masterHValue % 1 != 0)
                                    {
                                        if (!item.ErrorData.ContainsKey("Master Carton Dimensions H (CM)"))
                                        {
                                            item.ErrorData.Add("Master Carton Dimensions H (CM)", new List<string>());
                                        }

                                        item.ErrorData["Master Carton Dimensions H (CM)"].Add("Master Carton Dimensions H (CM) Field is expected to be a number");
                                    }
                                }
                                else
                                {
                                    if (masterCartonH is string)
                                    {
                                        if (!item.ErrorData.ContainsKey("Master Carton Dimensions H (CM)"))
                                        {
                                            item.ErrorData.Add("Master Carton Dimensions H (CM)", new List<string>());
                                        }

                                        item.ErrorData["Master Carton Dimensions H (CM)"].Add($"Special characters are present in Master Carton Dimensions H (CM) '{masterCartonH}'");
                                    }
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Net Weight Inner Carton"))
                        {
                            var netWeight = item.ColumnData["Net Weight Inner Carton"];
                            if (!string.IsNullOrWhiteSpace(netWeight.ToString()))
                            {
                                if (netWeight != null && decimal.TryParse(netWeight.ToString(), out decimal netWeightInnerValue))
                                {
                                    if (netWeightInnerValue <= 0 || netWeightInnerValue % 1 != 0)
                                    {
                                        if (!item.ErrorData.ContainsKey("Net Weight Inner Carton"))
                                        {
                                            item.ErrorData.Add("Net Weight Inner Carton", new List<string>());
                                        }

                                        item.ErrorData["Net Weight Inner Carton"].Add("Net Weight Inner Carton Field is expected to be a number");
                                    }
                                }
                                else
                                {
                                    if (netWeight is string)
                                    {
                                        if (!item.ErrorData.ContainsKey("Net Weight Inner Carton"))
                                        {
                                            item.ErrorData.Add("Net Weight Inner Carton", new List<string>());
                                        }

                                        item.ErrorData["Net Weight Inner Carton"].Add($"Special characters are present in Net Weight Inner Carton '{netWeight}'");
                                    }
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Inner Pack Gross Weight"))
                        {
                            var innerPackGross = item.ColumnData["Inner Pack Gross Weight"];
                            if (!string.IsNullOrWhiteSpace(innerPackGross.ToString()))
                            {
                                if (innerPackGross != null && decimal.TryParse(innerPackGross.ToString(), out decimal innerGrossValue))
                                {
                                    if (innerGrossValue <= 0 || innerGrossValue % 1 != 0)
                                    {
                                        if (!item.ErrorData.ContainsKey("Inner Pack Gross Weight"))
                                        {
                                            item.ErrorData.Add("Inner Pack Gross Weight", new List<string>());
                                        }

                                        item.ErrorData["Inner Pack Gross Weight"].Add("Inner Pack Gross Weight Field is expected to be a number");
                                    }
                                }
                                else
                                {
                                    if (innerPackGross is string)
                                    {
                                        if (!item.ErrorData.ContainsKey("Inner Pack Gross Weight"))
                                        {
                                            item.ErrorData.Add("Inner Pack Gross Weight", new List<string>());
                                        }

                                        item.ErrorData["Inner Pack Gross Weight"].Add($"Special characters are present in Inner Pack Gross Weight '{innerPackGross}'");
                                    }
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Master Carton Gross Weight"))
                        {
                            var masterCartonGross = item.ColumnData["Master Carton Gross Weight"];
                            if (!string.IsNullOrWhiteSpace(masterCartonGross.ToString()))
                            {
                                if (masterCartonGross != null && decimal.TryParse(masterCartonGross.ToString(), out decimal masterGrossValue))
                                {
                                    if (masterGrossValue <= 0 || masterGrossValue % 1 != 0)
                                    {
                                        if (!item.ErrorData.ContainsKey("Master Carton Gross Weight"))
                                        {
                                            item.ErrorData.Add("Master Carton Gross Weight", new List<string>());
                                        }

                                        item.ErrorData["Master Carton Gross Weight"].Add("Master Carton Gross Weight Field is expected to be a number");
                                    }
                                }
                                else
                                {
                                    if (masterCartonGross is string)
                                    {
                                        if (!item.ErrorData.ContainsKey("Master Carton Gross Weight"))
                                        {
                                            item.ErrorData.Add("Master Carton Gross Weight", new List<string>());
                                        }

                                        item.ErrorData["Master Carton Gross Weight"].Add($"Special characters are present in Master Carton Gross Weight '{masterCartonGross}'");
                                    }
                                }
                            }
                        }
                    }
                });

                if (!template.IsUidTemplate)
                {
                    Parallel.ForEach(data, item =>
                    {
                        if (item.ColumnData.ContainsKey("Tax Category"))
                        {
                            var taxCategoryCodeObj = item.ColumnData["Tax Category"];
                            if (string.IsNullOrWhiteSpace(taxCategoryCodeObj.ToString()))
                            {
                                if (!item.ErrorData.ContainsKey("Tax Category"))
                                {
                                    item.ErrorData.Add("Tax Category", new List<string>());
                                }

                                item.ErrorData["Tax Category"].Add("Tax Category Field is required");
                            }
                            else if (!int.TryParse(taxCategoryCodeObj.ToString(), out _))
                            {
                                if (!item.ErrorData.ContainsKey("Tax Category"))
                                {
                                    item.ErrorData.Add("Tax Category", new List<string>());
                                }

                                item.ErrorData["Tax Category"].Add("Tax Category Field is expected to be a number");
                            }
                            else
                            {
                                if (taxCategoryCodeObj is string)
                                {
                                    if (!item.ErrorData.ContainsKey("Tax Category"))
                                    {
                                        item.ErrorData.Add("Tax Category", new List<string>());
                                    }

                                    item.ErrorData["Tax Category"].Add($"Special characters are present in Tax Category '{taxCategoryCodeObj}'");
                                }
                                else
                                {
                                    var taxCategoryList = taxCategory.Where(a => a.Code.ToString() == taxCategoryCodeObj.ToString()).ToList();

                                    if (!taxCategoryList.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("Tax Category"))
                                        {
                                            item.ErrorData.Add("Tax Category", new List<string>());
                                        }

                                        item.ErrorData["Tax Category"].Add($"Invalid Tax Category '{taxCategoryCodeObj}'");
                                    }
                                }
                            }
                        }

                        if (templateDivisionId == "61")
                        {
                            if (item.ColumnData.ContainsKey("Inner Carton Code"))
                            {
                                var innerCartonCodeStr = item.ColumnData["Inner Carton Code"];
                                if (innerCartonCodeStr != null && decimal.TryParse(innerCartonCodeStr.ToString(), out decimal innerCartonCodeValue))
                                {
                                    if (innerCartonCodeValue <= 0 || innerCartonCodeValue % 1 != 0)
                                    {
                                        if (!item.ErrorData.ContainsKey("Inner Carton Code"))
                                        {
                                            item.ErrorData.Add("Inner Carton Code", new List<string>());
                                        }

                                        item.ErrorData["Inner Carton Code"].Add("Inner Carton Code Field is expected to be a number");
                                    }
                                }

                                if (string.IsNullOrWhiteSpace(innerCartonCodeStr.ToString()))
                                {
                                    if (!item.ErrorData.ContainsKey("Inner Carton Code"))
                                    {
                                        item.ErrorData.Add("Inner Carton Code", new List<string>());
                                    }

                                    item.ErrorData["Inner Carton Code"].Add("Inner Carton Code Field is required");
                                }
                                else
                                {
                                    if (innerCartonCodeStr is string)
                                    {
                                        if (!item.ErrorData.ContainsKey("Inner Carton Code"))
                                        {
                                            item.ErrorData.Add("Inner Carton Code", new List<string>());
                                        }

                                        item.ErrorData["Inner Carton Code"].Add($"Special characters are present in Inner Carton Code '{innerCartonCodeStr}'");
                                    }
                                    else
                                    {
                                        var inner_carton = innerCarton.Where(a => a.Code.ToString() == innerCartonCodeStr.ToString()).ToList();
                                        if (!inner_carton.Any())
                                        {
                                            if (!item.ErrorData.ContainsKey("Inner Carton Code"))
                                            {
                                                item.ErrorData.Add("Inner Carton Code", new List<string>());
                                            }

                                            item.ErrorData["Inner Carton Code"].Add($"Invalid Inner Carton Code '{innerCartonCodeStr}'");
                                        }
                                        else
                                        {
                                            var mtrinnercartoncode = innerCarton.Where(a => a.Code.ToString() == innerCartonCodeStr.ToString() && a.DivisionId.Split(',').Select(d => d.Trim()).Contains(templateDivisionId)).ToList();
                                            if (!mtrinnercartoncode.Any())
                                            {
                                                if (!item.ErrorData.ContainsKey("Inner Carton Code"))
                                                {
                                                    item.ErrorData.Add("Inner Carton Code", new List<string>());
                                                }

                                                item.ErrorData["Inner Carton Code"].Add($"Division Mismatched for Inner Carton Code {innerCartonCodeStr}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else if (templateDivisionId == "4" || templateDivisionId == "65")
                        {
                            if (item.ColumnData.ContainsKey("Inner Carton Code"))
                            {
                                var innerCartonCodeStr = item.ColumnData["Inner Carton Code"];

                                if (innerCartonCodeStr != null && decimal.TryParse(innerCartonCodeStr.ToString(), out decimal innerCartonCodeValue))
                                {
                                    if (innerCartonCodeValue <= 0 || innerCartonCodeValue % 1 != 0)
                                    {
                                        if (!item.ErrorData.ContainsKey("Inner Carton Code"))
                                        {
                                            item.ErrorData.Add("Inner Carton Code", new List<string>());
                                        }

                                        item.ErrorData["Inner Carton Code"].Add("Inner Carton Code Field is expected to be a number");
                                    }
                                }

                                if (!string.IsNullOrWhiteSpace(innerCartonCodeStr.ToString()))
                                {
                                    if (innerCartonCodeStr is string)
                                    {
                                        if (!item.ErrorData.ContainsKey("Inner Carton Code"))
                                        {
                                            item.ErrorData.Add("Inner Carton Code", new List<string>());
                                        }

                                        item.ErrorData["Inner Carton Code"].Add($"Special characters are present in Inner Carton Code '{innerCartonCodeStr}'");
                                    }
                                    else
                                    {
                                        var inner_carton = innerCarton.Where(a => a.Code.ToString() == innerCartonCodeStr.ToString()).ToList();
                                        if (!inner_carton.Any())
                                        {
                                            if (!item.ErrorData.ContainsKey("Inner Carton Code"))
                                            {
                                                item.ErrorData.Add("Inner Carton Code", new List<string>());
                                            }

                                            item.ErrorData["Inner Carton Code"].Add($"Invalid Inner Carton Code '{innerCartonCodeStr}'");
                                        }
                                        else
                                        {
                                            var mtrinnercartoncode = innerCarton.Where(a => a.Code.ToString() == innerCartonCodeStr.ToString() && a.DivisionId.Split(',').Select(d => d.Trim()).Contains(templateDivisionId)).ToList();
                                            if (!mtrinnercartoncode.Any())
                                            {
                                                if (!item.ErrorData.ContainsKey("Inner Carton Code"))
                                                {
                                                    item.ErrorData.Add("Inner Carton Code", new List<string>());
                                                }

                                                item.ErrorData["Inner Carton Code"].Add($"Division Mismatched for Inner Carton Code {innerCartonCodeStr}");
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (templateDivisionId == "61")
                        {
                            if (item.ColumnData.ContainsKey("Master Carton Code"))
                            {
                                var masterCartonCodeStr = item.ColumnData["Master Carton Code"];
                                if (masterCartonCodeStr != null && decimal.TryParse(masterCartonCodeStr.ToString(), out decimal masterCartonCodeValue))
                                {
                                    if (masterCartonCodeValue <= 0 || masterCartonCodeValue % 1 != 0)
                                    {
                                        if (!item.ErrorData.ContainsKey("Master Carton Code"))
                                        {
                                            item.ErrorData.Add("Master Carton Code", new List<string>());
                                        }

                                        item.ErrorData["Master Carton Code"].Add("Master Carton Code Field is expected to be a number");
                                    }
                                }

                                if (string.IsNullOrWhiteSpace(masterCartonCodeStr.ToString()))
                                {
                                    if (!item.ErrorData.ContainsKey("Master Carton Code"))
                                    {
                                        item.ErrorData.Add("Master Carton Code", new List<string>());
                                    }

                                    item.ErrorData["Master Carton Code"].Add("Master Carton Code Field is required");
                                }
                                else
                                {
                                    if (masterCartonCodeStr is string)
                                    {
                                        if (!item.ErrorData.ContainsKey("Master Carton Code"))
                                        {
                                            item.ErrorData.Add("Master Carton Code", new List<string>());
                                        }

                                        item.ErrorData["Master Carton Code"].Add($"Special characters are present in Master Carton Code '{masterCartonCodeStr}'");
                                    }
                                    else
                                    {
                                        var master_carton = masterCarton.Where(a => a.Code.ToString() == masterCartonCodeStr.ToString()).ToList();

                                        if (!master_carton.Any())
                                        {
                                            if (!item.ErrorData.ContainsKey("Master Carton Code"))
                                            {
                                                item.ErrorData.Add("Master Carton Code", new List<string>());
                                            }

                                            item.ErrorData["Master Carton Code"].Add($"Invalid Master Carton Code '{masterCartonCodeStr}'");
                                        }
                                        else
                                        {
                                            var mtrmastercartoncode = masterCarton.Where(a => a.Code.ToString() == masterCartonCodeStr.ToString() && a.DivisionId.Split(',').Select(d => d.Trim()).Contains(templateDivisionId)).ToList();
                                            if (!mtrmastercartoncode.Any())
                                            {
                                                if (!item.ErrorData.ContainsKey("Master Carton Code"))
                                                {
                                                    item.ErrorData.Add("Master Carton Code", new List<string>());
                                                }

                                                item.ErrorData["Master Carton Code"].Add($"Division Mismatched for Master Carton Code {masterCartonCodeStr}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else if (templateDivisionId == "4" || templateDivisionId == "65")
                        {
                            if (item.ColumnData.ContainsKey("Master Carton Code"))
                            {
                                var masterCartonCodeStr = item.ColumnData["Master Carton Code"];

                                if (masterCartonCodeStr != null && decimal.TryParse(masterCartonCodeStr.ToString(), out decimal masterCartonCodeValue))
                                {
                                    if (masterCartonCodeValue <= 0 || masterCartonCodeValue % 1 != 0)
                                    {
                                        if (!item.ErrorData.ContainsKey("Master Carton Code"))
                                        {
                                            item.ErrorData.Add("Master Carton Code", new List<string>());
                                        }

                                        item.ErrorData["Master Carton Code"].Add("Master Carton Code Field is expected to be a number");
                                    }
                                }

                                if (!string.IsNullOrWhiteSpace(masterCartonCodeStr.ToString()))
                                {
                                    if (masterCartonCodeStr is string)
                                    {
                                        if (!item.ErrorData.ContainsKey("Master Carton Code"))
                                        {
                                            item.ErrorData.Add("Master Carton Code", new List<string>());
                                        }

                                        item.ErrorData["Master Carton Code"].Add($"Special characters are present in Master Carton Code '{masterCartonCodeStr}'");
                                    }
                                    else
                                    {
                                        var master_carton = masterCarton.Where(a => a.Code.ToString() == masterCartonCodeStr.ToString()).ToList();

                                        if (!master_carton.Any())
                                        {
                                            if (!item.ErrorData.ContainsKey("Master Carton Code"))
                                            {
                                                item.ErrorData.Add("Master Carton Code", new List<string>());
                                            }

                                            item.ErrorData["Master Carton Code"].Add($"Invalid Master Carton Code '{masterCartonCodeStr}'");
                                        }
                                        else
                                        {
                                            var mtrmastercartoncode = masterCarton.Where(a => a.Code.ToString() == masterCartonCodeStr.ToString() && a.DivisionId.Split(',').Select(d => d.Trim()).Contains(templateDivisionId)).ToList();
                                            if (!mtrmastercartoncode.Any())
                                            {
                                                if (!item.ErrorData.ContainsKey("Master Carton Code"))
                                                {
                                                    item.ErrorData.Add("Master Carton Code", new List<string>());
                                                }

                                                item.ErrorData["Master Carton Code"].Add($"Division Mismatched for Master Carton Code {masterCartonCodeStr}");
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Made Of Value"))
                        {
                            var madeOfValueCodeStr = item.ColumnData["Made Of Value"];

                            if (templateDivisionId == "61")
                            {
                                if (madeOfValueCodeStr != null && decimal.TryParse(madeOfValueCodeStr.ToString(), out decimal madeValue))
                                {
                                    if (madeValue <= 0 || madeValue % 1 != 0)
                                    {
                                        if (!item.ErrorData.ContainsKey("Made Of Value"))
                                        {
                                            item.ErrorData.Add("Made Of Value", new List<string>());
                                        }

                                        item.ErrorData["Made Of Value"].Add("Made Of Value Field is expected to be a number");
                                    }
                                }

                                if (string.IsNullOrWhiteSpace(madeOfValueCodeStr.ToString()))
                                {
                                    if (!item.ErrorData.ContainsKey("Made Of Value"))
                                    {
                                        item.ErrorData.Add("Made Of Value", new List<string>());
                                    }

                                    item.ErrorData["Made Of Value"].Add(" Made Of Value Field Field is Required");
                                }
                                else
                                {
                                    if (madeOfValueCodeStr is string)
                                    {
                                        if (!item.ErrorData.ContainsKey("Made Of Value"))
                                        {
                                            item.ErrorData.Add("Made Of Value", new List<string>());
                                        }

                                        item.ErrorData["Made Of Value"].Add($"Special characters are present in Made Of Value '{madeOfValueCodeStr}'");
                                    }
                                    else
                                    {
                                        var made_of_value = madeOfValue.Where(a => a.Code.ToString() == madeOfValueCodeStr.ToString()).ToList();

                                        if (!made_of_value.Any())
                                        {
                                            if (!item.ErrorData.ContainsKey("Made Of Value"))
                                            {
                                                item.ErrorData.Add("Made Of Value", new List<string>());
                                            }

                                            item.ErrorData["Made Of Value"].Add($"Invalid Made Of Value '{madeOfValueCodeStr}'");
                                        }
                                    }
                                }
                            }
                            else if (templateDivisionId == "4" || templateDivisionId == "65")
                            {
                                if (madeOfValueCodeStr != null && decimal.TryParse(madeOfValueCodeStr.ToString(), out decimal madeValue))
                                {
                                    if (madeValue <= 0 || madeValue % 1 != 0)
                                    {
                                        if (!item.ErrorData.ContainsKey("Made Of Value"))
                                        {
                                            item.ErrorData.Add("Made Of Value", new List<string>());
                                        }

                                        item.ErrorData["Made Of Value"].Add("Made Of Value Field is expected to be a number");
                                    }
                                }

                                if (string.IsNullOrWhiteSpace(madeOfValueCodeStr.ToString()))
                                {
                                    if (!item.ErrorData.ContainsKey("Made Of Value"))
                                    {
                                        item.ErrorData.Add("Made Of Value", new List<string>());
                                    }

                                    item.ErrorData["Made Of Value"].Add("Made Of Value Field is required");
                                }
                                else
                                {
                                    if (madeOfValueCodeStr is string)
                                    {
                                        if (!item.ErrorData.ContainsKey("Made Of Value"))
                                        {
                                            item.ErrorData.Add("Made Of Value", new List<string>());
                                        }

                                        item.ErrorData["Made Of Value"].Add($"Special characters are present in Made Of Value '{madeOfValueCodeStr}'");
                                    }
                                    else
                                    {
                                        var made_of_value = madeOfValue.Where(a => a.Code.ToString() == madeOfValueCodeStr.ToString()).ToList();

                                        if (!made_of_value.Any())
                                        {
                                            if (!item.ErrorData.ContainsKey("Made Of Value"))
                                            {
                                                item.ErrorData.Add("Made Of Value", new List<string>());
                                            }

                                            item.ErrorData["Made Of Value"].Add($"Invalid Made Of Value '{madeOfValueCodeStr}'");
                                        }
                                    }
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("RMS Supplier Code"))
                        {
                            var rmsSupplierCodeStr = item.ColumnData["RMS Supplier Code"];

                            if (rmsSupplierCodeStr != null && decimal.TryParse(rmsSupplierCodeStr.ToString(), out decimal rmsValue))
                            {
                                if (rmsValue <= 0 || rmsValue % 1 != 0)
                                {
                                    if (!item.ErrorData.ContainsKey("RMS Supplier Code"))
                                    {
                                        item.ErrorData.Add("RMS Supplier Code", new List<string>());
                                    }

                                    item.ErrorData["RMS Supplier Code"].Add("RMS Supplier Code Field is expected to be a number");
                                }
                            }

                            if (string.IsNullOrWhiteSpace(rmsSupplierCodeStr.ToString()))
                            {
                                if (!item.ErrorData.ContainsKey("RMS Supplier Code"))
                                {
                                    item.ErrorData.Add("RMS Supplier Code", new List<string>());
                                }

                                item.ErrorData["RMS Supplier Code"].Add("RMS Supplier Code Field is required");
                            }
                            else
                            {
                                if (!int.TryParse(rmsSupplierCodeStr.ToString(), out _))
                                {
                                    if (!item.ErrorData.ContainsKey("RMS Supplier Code"))
                                    {
                                        item.ErrorData.Add("RMS Supplier Code", new List<string>());
                                    }

                                    item.ErrorData["RMS Supplier Code"].Add(" RMS Supplier Code Field is expected to be a number");
                                }
                                else
                                {
                                    var rms_supplier_code = supplierCode.Where(a => a.Code.ToString() == rmsSupplierCodeStr.ToString()).ToList();

                                    if (!rms_supplier_code.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("RMS Supplier Code"))
                                        {
                                            item.ErrorData.Add("RMS Supplier Code", new List<string>());
                                        }

                                        item.ErrorData["RMS Supplier Code"].Add($"Invalid RMS Supplier Code '{rmsSupplierCodeStr}'");
                                    }
                                    else
                                    {
                                        var mtrrmssupplier = supplierCode.Where(a => a.Code.ToString() == rmsSupplierCodeStr.ToString() && a.DivisionId.Split(',').Select(d => d.Trim()).Contains(templateDivisionId)).ToList();
                                        if (!mtrrmssupplier.Any())
                                        {
                                            if (!item.ErrorData.ContainsKey("RMS Supplier Code"))
                                            {
                                                item.ErrorData.Add("RMS Supplier Code", new List<string>());
                                            }

                                            item.ErrorData["RMS Supplier Code"].Add($"Division Mismatched for RMS Supplier Code {rmsSupplierCodeStr}");
                                        }
                                    }
                                }
                            }
                        }

                        if (templateDivisionId == "61")
                        {
                            if (item.ColumnData.ContainsKey("Diff Group2 Val"))
                            {
                                var diffGroupValCodeStr = item.ColumnData["Diff Group2 Val"];
                                if (diffGroupValCodeStr != null && decimal.TryParse(diffGroupValCodeStr.ToString(), out decimal diffValue))
                                {
                                    if (diffValue <= 0 || diffValue % 1 != 0)
                                    {
                                        if (!item.ErrorData.ContainsKey("Diff Group2 Val"))
                                        {
                                            item.ErrorData.Add("Diff Group2 Val", new List<string>());
                                        }

                                        item.ErrorData["Diff Group2 Val"].Add("Diff Group2 Val Field is expected to be a number");
                                    }
                                }

                                if (string.IsNullOrWhiteSpace(diffGroupValCodeStr.ToString()))
                                {
                                    if (!item.ErrorData.ContainsKey("Diff Group2 Val"))
                                    {
                                        item.ErrorData.Add("Diff Group2 Val", new List<string>());
                                    }

                                    item.ErrorData["Diff Group2 Val"].Add("Diff Group2 Val Field is required");
                                }
                                else
                                {
                                    if (diffGroupValCodeStr is string)
                                    {
                                        if (!item.ErrorData.ContainsKey("Diff Group2 Val"))
                                        {
                                            item.ErrorData.Add("Diff Group2 Val", new List<string>());
                                        }

                                        item.ErrorData["Diff Group2 Val"].Add($"Special characters are present in Diff Group2 Val '{diffGroupValCodeStr}'");
                                    }
                                    else
                                    {
                                        var diff_group_val = diffgroup.Where(a => a.Code.ToString() == diffGroupValCodeStr.ToString()).ToList();

                                        if (!diff_group_val.Any())
                                        {
                                            if (!item.ErrorData.ContainsKey("Diff Group2 Val"))
                                            {
                                                item.ErrorData.Add("Diff Group2 Val", new List<string>());
                                            }

                                            item.ErrorData["Diff Group2 Val"].Add($"Invalid Diff Group2 Val '{diffGroupValCodeStr}'");
                                        }
                                    }
                                }
                            }
                        }
                        else if (templateDivisionId == "4" || templateDivisionId == "65")
                        {
                            if (item.ColumnData.ContainsKey("Diff Group2 Val"))
                            {
                                var diffGroupValCodeStr = item.ColumnData["Diff Group2 Val"];

                                if (diffGroupValCodeStr != null && decimal.TryParse(diffGroupValCodeStr.ToString(), out decimal diffValue))
                                {
                                    if (diffValue <= 0 || diffValue % 1 != 0)
                                    {
                                        if (!item.ErrorData.ContainsKey("Diff Group2 Val"))
                                        {
                                            item.ErrorData.Add("Diff Group2 Val", new List<string>());
                                        }

                                        item.ErrorData["Diff Group2 Val"].Add("Diff Group2 Val Field is expected to be a number");
                                    }
                                }

                                if (!string.IsNullOrWhiteSpace(diffGroupValCodeStr.ToString()))
                                {
                                    if (diffGroupValCodeStr is string)
                                    {
                                        if (!item.ErrorData.ContainsKey("Diff Group2 Val"))
                                        {
                                            item.ErrorData.Add("Diff Group2 Val", new List<string>());
                                        }

                                        item.ErrorData["Diff Group2 Val"].Add($"Special characters are present in Diff Group2 Val '{diffGroupValCodeStr}'");
                                    }
                                    else
                                    {
                                        var diff_group_val = diffgroup.Where(a => a.Code.ToString() == diffGroupValCodeStr.ToString()).ToList();

                                        if (!diff_group_val.Any())
                                        {
                                            if (!item.ErrorData.ContainsKey("Diff Group2 Val"))
                                            {
                                                item.ErrorData.Add("Diff Group2 Val", new List<string>());
                                            }

                                            item.ErrorData["Diff Group2 Val"].Add($"Invalid Diff Group2 Val '{diffGroupValCodeStr}'");
                                        }
                                    }
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Color Code"))
                        {
                            var colorCodeStr = item.ColumnData["Color Code"];
                            if (colorCodeStr != null && decimal.TryParse(colorCodeStr.ToString(), out decimal colorValue))
                            {
                                if (colorValue <= 0 || colorValue % 1 != 0)
                                {
                                    if (!item.ErrorData.ContainsKey("Color Code"))
                                    {
                                        item.ErrorData.Add("Color Code", new List<string>());
                                    }

                                    item.ErrorData["Color Code"].Add("Color Code Field is expected to be a number");
                                }
                            }

                            if (string.IsNullOrWhiteSpace(colorCodeStr.ToString()))
                            {
                                if (!item.ErrorData.ContainsKey("Color Code"))
                                {
                                    item.ErrorData.Add("Color Code", new List<string>());
                                }

                                item.ErrorData["Color Code"].Add("Color Code Field is Required");
                            }
                            else
                            {
                                if (colorCodeStr is string)
                                {
                                    if (!item.ErrorData.ContainsKey("Color Code"))
                                    {
                                        item.ErrorData.Add("Color Code", new List<string>());
                                    }

                                    item.ErrorData["Color Code"].Add($"Special characters are present in Color Code '{colorCodeStr}'");
                                }
                                else
                                {
                                    var color_code = color.Where(a => a.Code.ToString() == colorCodeStr.ToString()).ToList();

                                    if (!color_code.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("Color Code"))
                                        {
                                            item.ErrorData.Add("Color Code", new List<string>());
                                        }

                                        item.ErrorData["Color Code"].Add($"Invalid Color Code '{colorCodeStr}'");
                                    }
                                    else
                                    {
                                        var mtrcolorcode = color.Where(a => a.Code.ToString() == colorCodeStr.ToString() && a.DivisionId.Split(',').Select(d => d.Trim()).Contains(templateDivisionId)).ToList();
                                        if (!mtrcolorcode.Any())
                                        {
                                            if (!item.ErrorData.ContainsKey("Color Code"))
                                            {
                                                item.ErrorData.Add("Color Code", new List<string>());
                                            }

                                            item.ErrorData["Color Code"].Add($"Division Mismatched for Color Code {colorCodeStr}");
                                        }
                                    }
                                }
                            }
                        }
                    });
                }

                // Buyer Template 1
                if (!template.IsUidTemplate)
                {
                    Parallel.ForEach(data, item =>
                    {
                        if (item.ColumnData.ContainsKey("Style Name"))
                        {
                            var styleName = item.ColumnData["Style Name"].ToString();
                            if (string.IsNullOrWhiteSpace(styleName))
                            {
                                if (!item.ErrorData.ContainsKey("Style Name"))
                                {
                                    item.ErrorData.Add("Style Name", new List<string>());
                                }

                                item.ErrorData["Style Name"].Add("Style Name Field is required");
                            }
                        }

                        if (item.ColumnData.ContainsKey("Launch Date"))
                        {
                            var launchDateString = item.ColumnData["Launch Date"]?.ToString();

                            if (string.IsNullOrWhiteSpace(launchDateString))
                            {
                                if (!item.ErrorData.ContainsKey("Launch Date"))
                                {
                                    item.ErrorData.Add("Launch Date", new List<string>());
                                }

                                item.ErrorData["Launch Date"].Add("Launch Date Field is Required");
                            }
                            else
                            {
                                // Remove the time part if present
                                int indexOfSpace = launchDateString.IndexOf(' ');
                                if (indexOfSpace > 0)
                                {
                                    launchDateString = launchDateString.Substring(0, indexOfSpace);
                                }

                                string[] dateFormats = { "d/M/yyyy", "dd/MM/yyyy", "d-M-yyyy", "dd-MM-yyyy", "MM/dd/yyyy" };

                                if (DateTime.TryParseExact(launchDateString, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime launchDate))
                                {
                                    launchDate.ToString("dd/MM/yyyy");

                                    if (launchDate.Date < DateTime.Today)
                                    {
                                        if (!item.ErrorData.ContainsKey("Launch Date"))
                                        {
                                            item.ErrorData.Add("Launch Date", new List<string>());
                                        }

                                        item.ErrorData["Launch Date"].Add($"The date entered must be today or a future date. Please enter a valid date: '{launchDate}'");
                                    }
                                }
                                else
                                {
                                    if (!item.ErrorData.ContainsKey("Launch Date"))
                                    {
                                        item.ErrorData.Add("Launch Date", new List<string>());
                                    }

                                    item.ErrorData["Launch Date"].Add($"Invalid Launch Date '{launchDateString}'");
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("MOQ"))
                        {
                            var moq = item.ColumnData["MOQ"].ToString();
                            if (string.IsNullOrWhiteSpace(moq))
                            {
                                if (!item.ErrorData.ContainsKey("MOQ"))
                                {
                                    item.ErrorData.Add("MOQ", new List<string>());
                                }

                                item.ErrorData["MOQ"].Add("MOQ Field is required");
                            }
                        }

                        if (templateDivisionId == "61")
                        {
                            if (item.ColumnData.ContainsKey("Product Validation Code"))
                            {
                                var productvalidation = item.ColumnData["Product Validation Code"].ToString();
                                if (string.IsNullOrWhiteSpace(productvalidation))
                                {
                                    if (!item.ErrorData.ContainsKey("Product Validation Code"))
                                    {
                                        item.ErrorData.Add("Product Validation Code", new List<string>());
                                    }

                                    item.ErrorData["Product Validation Code"].Add("Product Validation Code Field is required");
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Item Desc"))
                        {
                            var itemDesc = item.ColumnData["Item Desc"].ToString();
                            if (string.IsNullOrWhiteSpace(itemDesc))
                            {
                                if (!item.ErrorData.ContainsKey("Item Desc"))
                                {
                                    item.ErrorData.Add("Item Desc", new List<string>());
                                }

                                item.ErrorData["Item Desc"].Add("Item Desc Field is required");
                            }
                        }

                        if (item.ColumnData.ContainsKey("Specific Product Name"))
                        {
                            var specificProduct = item.ColumnData["Specific Product Name"].ToString();
                            if (string.IsNullOrWhiteSpace(specificProduct))
                            {
                                if (!item.ErrorData.ContainsKey("Specific Product Name"))
                                {
                                    item.ErrorData.Add("Specific Product Name", new List<string>());
                                }

                                item.ErrorData["Specific Product Name"].Add("Specific Product Name Field is required");
                            }
                        }

                        if (item.ColumnData.ContainsKey("UOM"))
                        {
                            var uom = item.ColumnData["UOM"].ToString();
                            if (string.IsNullOrWhiteSpace(uom))
                            {
                                if (!item.ErrorData.ContainsKey("UOM"))
                                {
                                    item.ErrorData.Add("UOM", new List<string>());
                                }

                                item.ErrorData["UOM"].Add("UOM Field is required");
                            }
                            else
                            {
                                var uomCheck = setpcs.Where(a => a.Code.ToString() == uom).ToList();

                                if (!uomCheck.Any())
                                {
                                    if (!item.ErrorData.ContainsKey("UOM"))
                                    {
                                        item.ErrorData.Add("UOM", new List<string>());
                                    }

                                    item.ErrorData["UOM"].Add($"Invalid UOM '{uom}'");
                                }
                            }
                        }

                        if (templateDivisionId == "4" || templateDivisionId == "65")
                        {
                            if (item.ColumnData.ContainsKey("Process Techniques"))
                            {
                                var process = item.ColumnData["Process Techniques"].ToString();
                                if (string.IsNullOrWhiteSpace(process))
                                {
                                    if (!item.ErrorData.ContainsKey("Process Techniques"))
                                    {
                                        item.ErrorData.Add("Process Techniques", new List<string>());
                                    }

                                    item.ErrorData["Process Techniques"].Add("Process Techniques Field is required");
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Loipl Shoot Required"))
                        {
                            var loipl = item.ColumnData["Loipl Shoot Required"].ToString();
                            string[] ipl = { "Yes", "No" };
                            if (string.IsNullOrWhiteSpace(loipl))
                            {
                                if (!item.ErrorData.ContainsKey("Loipl Shoot Required"))
                                {
                                    item.ErrorData.Add("Loipl Shoot Required", new List<string>());
                                }

                                item.ErrorData["Loipl Shoot Required"].Add("Loipl Shoot Required Field is required");
                            }
                            else
                            {
                                if (!ipl.Contains(loipl))
                                {
                                    if (!item.ErrorData.ContainsKey("Loipl Shoot Required"))
                                    {
                                        item.ErrorData.Add("Loipl Shoot Required", new List<string>());
                                    }

                                    item.ErrorData["Loipl Shoot Required"].Add($"Invalid Loipl Shoot Required '{loipl}'");
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("Material Description"))
                        {
                            var materialDesc = item.ColumnData["Material Description"].ToString();
                            if (string.IsNullOrWhiteSpace(materialDesc))
                            {
                                if (!item.ErrorData.ContainsKey("Material Description"))
                                {
                                    item.ErrorData.Add("Material Description", new List<string>());
                                }

                                item.ErrorData["Material Description"].Add("Material Description Field is required");
                            }
                        }

                        if (item.ColumnData.ContainsKey("MRP"))
                        {
                            var mrp = item.ColumnData["MRP"];
                            if (string.IsNullOrWhiteSpace(mrp.ToString()))
                            {
                                if (!item.ErrorData.ContainsKey("MRP"))
                                {
                                    item.ErrorData.Add("MRP", new List<string>());
                                }

                                item.ErrorData["MRP"].Add("MRP Field is required");
                            }
                            else
                            {
                                if (mrp != null && decimal.TryParse(mrp.ToString(), out decimal mrpValue))
                                {
                                    if (mrpValue <= 0 || mrpValue % 1 != 0)
                                    {
                                        if (!item.ErrorData.ContainsKey("MRP"))
                                        {
                                            item.ErrorData.Add("MRP", new List<string>());
                                        }

                                        item.ErrorData["MRP"].Add("MRP Field is expected to be a number");
                                    }
                                }
                                else
                                {
                                    if (mrp is string)
                                    {
                                        if (!item.ErrorData.ContainsKey("MRP"))
                                        {
                                            item.ErrorData.Add("MRP", new List<string>());
                                        }

                                        item.ErrorData["MRP"].Add($"Special characters are present in MRP '{mrp}'");
                                    }
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("EXW FOB Cost"))
                        {
                            var fob = item.ColumnData["EXW FOB Cost"];
                            if (string.IsNullOrWhiteSpace(fob.ToString()))
                            {
                                if (!item.ErrorData.ContainsKey("EXW FOB Cost"))
                                {
                                    item.ErrorData.Add("EXW FOB Cost", new List<string>());
                                }

                                item.ErrorData["EXW FOB Cost"].Add("EXW FOB Cost Field is required");
                            }
                            else
                            {
                                if (fob != null && decimal.TryParse(fob.ToString(), out decimal fobValue))
                                {
                                    if (fobValue <= 0 || fobValue % 1 != 0)
                                    {
                                        if (!item.ErrorData.ContainsKey("EXW FOB Cost"))
                                        {
                                            item.ErrorData.Add("EXW FOB Cost", new List<string>());
                                        }

                                        item.ErrorData["EXW FOB Cost"].Add("EXW FOB Cost Field is expected to be a number");
                                    }
                                }
                                else
                                {
                                    if (fob is string)
                                    {
                                        if (!item.ErrorData.ContainsKey("EXW FOB Cost"))
                                        {
                                            item.ErrorData.Add("EXW FOB Cost", new List<string>());
                                        }

                                        item.ErrorData["EXW FOB Cost"].Add($"Special characters are present in EXW FOB Cost '{fob}'");
                                    }
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("HSN Code"))
                        {
                            var hsn = item.ColumnData["HSN Code"];

                            if (hsn != null && decimal.TryParse(hsn.ToString(), out decimal hsnValue))
                            {
                                if (hsnValue <= 0 || hsnValue % 1 != 0)
                                {
                                    if (!item.ErrorData.ContainsKey("HSN Code"))
                                    {
                                        item.ErrorData.Add("HSN Code", new List<string>());
                                    }

                                    item.ErrorData["HSN Code"].Add("HSN Code Field is expected to be a number");
                                }
                            }

                            if (string.IsNullOrWhiteSpace(hsn.ToString()))
                            {
                                if (!item.ErrorData.ContainsKey("HSN Code"))
                                {
                                    item.ErrorData.Add("HSN Code", new List<string>());
                                }

                                item.ErrorData["HSN Code"].Add("HSN Code Field is required");
                            }
                            else
                            {
                                if (hsn is string)
                                {
                                    if (!item.ErrorData.ContainsKey("HSN Code"))
                                    {
                                        item.ErrorData.Add("HSN Code", new List<string>());
                                    }

                                    item.ErrorData["HSN Code"].Add($"Special characters are present in HSN Code '{hsn}'");
                                }
                                else
                                {
                                    var hsnCheck = hsnCode.Where(a => a.Code == hsn.ToString()).ToList();
                                    if (!hsnCheck.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("HSN Code"))
                                        {
                                            item.ErrorData.Add("HSN Code", new List<string>());
                                        }

                                        item.ErrorData["HSN Code"].Add($"Invalid HSN Code '{hsn}'");
                                    }
                                }
                            }
                        }

                        if (item.ColumnData.ContainsKey("GST %"))
                        {
                            var gst = item.ColumnData["GST %"];
                            if (string.IsNullOrWhiteSpace(gst.ToString()))
                            {
                                if (!item.ErrorData.ContainsKey("GST %"))
                                {
                                    item.ErrorData.Add("GST %", new List<string>());
                                }

                                item.ErrorData["GST %"].Add("GST % Field is required");
                            }
                            else
                            {
                                if (gst != null && decimal.TryParse(gst.ToString(), out decimal gstValue))
                                {
                                    if (gstValue <= 0 || gstValue % 1 != 0)
                                    {
                                        if (!item.ErrorData.ContainsKey("GST %"))
                                        {
                                            item.ErrorData.Add("GST %", new List<string>());
                                        }

                                        item.ErrorData["GST %"].Add("GST % Field is expected to be a number");
                                    }
                                }
                                else
                                {
                                    if (gst is string)
                                    {
                                        if (!item.ErrorData.ContainsKey("GST %"))
                                        {
                                            item.ErrorData.Add("GST %", new List<string>());
                                        }

                                        item.ErrorData["GST %"].Add($"Special characters are present in GST % '{gst}'");
                                    }
                                }
                            }
                        }
                    });

                    // Buyer Template - 1 Optional field data type validation
                    if (!template.IsUidTemplate)
                    {
                        Parallel.ForEach(data, item =>
                        {
                            if (item.ColumnData.ContainsKey("Net Qty As Per MRP Label"))
                            {
                                var netQty = item.ColumnData["Net Qty As Per MRP Label"];

                                if (string.IsNullOrWhiteSpace(netQty.ToString()))
                                {
                                    if (!item.ErrorData.ContainsKey("Net Qty As Per MRP Label"))
                                    {
                                        item.ErrorData.Add("Net Qty As Per MRP Label", new List<string>());
                                    }

                                    item.ErrorData["Net Qty As Per MRP Label"].Add("Net Qty As Per MRP Label Field is Required");
                                }
                                else if (netQty != null && decimal.TryParse(netQty.ToString(), out decimal netValue))
                                {
                                    if (netValue <= 0 || netValue % 1 != 0)
                                    {
                                        if (!item.ErrorData.ContainsKey("Net Qty As Per MRP Label"))
                                        {
                                            item.ErrorData.Add("Net Qty As Per MRP Label", new List<string>());
                                        }

                                        item.ErrorData["Net Qty As Per MRP Label"].Add("Net Qty As Per MRP Label Field is expected to be a number");
                                    }
                                }
                                else
                                {
                                    if (netQty is string)
                                    {
                                        if (!item.ErrorData.ContainsKey("Net Qty As Per MRP Label"))
                                        {
                                            item.ErrorData.Add("Net Qty As Per MRP Label", new List<string>());
                                        }

                                        item.ErrorData["Net Qty As Per MRP Label"].Add($"Special characters are present in Net Qty As Per MRP Label '{netQty}'");
                                    }
                                }
                            }

                            if (templateDivisionId == "61")
                            {
                                if (item.ColumnData.ContainsKey("Pack Contents"))
                                {
                                    var packContent = item.ColumnData["Pack Contents"];

                                    if (string.IsNullOrWhiteSpace(packContent.ToString()))
                                    {
                                        if (!item.ErrorData.ContainsKey("Pack Contents"))
                                        {
                                            item.ErrorData.Add("Pack Contents", new List<string>());
                                        }

                                        item.ErrorData["Pack Contents"].Add("Pack Contents Field is Required");
                                    }
                                }
                            }

                            if (item.ColumnData.ContainsKey("MOQ Remark 1"))
                            {
                                var moqremark = item.ColumnData["MOQ Remark 1"];

                                if (string.IsNullOrWhiteSpace(moqremark.ToString()))
                                {
                                    if (!item.ErrorData.ContainsKey("MOQ Remark 1"))
                                    {
                                        item.ErrorData.Add("MOQ Remark 1", new List<string>());
                                    }

                                    item.ErrorData["MOQ Remark 1"].Add("MOQ Remark 1 Field is Required");
                                }
                            }

                            if (item.ColumnData.ContainsKey("Currency"))
                            {
                                var currency = item.ColumnData["Currency"];

                                if (string.IsNullOrWhiteSpace(currency.ToString()))
                                {
                                    if (!item.ErrorData.ContainsKey("Currency"))
                                    {
                                        item.ErrorData.Add("Currency", new List<string>());
                                    }

                                    item.ErrorData["Currency"].Add("Currency Field is Required");
                                }
                                else
                                {
                                    var currency_value = mtrcurrency.Where(a => string.Equals(a.Name, currency.ToString(), StringComparison.OrdinalIgnoreCase)).ToList();
                                    if (!currency_value.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("Currency"))
                                        {
                                            item.ErrorData.Add("Currency", new List<string>());
                                        }

                                        item.ErrorData["Currency"].Add($"Invalid Currency '{currency}'");
                                    }
                                }
                            }

                            if (item.ColumnData.ContainsKey("Item Shape"))
                            {
                                var item_shape = item.ColumnData["Item Shape"];
                                if (!string.IsNullOrWhiteSpace(item_shape.ToString()))
                                {
                                    var shape = mtritemShape.Where(a => string.Equals(a.Name, item_shape.ToString(), StringComparison.OrdinalIgnoreCase)).ToList();
                                    if (!shape.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("Item Shape"))
                                        {
                                            item.ErrorData.Add("Item Shape", new List<string>());
                                        }

                                        item.ErrorData["Item Shape"].Add($"Invalid Item Shape '{item_shape}'");
                                    }
                                }
                            }

                            if (item.ColumnData.ContainsKey("Cost Based MRP Ind"))
                            {
                                var costBased = item.ColumnData["Cost Based MRP Ind"];

                                if (string.IsNullOrWhiteSpace(costBased.ToString()))
                                {
                                    if (!item.ErrorData.ContainsKey("Cost Based MRP Ind"))
                                    {
                                        item.ErrorData.Add("Cost Based MRP Ind", new List<string>());
                                    }

                                    item.ErrorData["Cost Based MRP Ind"].Add("Cost Based MRP Ind Field is Required");
                                }
                            }

                            if (templateDivisionId == "4" || templateDivisionId == "65")
                            {
                                if (item.ColumnData.ContainsKey("Lead Time Days"))
                                {
                                    var leadTime = item.ColumnData["Lead Time Days"];

                                    if (!string.IsNullOrWhiteSpace(leadTime.ToString()))
                                    {
                                        var leadDays = mtrleadtime.Where(a => string.Equals(a.Name, leadTime.ToString(), StringComparison.OrdinalIgnoreCase)).ToList();
                                        if (!leadDays.Any())
                                        {
                                            if (!item.ErrorData.ContainsKey("Lead Time Days"))
                                            {
                                                item.ErrorData.Add("Lead Time Days", new List<string>());
                                            }

                                            item.ErrorData["Lead Time Days"].Add($"Invalid Lead Time Days '{leadTime}'");
                                        }
                                    }
                                }
                            }

                            if (item.ColumnData.ContainsKey("Item Type code"))
                            {
                                var itemTypeCode = item.ColumnData["Item Type code"];

                                if (string.IsNullOrWhiteSpace(itemTypeCode.ToString()))
                                {
                                    if (!item.ErrorData.ContainsKey("Item Type code"))
                                    {
                                        item.ErrorData.Add("Item Type code", new List<string>());
                                    }

                                    item.ErrorData["Item Type code"].Add("Item Type code Field is Required");
                                }
                                else
                                {
                                    var productCheck = mtritemtypecode.Where(a => a.Code.ToString() == itemTypeCode.ToString()).ToList();

                                    if (!productCheck.Any())
                                    {
                                        if (!item.ErrorData.ContainsKey("Item Type code"))
                                        {
                                            item.ErrorData.Add("Item Type code", new List<string>());
                                        }

                                        item.ErrorData["Item Type code"].Add($"Invalid Item Type code '{itemTypeCode}'");
                                    }
                                    else
                                    {
                                        var mtritemtypecodes = mtritemtypecode.Where(a => a.Code.ToString() == itemTypeCode.ToString() && a.DivisionId.Split(',').Select(d => d.Trim()).Contains(templateDivisionId)).ToList();
                                        if (!mtritemtypecodes.Any())
                                        {
                                            if (!item.ErrorData.ContainsKey("Item Type code"))
                                            {
                                                item.ErrorData.Add("Item Type code", new List<string>());
                                            }

                                            item.ErrorData["Item Type code"].Add($"Division Mismatched for Item Type code {itemTypeCode}");
                                        }
                                    }
                                }
                            }

                            if (templateDivisionId == "61")
                            {
                                if (item.ColumnData.ContainsKey("Lead Time Days"))
                                {
                                    var leadTime = item.ColumnData["Lead Time Days"];

                                    if (string.IsNullOrWhiteSpace(leadTime.ToString()))
                                    {
                                        if (!item.ErrorData.ContainsKey("Lead Time Days"))
                                        {
                                            item.ErrorData.Add("Lead Time Days", new List<string>());
                                        }

                                        item.ErrorData["Lead Time Days"].Add("Lead Time Days Field is Required");
                                    }
                                    else
                                    {
                                        var leadDays = mtrleadtime.Where(a => string.Equals(a.Name, leadTime.ToString(), StringComparison.OrdinalIgnoreCase)).ToList();
                                        if (!leadDays.Any())
                                        {
                                            if (!item.ErrorData.ContainsKey("Lead Time Days"))
                                            {
                                                item.ErrorData.Add("Lead Time Days", new List<string>());
                                            }

                                            item.ErrorData["Lead Time Days"].Add($"Invalid Lead Time Days '{leadTime}'");
                                        }
                                    }
                                }
                            }

                            if (item.ColumnData.ContainsKey("Initital Product Size (L) (CM)"))
                            {
                                var initialL = item.ColumnData["Initital Product Size (L) (CM)"];
                                if (string.IsNullOrWhiteSpace(initialL.ToString()))
                                {
                                    if (!item.ErrorData.ContainsKey("Initital Product Size (L) (CM)"))
                                    {
                                        item.ErrorData.Add("Initital Product Size (L) (CM)", new List<string>());
                                    }

                                    item.ErrorData["Initital Product Size (L) (CM)"].Add("Initital Product Size (L) (CM) Field is Required");
                                }

                                // else if (initialL != null && decimal.TryParse(initialL.ToString(), out decimal initialLValue))
                                // {
                                //    if (initialLValue <= 0 || initialLValue % 1 != 0)
                                //    {
                                //        if (!item.ErrorData.ContainsKey("Initital Product Size (L) (CM)"))
                                //        {
                                //            item.ErrorData.Add("Initital Product Size (L) (CM)", new List<string>());
                                //        }

                                // item.ErrorData["Initital Product Size (L) (CM)"].Add("Initital Product Size (L) (CM) Field is expected to be a number");
                                //    }
                                // }
                                else
                                {
                                    if (initialL is string)
                                    {
                                        if (!item.ErrorData.ContainsKey("Initital Product Size (L) (CM)"))
                                        {
                                            item.ErrorData.Add("Initital Product Size (L) (CM)", new List<string>());
                                        }

                                        item.ErrorData["Initital Product Size (L) (CM)"].Add($"Special characters are present in Initital Product Size (L) (CM) '{initialL}'");
                                    }
                                }
                            }

                            if (item.ColumnData.ContainsKey("Initital Product Size (W) (CM)"))
                            {
                                var initialW = item.ColumnData["Initital Product Size (W) (CM)"];
                                if (string.IsNullOrWhiteSpace(initialW.ToString()))
                                {
                                    if (!item.ErrorData.ContainsKey("Initital Product Size (W) (CM)"))
                                    {
                                        item.ErrorData.Add("Initital Product Size (W) (CM)", new List<string>());
                                    }

                                    item.ErrorData["Initital Product Size (W) (CM)"].Add("Initital Product Size (W) (CM) Field is Required");
                                }

                                // else if (initialW != null && decimal.TryParse(initialW.ToString(), out decimal initialWValue))
                                // {
                                //    if (initialWValue <= 0 || initialWValue % 1 != 0)
                                //    {
                                //        if (!item.ErrorData.ContainsKey("Initital Product Size (W) (CM)"))
                                //        {
                                //            item.ErrorData.Add("Initital Product Size (W) (CM)", new List<string>());
                                //        }

                                // item.ErrorData["Initital Product Size (W) (CM)"].Add("Initital Product Size (W) (CM) Field is expected to be a number");
                                //    }
                                // }
                                else
                                {
                                    if (initialW is string)
                                    {
                                        if (!item.ErrorData.ContainsKey("Initital Product Size (W) (CM)"))
                                        {
                                            item.ErrorData.Add("Initital Product Size (W) (CM)", new List<string>());
                                        }

                                        item.ErrorData["Initital Product Size (W) (CM)"].Add($"Special characters are present in Initital Product Size (W) (CM) '{initialW}'");
                                    }
                                }
                            }

                            if (item.ColumnData.ContainsKey("Initital Product Size (H) (CM)"))
                            {
                                var initialH = item.ColumnData["Initital Product Size (H) (CM)"];
                                if (string.IsNullOrWhiteSpace(initialH.ToString()))
                                {
                                    if (!item.ErrorData.ContainsKey("Initital Product Size (H) (CM)"))
                                    {
                                        item.ErrorData.Add("Initital Product Size (H) (CM)", new List<string>());
                                    }

                                    item.ErrorData["Initital Product Size (H) (CM)"].Add("Initital Product Size (H) (CM) Field is Required");
                                }

                                // else if (initialH != null && decimal.TryParse(initialH.ToString(), out decimal initialHValue))
                                // {
                                //    if (initialHValue <= 0 || initialHValue % 1 != 0)
                                //    {
                                //        if (!item.ErrorData.ContainsKey("Initital Product Size (H) (CM)"))
                                //        {
                                //            item.ErrorData.Add("Initital Product Size (H) (CM)", new List<string>());
                                //        }
                                //        item.ErrorData["Initital Product Size (H) (CM)"].Add("Initital Product Size (H) (CM) Field is expected to be a number");
                                //    }
                                // }
                                else
                                {
                                    if (initialH is string)
                                    {
                                        if (!item.ErrorData.ContainsKey("Initital Product Size (H) (CM)"))
                                        {
                                            item.ErrorData.Add("Initital Product Size (H) (CM)", new List<string>());
                                        }

                                        item.ErrorData["Initital Product Size (H) (CM)"].Add($"Special characters are present in Initital Product Size (H) (CM) '{initialH}'");
                                    }
                                }
                            }

                            if (item.ColumnData.ContainsKey("Planned Discount"))
                            {
                                var plannedDiscount = item.ColumnData["Planned Discount"];
                                if (!string.IsNullOrWhiteSpace(plannedDiscount.ToString()))
                                {
                                    if (plannedDiscount != null && decimal.TryParse(plannedDiscount.ToString(), out decimal plannedValue))
                                    {
                                        if (plannedValue <= 0 || plannedValue % 1 != 0)
                                        {
                                            if (!item.ErrorData.ContainsKey("Planned Discount"))
                                            {
                                                item.ErrorData.Add("Planned Discount", new List<string>());
                                            }

                                            item.ErrorData["Planned Discount"].Add("Planned Discount Field is expected to be a number");
                                        }
                                    }
                                    else
                                    {
                                        if (plannedDiscount is string)
                                        {
                                            if (!item.ErrorData.ContainsKey("Planned Discount"))
                                            {
                                                item.ErrorData.Add("Planned Discount", new List<string>());
                                            }

                                            item.ErrorData["Planned Discount"].Add($"Special characters are present in Planned Discount '{plannedDiscount}'");
                                        }
                                    }
                                }
                            }

                            if (item.ColumnData.ContainsKey("BAU Discount"))
                            {
                                var bauDiscount = item.ColumnData["BAU Discount"];
                                if (!string.IsNullOrWhiteSpace(bauDiscount.ToString()))
                                {
                                    if (bauDiscount != null && decimal.TryParse(bauDiscount.ToString(), out decimal bauValue))
                                    {
                                        if (bauValue <= 0 || bauValue % 1 != 0)
                                        {
                                            if (!item.ErrorData.ContainsKey("BAU Discount"))
                                            {
                                                item.ErrorData.Add("BAU Discount", new List<string>());
                                            }

                                            item.ErrorData["BAU Discount"].Add("BAU Discount Field is expected to be a number");
                                        }
                                    }
                                    else
                                    {
                                        if (bauDiscount is string)
                                        {
                                            if (!item.ErrorData.ContainsKey("BAU Discount"))
                                            {
                                                item.ErrorData.Add("BAU Discount", new List<string>());
                                            }

                                            item.ErrorData["BAU Discount"].Add($"Special characters are present in BAU Discount '{bauDiscount}'");
                                        }
                                    }
                                }
                            }

                            if (item.ColumnData.ContainsKey("Ref SKU 1"))
                            {
                                var refsku1 = item.ColumnData["Ref SKU 1"];
                                if (!string.IsNullOrWhiteSpace(refsku1.ToString()))
                                {
                                    if (refsku1 != null && decimal.TryParse(refsku1.ToString(), out decimal sku1Value))
                                    {
                                        if (sku1Value <= 0 || sku1Value % 1 != 0)
                                        {
                                            if (!item.ErrorData.ContainsKey("Ref SKU 1"))
                                            {
                                                item.ErrorData.Add("Ref SKU 1", new List<string>());
                                            }

                                            item.ErrorData["Ref SKU 1"].Add("Ref SKU 1 Field is expected to be a number");
                                        }
                                    }
                                    else
                                    {
                                        if (refsku1 is string)
                                        {
                                            if (!item.ErrorData.ContainsKey("Ref SKU 1"))
                                            {
                                                item.ErrorData.Add("Ref SKU 1", new List<string>());
                                            }

                                            item.ErrorData["Ref SKU 1"].Add($"Special characters are present in Ref SKU 1 '{refsku1}'");
                                        }
                                    }
                                }
                            }

                            if (item.ColumnData.ContainsKey("Ref SKU 2"))
                            {
                                var refsku2 = item.ColumnData["Ref SKU 2"];
                                if (!string.IsNullOrWhiteSpace(refsku2.ToString()))
                                {
                                    if (refsku2 != null && decimal.TryParse(refsku2.ToString(), out decimal sku2Value))
                                    {
                                        if (sku2Value <= 0 || sku2Value % 1 != 0)
                                        {
                                            if (!item.ErrorData.ContainsKey("Ref SKU 2"))
                                            {
                                                item.ErrorData.Add("Ref SKU 2", new List<string>());
                                            }

                                            item.ErrorData["Ref SKU 2"].Add("Ref SKU 2 Field is expected to be a number");
                                        }
                                    }
                                    else
                                    {
                                        if (refsku2 is string)
                                        {
                                            if (!item.ErrorData.ContainsKey("Ref SKU 2"))
                                            {
                                                item.ErrorData.Add("Ref SKU 2", new List<string>());
                                            }

                                            item.ErrorData["Ref SKU 2"].Add($"Special characters are present in Ref SKU 2 '{refsku2}'");
                                        }
                                    }
                                }
                            }

                            if (item.ColumnData.ContainsKey("No Of Stores Planned"))
                            {
                                var noOfStored = item.ColumnData["No Of Stores Planned"];
                                if (string.IsNullOrWhiteSpace(noOfStored.ToString()))
                                {
                                    if (!item.ErrorData.ContainsKey("No Of Stores Planned"))
                                    {
                                        item.ErrorData.Add("No Of Stores Planned", new List<string>());
                                    }

                                    item.ErrorData["No Of Stores Planned"].Add("No Of Stores Planned Field is Required");
                                }
                                else if (noOfStored != null && decimal.TryParse(noOfStored.ToString(), out decimal storeValue))
                                {
                                    if (storeValue <= 0 || storeValue % 1 != 0)
                                    {
                                        if (!item.ErrorData.ContainsKey("No Of Stores Planned"))
                                        {
                                            item.ErrorData.Add("No Of Stores Planned", new List<string>());
                                        }

                                        item.ErrorData["No Of Stores Planned"].Add("No Of Stores Planned Field is expected to be a number");
                                    }
                                }
                                else
                                {
                                    if (noOfStored is string)
                                    {
                                        if (!item.ErrorData.ContainsKey("No Of Stores Planned"))
                                        {
                                            item.ErrorData.Add("No Of Stores Planned", new List<string>());
                                        }

                                        item.ErrorData["No Of Stores Planned"].Add($"Special characters are present in No Of Stores Planned '{noOfStored}'");
                                    }
                                }
                            }

                            if (templateDivisionId == "61")
                            {
                                if (item.ColumnData.ContainsKey("Planogram Fitment"))
                                {
                                    var planoFitment = item.ColumnData["Planogram Fitment"];
                                    if (string.IsNullOrWhiteSpace(planoFitment.ToString()))
                                    {
                                        if (!item.ErrorData.ContainsKey("Planogram Fitment"))
                                        {
                                            item.ErrorData.Add("Planogram Fitment", new List<string>());
                                        }

                                        item.ErrorData["Planogram Fitment"].Add("Planogram Fitment Field is Required");
                                    }
                                    else if (planoFitment != null && decimal.TryParse(planoFitment.ToString(), out decimal fitmentValue))
                                    {
                                        if (fitmentValue <= 0 || fitmentValue % 1 != 0)
                                        {
                                            if (!item.ErrorData.ContainsKey("Planogram Fitment"))
                                            {
                                                item.ErrorData.Add("Planogram Fitment", new List<string>());
                                            }

                                            item.ErrorData["Planogram Fitment"].Add("Planogram Fitment Field is expected to be a number");
                                        }
                                    }
                                    else
                                    {
                                        if (planoFitment is string)
                                        {
                                            if (!item.ErrorData.ContainsKey("Planogram Fitment"))
                                            {
                                                item.ErrorData.Add("Planogram Fitment", new List<string>());
                                            }

                                            item.ErrorData["Planogram Fitment"].Add($"Special characters are present in Planogram Fitment '{planoFitment}'");
                                        }
                                    }
                                }
                            }
                            else if (templateDivisionId == "4" || templateDivisionId == "65")
                            {
                                if (item.ColumnData.ContainsKey("Planogram Fitment"))
                                {
                                    var planoFitment = item.ColumnData["Planogram Fitment"];
                                    if (!string.IsNullOrWhiteSpace(planoFitment.ToString()))
                                    {
                                        if (planoFitment != null && decimal.TryParse(planoFitment.ToString(), out decimal fitmentValue))
                                        {
                                            if (fitmentValue <= 0 || fitmentValue % 1 != 0)
                                            {
                                                if (!item.ErrorData.ContainsKey("Planogram Fitment"))
                                                {
                                                    item.ErrorData.Add("Planogram Fitment", new List<string>());
                                                }

                                                item.ErrorData["Planogram Fitment"].Add("Planogram Fitment Field is expected to be a number");
                                            }
                                        }
                                        else
                                        {
                                            if (planoFitment is string)
                                            {
                                                if (!item.ErrorData.ContainsKey("Planogram Fitment"))
                                                {
                                                    item.ErrorData.Add("Planogram Fitment", new List<string>());
                                                }

                                                item.ErrorData["Planogram Fitment"].Add($"Special characters are present in Planogram Fitment '{planoFitment}'");
                                            }
                                        }
                                    }
                                }
                            }

                            if (item.ColumnData.ContainsKey("Margin %"))
                            {
                                var margin = item.ColumnData["Margin %"];
                                if (!string.IsNullOrWhiteSpace(margin.ToString()))
                                {
                                    if (margin != null && decimal.TryParse(margin.ToString(), out decimal marginValue))
                                    {
                                        if (marginValue <= 0 || marginValue % 1 != 0)
                                        {
                                            if (!item.ErrorData.ContainsKey("Margin %"))
                                            {
                                                item.ErrorData.Add("Margin %", new List<string>());
                                            }

                                            item.ErrorData["Margin %"].Add("Margin % Field is expected to be a number");
                                        }
                                    }
                                    else
                                    {
                                        if (margin is string)
                                        {
                                            if (!item.ErrorData.ContainsKey("Margin %"))
                                            {
                                                item.ErrorData.Add("Margin %", new List<string>());
                                            }

                                            item.ErrorData["Margin %"].Add($"Special characters are present in Margin % '{margin}'");
                                        }
                                    }
                                }
                            }

                            if (item.ColumnData.ContainsKey("RRP"))
                            {
                                var rrp = item.ColumnData["RRP"];
                                if (!string.IsNullOrWhiteSpace(rrp.ToString()))
                                {
                                    if (rrp != null && decimal.TryParse(rrp.ToString(), out decimal rrpValue))
                                    {
                                        if (rrpValue <= 0 || rrpValue % 1 != 0)
                                        {
                                            if (!item.ErrorData.ContainsKey("RRP"))
                                            {
                                                item.ErrorData.Add("RRP", new List<string>());
                                            }

                                            item.ErrorData["RRP"].Add("RRP Field is expected to be a number");
                                        }
                                    }
                                    else
                                    {
                                        if (rrp is string)
                                        {
                                            if (!item.ErrorData.ContainsKey("RRP"))
                                            {
                                                item.ErrorData.Add("RRP", new List<string>());
                                            }

                                            item.ErrorData["RRP"].Add($"Special characters are present in Margin % '{rrp}'");
                                        }
                                    }
                                }
                            }
                        });
                    }

                    // Buyer Template 2
                    if (!template.IsUidTemplate)
                    {
                        Parallel.ForEach(data, item =>
                        {
                            if (item.ColumnData.ContainsKey("Sub Range Collection Name"))
                            {
                                var subRange = item.ColumnData["Sub Range Collection Name"].ToString();
                                if (string.IsNullOrWhiteSpace(subRange))
                                {
                                    if (!item.ErrorData.ContainsKey("Sub Range Collection Name"))
                                    {
                                        item.ErrorData.Add("Sub Range Collection Name", new List<string>());
                                    }

                                    item.ErrorData["Sub Range Collection Name"].Add("Sub Range Collection Name Field is required");
                                }
                            }

                            if (item.ColumnData.ContainsKey("Customer Centric Size"))
                            {
                                var customer = item.ColumnData["Customer Centric Size"].ToString();
                                if (string.IsNullOrWhiteSpace(customer))
                                {
                                    if (!item.ErrorData.ContainsKey("Customer Centric Size"))
                                    {
                                        item.ErrorData.Add("Customer Centric Size", new List<string>());
                                    }

                                    item.ErrorData["Customer Centric Size"].Add("Customer Centric Size Field is required");
                                }
                            }

                            if (item.ColumnData.ContainsKey("Primary Selling Feature"))
                            {
                                var selling = item.ColumnData["Primary Selling Feature"].ToString();
                                if (string.IsNullOrWhiteSpace(selling))
                                {
                                    if (!item.ErrorData.ContainsKey("Primary Selling Feature"))
                                    {
                                        item.ErrorData.Add("Primary Selling Feature", new List<string>());
                                    }

                                    item.ErrorData["Primary Selling Feature"].Add("Primary Selling Feature Field is required");
                                }
                            }

                            if (item.ColumnData.ContainsKey("Primary Material"))
                            {
                                var material = item.ColumnData["Primary Material"].ToString();
                                if (string.IsNullOrWhiteSpace(material))
                                {
                                    if (!item.ErrorData.ContainsKey("Primary Material"))
                                    {
                                        item.ErrorData.Add("Primary Material", new List<string>());
                                    }

                                    item.ErrorData["Primary Material"].Add("Primary Material Field is required");
                                }
                            }

                            if (item.ColumnData.ContainsKey("Buyers Thought"))
                            {
                                var buyer = item.ColumnData["Buyers Thought"].ToString();
                                if (string.IsNullOrWhiteSpace(buyer))
                                {
                                    if (!item.ErrorData.ContainsKey("Buyers Thought"))
                                    {
                                        item.ErrorData.Add("Buyers Thought", new List<string>());
                                    }

                                    item.ErrorData["Buyers Thought"].Add("Buyers Thought Field is required");
                                }
                            }

                            if (item.ColumnData.ContainsKey("Selling Tips"))
                            {
                                var selling = item.ColumnData["Selling Tips"].ToString();
                                if (string.IsNullOrWhiteSpace(selling))
                                {
                                    if (!item.ErrorData.ContainsKey("Selling Tips"))
                                    {
                                        item.ErrorData.Add("Selling Tips", new List<string>());
                                    }

                                    item.ErrorData["Selling Tips"].Add("Selling Tips Field is required");
                                }
                            }

                            if (item.ColumnData.ContainsKey("Is Fragile"))
                            {
                                var fragile = item.ColumnData["Is Fragile"].ToString();
                                if (string.IsNullOrWhiteSpace(fragile))
                                {
                                    if (!item.ErrorData.ContainsKey("Is Fragile"))
                                    {
                                        item.ErrorData.Add("Is Fragile", new List<string>());
                                    }

                                    item.ErrorData["Is Fragile"].Add("Is Fragile Field is required");
                                }
                            }

                            if (item.ColumnData.ContainsKey("First Not Before Date"))
                            {
                                var firstBefore = item.ColumnData["First Not Before Date"]?.ToString();

                                if (!string.IsNullOrWhiteSpace(firstBefore))
                                {
                                    if (!string.IsNullOrEmpty(firstBefore))
                                    {
                                        int indexOfSpace = firstBefore.IndexOf(' ');
                                        if (indexOfSpace > 0)
                                        {
                                            firstBefore = firstBefore.Substring(0, indexOfSpace);
                                        }
                                    }

                                    if (!(DateTime.TryParseExact(firstBefore, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) ||
                                          DateTime.TryParseExact(firstBefore, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)))
                                    {
                                        if (!item.ErrorData.ContainsKey("First Not Before Date"))
                                        {
                                            item.ErrorData.Add("First Not Before Date", new List<string>());
                                        }

                                        item.ErrorData["First Not Before Date"].Add($"Invalid First Not Before Date '{firstBefore}'");
                                    }
                                }
                            }
                        });

                        // Buyer Template - 2 optional data type Validation
                        if (!template.IsUidTemplate)
                        {
                            Parallel.ForEach(data, item =>
                            {
                                if (item.ColumnData.ContainsKey("Ref Sku For Floor Display"))
                                {
                                    var floorDisplay = item.ColumnData["Ref Sku For Floor Display"];
                                    if (!string.IsNullOrWhiteSpace(floorDisplay.ToString()))
                                    {
                                        if (floorDisplay != null && decimal.TryParse(floorDisplay.ToString(), out decimal floorValue))
                                        {
                                            if (floorValue <= 0 || floorValue % 1 != 0)
                                            {
                                                if (!item.ErrorData.ContainsKey("Ref Sku For Floor Display"))
                                                {
                                                    item.ErrorData.Add("Ref Sku For Floor Display", new List<string>());
                                                }

                                                item.ErrorData["Ref Sku For Floor Display"].Add("Ref Sku For Floor Display Field is expected to be a number");
                                            }
                                        }
                                        else
                                        {
                                            if (floorDisplay is string)
                                            {
                                                if (!item.ErrorData.ContainsKey("Ref Sku For Floor Display"))
                                                {
                                                    item.ErrorData.Add("Ref Sku For Floor Display", new List<string>());
                                                }

                                                item.ErrorData["Ref Sku For Floor Display"].Add($"Special characters are present in Ref Sku For Floor Display '{floorDisplay}'");
                                            }
                                        }
                                    }
                                }
                            });
                        }

                        // QC Template
                        if (!template.IsUidTemplate)
                        {
                            Parallel.ForEach(data, item =>
                            {
                                if (item.ColumnData.ContainsKey("Assembly Required"))
                                {
                                    var assembly = item.ColumnData["Assembly Required"].ToString();
                                    if (string.IsNullOrWhiteSpace(assembly))
                                    {
                                        if (!item.ErrorData.ContainsKey("Assembly Required"))
                                        {
                                            item.ErrorData.Add("Assembly Required", new List<string>());
                                        }

                                        item.ErrorData["Assembly Required"].Add("Assembly Required Field is required");
                                    }
                                }

                                if (item.ColumnData.ContainsKey("Product Dimension L (CM)"))
                                {
                                    var dimensionL = item.ColumnData["Product Dimension L (CM)"];
                                    if (string.IsNullOrWhiteSpace(dimensionL.ToString()))
                                    {
                                        if (!item.ErrorData.ContainsKey("Product Dimension L (CM)"))
                                        {
                                            item.ErrorData.Add("Product Dimension L (CM)", new List<string>());
                                        }

                                        item.ErrorData["Product Dimension L (CM)"].Add("Product Dimension L (CM) Field is required");
                                    }
                                    else
                                    {
                                        // if (dimensionL != null && decimal.TryParse(dimensionL.ToString(), out decimal dimensionLValue))
                                        // {
                                        //    if (dimensionLValue <= 0 || dimensionLValue % 1 != 0)
                                        //    {
                                        //        if (!item.ErrorData.ContainsKey("Product Dimension L (CM)"))
                                        //        {
                                        //            item.ErrorData.Add("Product Dimension L (CM)", new List<string>());
                                        //        }
                                        //        item.ErrorData["Product Dimension L (CM)"].Add("Product Dimension L (CM) Field is expected to be a number");
                                        //    }
                                        // }
                                        // else
                                        // {
                                        if (dimensionL is string)
                                        {
                                            if (!item.ErrorData.ContainsKey("Product Dimension L (CM)"))
                                            {
                                                item.ErrorData.Add("Product Dimension L (CM)", new List<string>());
                                            }

                                            item.ErrorData["Product Dimension L (CM)"].Add($"Special characters are present in Product Dimension L (CM) '{dimensionL}'");
                                        }

                                        // }
                                    }
                                }

                                if (item.ColumnData.ContainsKey("Product Dimension W (CM)"))
                                {
                                    var dimensionW = item.ColumnData["Product Dimension W (CM)"];
                                    if (string.IsNullOrWhiteSpace(dimensionW.ToString()))
                                    {
                                        if (!item.ErrorData.ContainsKey("Product Dimension W (CM)"))
                                        {
                                            item.ErrorData.Add("Product Dimension W (CM)", new List<string>());
                                        }

                                        item.ErrorData["Product Dimension W (CM)"].Add("Product Dimension W (CM) Field is required");
                                    }
                                    else
                                    {
                                        // if (dimensionW != null && decimal.TryParse(dimensionW.ToString(), out decimal dimensionWValue))
                                        // {
                                        //    if (dimensionWValue <= 0 || dimensionWValue % 1 != 0)
                                        //    {
                                        //        if (!item.ErrorData.ContainsKey("Product Dimension W (CM)"))
                                        //        {
                                        //            item.ErrorData.Add("Product Dimension W (CM)", new List<string>());
                                        //        }
                                        //        item.ErrorData["Product Dimension W (CM)"].Add("Product Dimension W (CM) Field is expected to be a number");
                                        //    }
                                        // }
                                        // else
                                        // {
                                        if (dimensionW is string)
                                        {
                                            if (!item.ErrorData.ContainsKey("Product Dimension W (CM)"))
                                            {
                                                item.ErrorData.Add("Product Dimension W (CM)", new List<string>());
                                            }

                                            item.ErrorData["Product Dimension W (CM)"].Add($"Special characters are present in Product Dimension W (CM) '{dimensionW}'");
                                        }

                                        // }
                                    }
                                }

                                if (item.ColumnData.ContainsKey("Product Dimension H (CM)"))
                                {
                                    var dimensionH = item.ColumnData["Product Dimension H (CM)"];
                                    if (string.IsNullOrWhiteSpace(dimensionH.ToString()))
                                    {
                                        if (!item.ErrorData.ContainsKey("Product Dimension H (CM)"))
                                        {
                                            item.ErrorData.Add("Product Dimension H (CM)", new List<string>());
                                        }

                                        item.ErrorData["Product Dimension H (CM)"].Add("Product Dimension H (CM) Field is required");
                                    }
                                    else
                                    {
                                        // if (dimensionH != null && decimal.TryParse(dimensionH.ToString(), out decimal dimensionHValue))
                                        // {
                                        //    if (dimensionHValue <= 0 || dimensionHValue % 1 != 0)
                                        //    {
                                        //        if (!item.ErrorData.ContainsKey("Product Dimension H (CM)"))
                                        //        {
                                        //            item.ErrorData.Add("Product Dimension H (CM)", new List<string>());
                                        //        }
                                        //        item.ErrorData["Product Dimension H (CM)"].Add("Product Dimension H (CM) Field is expected to be a number");
                                        //    }
                                        // }
                                        // else
                                        // {
                                        if (dimensionH is string)
                                        {
                                            if (!item.ErrorData.ContainsKey("Product Dimension H (CM)"))
                                            {
                                                item.ErrorData.Add("Product Dimension H (CM)", new List<string>());
                                            }

                                            item.ErrorData["Product Dimension H (CM)"].Add($"Special characters are present in Product Dimension H (CM) '{dimensionH}'");
                                        }

                                        // }
                                    }
                                }

                                if (item.ColumnData.ContainsKey("Product Net Weight (GMS)"))
                                {
                                    var netWeightGMS = item.ColumnData["Product Net Weight (GMS)"];
                                    if (string.IsNullOrWhiteSpace(netWeightGMS.ToString()))
                                    {
                                        if (!item.ErrorData.ContainsKey("Product Net Weight (GMS)"))
                                        {
                                            item.ErrorData.Add("Product Net Weight (GMS)", new List<string>());
                                        }

                                        item.ErrorData["Product Net Weight (GMS)"].Add("Product Net Weight (GMS) Field is required");
                                    }
                                    else
                                    {
                                        // if (netWeightGMS != null && decimal.TryParse(netWeightGMS.ToString(), out decimal netgmsValue))
                                        // {
                                        //    if (netgmsValue <= 0 || netgmsValue % 1 != 0)
                                        //    {
                                        //        if (!item.ErrorData.ContainsKey("Product Net Weight (GMS)"))
                                        //        {
                                        //            item.ErrorData.Add("Product Net Weight (GMS)", new List<string>());
                                        //        }
                                        //        item.ErrorData["Product Net Weight (GMS)"].Add("Product Net Weight (GMS) Field is expected to be a number");
                                        //    }
                                        // }
                                        // else
                                        // {
                                        if (netWeightGMS is string)
                                        {
                                            if (!item.ErrorData.ContainsKey("Product Net Weight (GMS)"))
                                            {
                                                item.ErrorData.Add("Product Net Weight (GMS)", new List<string>());
                                            }

                                            item.ErrorData["Product Net Weight (GMS)"].Add($"Special characters are present in Product Net Weight (GMS) '{netWeightGMS}'");
                                        }

                                        // }
                                    }
                                }
                            });

                            // Qc Template Optional field datatype validation
                            if (!template.IsUidTemplate)
                            {
                                Parallel.ForEach(data, item =>
                                {
                                    if (item.ColumnData.ContainsKey("Product Capacity Volume (ML)"))
                                    {
                                        var productCapacity = item.ColumnData["Product Capacity Volume (ML)"];
                                        if (!string.IsNullOrWhiteSpace(productCapacity.ToString()))
                                        {
                                            // if (productCapacity != null && decimal.TryParse(productCapacity.ToString(), out decimal capacityValue))
                                            // {
                                            //    if (capacityValue <= 0 || capacityValue % 1 != 0)
                                            //    {
                                            //        if (!item.ErrorData.ContainsKey("Product Capacity Volume (ML)"))
                                            //        {
                                            //            item.ErrorData.Add("Product Capacity Volume (ML)", new List<string>());
                                            //        }
                                            //         item.ErrorData["Product Capacity Volume (ML)"].Add("Product Capacity Volume (ML) Field is expected to be a number");
                                            //    }
                                            // }
                                            // else
                                            // {
                                            if (productCapacity is string)
                                            {
                                                if (!item.ErrorData.ContainsKey("Product Capacity Volume (ML)"))
                                                {
                                                    item.ErrorData.Add("Product Capacity Volume (ML)", new List<string>());
                                                }

                                                item.ErrorData["Product Capacity Volume (ML)"].Add($"Special characters are present in Product Capacity Volume (ML) '{productCapacity}'");
                                            }

                                            // }
                                        }
                                    }
                                });
                            }

                            // QC Template
                            if (!template.IsUidTemplate)
                            {
                                Parallel.ForEach(data, item =>
                                {
                                    if (templateDivisionId == "61")
                                    {
                                        if (item.ColumnData.ContainsKey("Care Instruction 1 Cleaning"))
                                        {
                                            var care1 = item.ColumnData["Care Instruction 1 Cleaning"].ToString();
                                            if (string.IsNullOrWhiteSpace(care1))
                                            {
                                                if (!item.ErrorData.ContainsKey("Care Instruction 1 Cleaning"))
                                                {
                                                    item.ErrorData.Add("Care Instruction 1 Cleaning", new List<string>());
                                                }

                                                item.ErrorData["Care Instruction 1 Cleaning"].Add("Care Instruction 1 Cleaning Field is required");
                                            }
                                        }

                                        if (item.ColumnData.ContainsKey("Care Instruction 2 Caring Dos"))
                                        {
                                            var care2 = item.ColumnData["Care Instruction 2 Caring Dos"].ToString();
                                            if (string.IsNullOrWhiteSpace(care2))
                                            {
                                                if (!item.ErrorData.ContainsKey("Care Instruction 2 Caring Dos"))
                                                {
                                                    item.ErrorData.Add("Care Instruction 2 Caring Dos", new List<string>());
                                                }

                                                item.ErrorData["Care Instruction 2 Caring Dos"].Add("Care Instruction 2 Caring Dos Field is required");
                                            }
                                        }

                                        if (item.ColumnData.ContainsKey("Care Instruction 3 Caring Don't"))
                                        {
                                            var care3 = item.ColumnData["Care Instruction 3 Caring Don't"].ToString();
                                            if (string.IsNullOrWhiteSpace(care3))
                                            {
                                                if (!item.ErrorData.ContainsKey("Care Instruction 3 Caring Don't"))
                                                {
                                                    item.ErrorData.Add("Care Instruction 3 Caring Don't", new List<string>());
                                                }

                                                item.ErrorData["Care Instruction 3 Caring Don't"].Add("Care Instruction 3 Caring Don't Field is required");
                                            }
                                        }

                                        if (item.ColumnData.ContainsKey("Care Instruction 4 Storage Instructions"))
                                        {
                                            var care4 = item.ColumnData["Care Instruction 4 Storage Instructions"].ToString();
                                            if (string.IsNullOrWhiteSpace(care4))
                                            {
                                                if (!item.ErrorData.ContainsKey("Care Instruction 4 Storage Instructions"))
                                                {
                                                    item.ErrorData.Add("Care Instruction 4 Storage Instructions", new List<string>());
                                                }

                                                item.ErrorData["Care Instruction 4 Storage Instructions"].Add("Care Instruction 4 Storage Instructions Field is required");
                                            }
                                        }
                                    }
                                });
                            }

                            // Packaging Designer Template
                            if (!template.IsUidTemplate)
                            {
                                Parallel.ForEach(data, item =>
                                {
                                    if (templateDivisionId == "61")
                                    {
                                        if (item.ColumnData.ContainsKey("Packaging Image"))
                                        {
                                            var packagingImage = item.ColumnData["Packaging Image"].ToString();
                                            if (string.IsNullOrWhiteSpace(packagingImage))
                                            {
                                                if (!item.ErrorData.ContainsKey("Packaging Image"))
                                                {
                                                    item.ErrorData.Add("Packaging Image", new List<string>());
                                                }

                                                item.ErrorData["Packaging Image"].Add("Packaging Image Field is required");
                                            }
                                        }

                                        if (item.ColumnData.ContainsKey("PD Packaging Type"))
                                        {
                                            var pdPackaging = item.ColumnData["PD Packaging Type"].ToString();
                                            if (string.IsNullOrWhiteSpace(pdPackaging))
                                            {
                                                if (!item.ErrorData.ContainsKey("PD Packaging Type"))
                                                {
                                                    item.ErrorData.Add("PD Packaging Type", new List<string>());
                                                }

                                                item.ErrorData["PD Packaging Type"].Add("PD Packaging Type Field is required");
                                            }
                                        }

                                        if (item.ColumnData.ContainsKey("PD Packaging Size L (CM)"))
                                        {
                                            var pdSizeL = item.ColumnData["PD Packaging Size L (CM)"];
                                            if (string.IsNullOrWhiteSpace(pdSizeL.ToString()))
                                            {
                                                if (!item.ErrorData.ContainsKey("PD Packaging Size L (CM)"))
                                                {
                                                    item.ErrorData.Add("PD Packaging Size L (CM)", new List<string>());
                                                }

                                                item.ErrorData["PD Packaging Size L (CM)"].Add("PD Packaging Size L (CM) Field is required");
                                            }
                                            else
                                            {
                                                // if (pdSizeL != null && decimal.TryParse(pdSizeL.ToString(), out decimal pdLValue))
                                                // {
                                                //    if (pdLValue <= 0 || pdLValue % 1 != 0)
                                                //    {
                                                //        if (!item.ErrorData.ContainsKey("PD Packaging Size L (CM)"))
                                                //        {
                                                //            item.ErrorData.Add("PD Packaging Size L (CM)", new List<string>());
                                                //        }
                                                //        item.ErrorData["PD Packaging Size L (CM)"].Add("PD Packaging Size L (CM) is expected to be a number");
                                                //    }
                                                // }
                                                // else
                                                // {
                                                if (pdSizeL is string)
                                                {
                                                    if (!item.ErrorData.ContainsKey("PD Packaging Size L (CM)"))
                                                    {
                                                        item.ErrorData.Add("PD Packaging Size L (CM)", new List<string>());
                                                    }

                                                    item.ErrorData["PD Packaging Size L (CM)"].Add($"Special characters are present in PD Packaging Size L (CM) '{pdSizeL}'");
                                                }

                                                // }
                                            }
                                        }

                                        if (item.ColumnData.ContainsKey("PD Packaging Size W (CM)"))
                                        {
                                            var pdSizeW = item.ColumnData["PD Packaging Size W (CM)"];
                                            if (string.IsNullOrWhiteSpace(pdSizeW.ToString()))
                                            {
                                                if (!item.ErrorData.ContainsKey("PD Packaging Size L (CM)"))
                                                {
                                                    item.ErrorData.Add("PD Packaging Size L (CM)", new List<string>());
                                                }

                                                item.ErrorData["PD Packaging Size L (CM)"].Add("PD Packaging Size W (CM) Field is required");
                                            }

                                            // else
                                            // {
                                            //    if (pdSizeW != null && decimal.TryParse(pdSizeW.ToString(), out decimal pdWValue))
                                            //    {
                                            //        if (pdWValue <= 0 || pdWValue % 1 != 0)
                                            //        {
                                            //            if (!item.ErrorData.ContainsKey("PD Packaging Size W (CM)"))
                                            //            {
                                            //                item.ErrorData.Add("PD Packaging Size W (CM)", new List<string>());
                                            //            }
                                            //            item.ErrorData["PD Packaging Size W (CM)"].Add("PD Packaging Size W (CM) is expected to be a number");
                                            //        }
                                            //    }
                                            else
                                            {
                                                if (pdSizeW is string)
                                                {
                                                    if (!item.ErrorData.ContainsKey("PD Packaging Size W (CM)"))
                                                    {
                                                        item.ErrorData.Add("PD Packaging Size W (CM)", new List<string>());
                                                    }

                                                    item.ErrorData["PD Packaging Size W (CM)"].Add($"Special characters are present in PD Packaging Size W (CM) '{pdSizeW}'");
                                                }
                                            }

                                            // }
                                        }

                                        if (item.ColumnData.ContainsKey("PD Packaging Size H (CM)"))
                                        {
                                            var pdSizeH = item.ColumnData["PD Packaging Size H (CM)"];
                                            if (string.IsNullOrWhiteSpace(pdSizeH.ToString()))
                                            {
                                                if (!item.ErrorData.ContainsKey("PD Packaging Size L (CM)"))
                                                {
                                                    item.ErrorData.Add("PD Packaging Size L (CM)", new List<string>());
                                                }

                                                item.ErrorData["PD Packaging Size L (CM)"].Add("PD Packaging Size H (CM) Field is required");
                                            }

                                            // else if (pdSizeH != null && decimal.TryParse(pdSizeH.ToString(), out decimal pdHValue))
                                            // {
                                            //    if (pdHValue <= 0 || pdHValue % 1 != 0)
                                            //    {
                                            //        if (!item.ErrorData.ContainsKey("PD Packaging Size H (CM)"))
                                            //        {
                                            //            item.ErrorData.Add("PD Packaging Size H (CM)", new List<string>());
                                            //        }
                                            //        item.ErrorData["PD Packaging Size H (CM)"].Add("PD Packaging Size H (CM) is expected to be a number");
                                            //    }
                                            // }
                                            else
                                            {
                                                if (pdSizeH is string)
                                                {
                                                    if (!item.ErrorData.ContainsKey("PD Packaging Size H (CM)"))
                                                    {
                                                        item.ErrorData.Add("PD Packaging Size H (CM)", new List<string>());
                                                    }

                                                    item.ErrorData["PD Packaging Size H (CM)"].Add($"Special characters are present in PD Packaging Size H (CM) '{pdSizeH}'");
                                                }
                                            }
                                        }
                                    }
                                    else if (templateDivisionId == "4" || templateDivisionId == "65")
                                    {
                                        if (item.ColumnData.ContainsKey("PD Packaging Size L (CM)"))
                                        {
                                            var pdSizeL = item.ColumnData["PD Packaging Size L (CM)"];
                                            if (!string.IsNullOrWhiteSpace(pdSizeL.ToString()))
                                            {
                                                if (pdSizeL != null && decimal.TryParse(pdSizeL.ToString(), out decimal pdLValue))
                                                {
                                                    if (pdLValue <= 0 || pdLValue % 1 != 0)
                                                    {
                                                        if (!item.ErrorData.ContainsKey("PD Packaging Size L (CM)"))
                                                        {
                                                            item.ErrorData.Add("PD Packaging Size L (CM)", new List<string>());
                                                        }

                                                        item.ErrorData["PD Packaging Size L (CM)"].Add("PD Packaging Size L (CM) is expected to be a number");
                                                    }
                                                }
                                                else
                                                {
                                                    if (pdSizeL is string)
                                                    {
                                                        if (!item.ErrorData.ContainsKey("PD Packaging Size L (CM)"))
                                                        {
                                                            item.ErrorData.Add("PD Packaging Size L (CM)", new List<string>());
                                                        }

                                                        item.ErrorData["PD Packaging Size L (CM)"].Add($"Special characters are present in PD Packaging Size L (CM) '{pdSizeL}'");
                                                    }
                                                }
                                            }
                                        }

                                        if (item.ColumnData.ContainsKey("PD Packaging Size W (CM)"))
                                        {
                                            var pdSizeW = item.ColumnData["PD Packaging Size W (CM)"];
                                            if (!string.IsNullOrWhiteSpace(pdSizeW.ToString()))
                                            {
                                                if (pdSizeW != null && decimal.TryParse(pdSizeW.ToString(), out decimal pdWValue))
                                                {
                                                    if (pdWValue <= 0 || pdWValue % 1 != 0)
                                                    {
                                                        if (!item.ErrorData.ContainsKey("PD Packaging Size W (CM)"))
                                                        {
                                                            item.ErrorData.Add("PD Packaging Size W (CM)", new List<string>());
                                                        }

                                                        item.ErrorData["PD Packaging Size W (CM)"].Add("PD Packaging Size W (CM) is expected to be a number");
                                                    }
                                                }
                                                else
                                                {
                                                    if (pdSizeW is string)
                                                    {
                                                        if (!item.ErrorData.ContainsKey("PD Packaging Size W (CM)"))
                                                        {
                                                            item.ErrorData.Add("PD Packaging Size W (CM)", new List<string>());
                                                        }

                                                        item.ErrorData["PD Packaging Size W (CM)"].Add($"Special characters are present in PD Packaging Size W (CM) '{pdSizeW}'");
                                                    }
                                                }
                                            }
                                        }

                                        if (item.ColumnData.ContainsKey("PD Packaging Size H (CM)"))
                                        {
                                            var pdSizeH = item.ColumnData["PD Packaging Size H (CM)"];
                                            if (!string.IsNullOrWhiteSpace(pdSizeH.ToString()))
                                            {
                                                if (pdSizeH != null && decimal.TryParse(pdSizeH.ToString(), out decimal pdHValue))
                                                {
                                                    if (pdHValue <= 0 || pdHValue % 1 != 0)
                                                    {
                                                        if (!item.ErrorData.ContainsKey("PD Packaging Size H (CM)"))
                                                        {
                                                            item.ErrorData.Add("PD Packaging Size H (CM)", new List<string>());
                                                        }

                                                        item.ErrorData["PD Packaging Size H (CM)"].Add("PD Packaging Size H (CM) is expected to be a number");
                                                    }
                                                }
                                                else
                                                {
                                                    if (pdSizeH is string)
                                                    {
                                                        if (!item.ErrorData.ContainsKey("PD Packaging Size H (CM)"))
                                                        {
                                                            item.ErrorData.Add("PD Packaging Size H (CM)", new List<string>());
                                                        }

                                                        item.ErrorData["PD Packaging Size H (CM)"].Add($"Special characters are present in PD Packaging Size H (CM) '{pdSizeH}'");
                                                    }
                                                }
                                            }
                                        }
                                    }
                                });

                                // FAB Template
                                if (!template.IsUidTemplate)
                                {
                                    Parallel.ForEach(data, item =>
                                    {
                                        if (item.ColumnData.ContainsKey("FAB Based on Finish Process"))
                                        {
                                            var fabFinish = item.ColumnData["FAB Based on Finish Process"].ToString();
                                            if (string.IsNullOrWhiteSpace(fabFinish))
                                            {
                                                if (!item.ErrorData.ContainsKey("FAB Based on Finish Process"))
                                                {
                                                    item.ErrorData.Add("FAB Based on Finish Process", new List<string>());
                                                }

                                                item.ErrorData["FAB Based on Finish Process"].Add("FAB Based on Finish Process Field is required");
                                            }
                                        }

                                        if (item.ColumnData.ContainsKey("FAB Based on Material"))
                                        {
                                            var fabMaterial = item.ColumnData["FAB Based on Material"].ToString();
                                            if (string.IsNullOrWhiteSpace(fabMaterial))
                                            {
                                                if (!item.ErrorData.ContainsKey("FAB Based on Material"))
                                                {
                                                    item.ErrorData.Add("FAB Based on Material", new List<string>());
                                                }

                                                item.ErrorData["FAB Based on Material"].Add("FAB Based on Material Field is required");
                                            }
                                        }

                                        if (item.ColumnData.ContainsKey("Utility Functional Usage USP"))
                                        {
                                            var utility = item.ColumnData["Utility Functional Usage USP"].ToString();
                                            if (string.IsNullOrWhiteSpace(utility))
                                            {
                                                if (!item.ErrorData.ContainsKey("Utility Functional Usage USP"))
                                                {
                                                    item.ErrorData.Add("Utility Functional Usage USP", new List<string>());
                                                }

                                                item.ErrorData["Utility Functional Usage USP"].Add("Utility Functional Usage USP Field is required");
                                            }
                                        }
                                    });
                                }
                            }
                        }
                    }
                }
            }

            return data;
        }
        catch (Exception e)
        {
            throw new Exception($"Error on UploadingFile : {e.Message}");
        }
    }

    public async Task SaveData(List<UploadedFileData> data, long templateId)
    {
        try
        {
            var validateData = await this.ValidateData(data, templateId);
            if (data.Count == 0)
            {
                return;
            }

            using var connection = this.GetConnection();
            using var transaction = connection.BeginTransaction();
            transaction.BulkMerge("uploadedfiledata", validateData.ToArray().GetDataTableFromObjects());
        }
        catch (Exception e)
        {
            throw new Exception($"Error on UploadingFile : {e.Message}");
        }
    }

    private async Task ValidateAttr(UploadedFileData product, ProductAttribute attr)
    {
        var errorData = new List<string>();
        var attrValue = product.ColumnData[attr.Name];
        try
        {
            if (attr.IsRequired)
            {
                if (attrValue == null || attrValue == string.Empty)
                {
                    errorData.Add($"{attr.Name} field is required");
                    isCombination = true;
                }
                else if (attr.DataType == AttrDataType.Number)
                {
                    if (attrValue?.ToString() == "0" || string.IsNullOrEmpty(attrValue?.ToString()))
                    {
                        errorData.Add($"{attr.Name} field is required");
                        isCombination = true;
                    }
                    else if (attrValue != null)
                    {
                        if (!attrValue.ToString().IsNumeric())
                        {
                            errorData.Add($"{attr.Name} field is expected to be a number");
                        }
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(attrValue?.ToString()))
                    {
                        errorData.Add($"{attr.Name} field is required");
                        isCombination = true;
                    }
                }
            }

            if (attr.DataType == AttrDataType.Lookup && !string.IsNullOrWhiteSpace(attrValue?.ToString()))
            {
                using var connection = this.GetConnection();
                if (attr.Name != "Season Code" && attr.Name != "Packaging Type")
                {
                    var query = "SELECT * FROM " + attr.LookupTable + " WHERE CASE WHEN " + attr.LookupTableColumn + "::text ~'^[0-9]+$' THEN " + attr.LookupTableColumn + "::text = '" + attrValue + "' ELSE lower(" + attr.LookupTableColumn + "::text) = lower('" + attrValue + "') END";
                    var cnd = connection.QueryAsync(query).Result;
                    if (cnd.ToList().Count == 0)
                    {
                        if (!product.ErrorData.ContainsKey(attr.Name))
                        {
                            errorData.Add(
                            $"Invalid {attr.Name} '{attrValue}'");
                        }
                    }
                }
            }

            if (attr.DataType == AttrDataType.Number)
            {
                if (attr.Name == null && attr.Name.ToString() != string.Empty)
                {
                    if (!attrValue.ToString().IsNumeric())
                    {
                        errorData.Add($"{attr.Name} field is expected to be a number");
                    }
                }
            }

            if (attr.DataType == AttrDataType.DateTime)
            {
                var formats = new string[] { "dd-MM-yyyy", "dd/MM/yyyy" };
                var attrValueStr = attrValue?.ToString();

                if (!DateTime.TryParseExact(attrValueStr, formats, CultureInfo.CurrentCulture, DateTimeStyles.None, out var date) &&
                    !DateTime.TryParseExact(attrValueStr, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    if (DateTime.TryParse(attrValueStr, out date))
                    {
                        var parsedDate = date.ToString("dd/MM/yyyy");
                        product.ColumnData[attr.Name] = parsedDate;
                    }
                    else
                    {
                        errorData.Add($"Invalid Date Format'{attrValueStr}'");
                    }
                }
                else
                {
                    var parsedDate = date.ToString("dd/MM/yyyy");
                    product.ColumnData[attr.Name] = parsedDate;
                }
            }

            if (attr.DataType == AttrDataType.Boolean)
            {
                if (attrValue != string.Empty && attrValue != null)
                {
                    if (!attrValue.ToString().Equals("Yes") && !attrValue.ToString().Equals("No"))
                    {
                        errorData.Add(
                       $" Invalid {attr.Name} '{attrValue}' ");
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            errorData.Add($"{attr.Name}>{attrValue} has errored {e.Message}");
        }

        if (errorData.Count > 0)
        {
            product.ErrorData ??= new Dictionary<string, List<string>>();
            product.ErrorData[attr.Name] = errorData;
        }
    }
}