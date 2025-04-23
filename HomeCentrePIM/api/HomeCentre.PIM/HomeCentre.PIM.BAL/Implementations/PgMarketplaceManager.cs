using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using ClosedXML.Excel;
using Dapper;
using DocumentFormat.OpenXml.InkML;
using HashidsNet;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.DomainModel;
using HomeCentre.PIM.DAL.Staging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.HSSF.Record;
using OfficeOpenXml;
using Org.BouncyCastle.Asn1.Cms;

namespace HomeCentre.PIM.BAL.Implementations;

public class PgMarketplaceManager : PgEntityAction<Marketplace>, IMarketplaceManager
{
    private readonly IBlobRepo _blobRepo;
    private readonly IDbCache _cache;

    public PgMarketplaceManager(IUserContext userContext, IDbConnectionFactory connectionFactory,
        IConfiguration configuration, IBlobRepo blobRepo, IDbCache cache)
        : base(userContext, connectionFactory, configuration)
    {
        _blobRepo = blobRepo;
        _cache = cache;
    }

    public async Task<Marketplace> AddOrUpdate(Marketplace marketplace)
    {
        using var connection = this.GetConnection();
        var existing = await connection.QueryAsync<Marketplace>("select * from marketplace where LOWER(TRIM(Name)) = LOWER(@Name)", new { Name = marketplace.Name.Trim() });
        if (existing.Any(x => x.Id != marketplace.Id))
        {
            throw new HandledException($"MarketPlace '{marketplace.Name}' already exists");
        }

        marketplace.Name = marketplace.Name.MakeFirstLetterCapital();

        return await base.AddOrUpdate(marketplace);
    }

    public async Task<MarketplaceAttribute> AddOrUpdateAttribute(MarketplaceAttribute marketplaceAttribute)
    {
        using var connection = this.GetConnection();
        var action = this.GetAction<MarketplaceAttribute>();
        var existing = await connection.QueryAsync<MarketplaceAttribute>("select * from marketplaceattribute");
        if (existing.Any(x => x.Name.ToLower() == marketplaceAttribute.Name.ToLower() && x.MarketplaceId == marketplaceAttribute.MarketplaceId && x.Id != marketplaceAttribute.Id))
        {
            throw new HandledException($"Attribute '{marketplaceAttribute.Name}' already exists");
        }

        return await action.AddOrUpdate(marketplaceAttribute);
    }

    public async Task<MarketplaceTemplate> AddOrUpdateTemplate(MarketplaceTemplate marketplaceTemplate)
    {
        if (marketplaceTemplate.Id == 0)
        {
            var hashids = new Hashids(Guid.NewGuid().ToString());
            using var connection = this.GetConnection();
            var marketplace = await this.Get(marketplaceTemplate.MarketplaceId);
            var user = await _cache.GetUser(await this.GetUserId());
            var sequence = hashids.Encode(await connection.QueryFirstAsync<int>("select LPAD(nextval('market_template_sequence')::text, 4, '0')"));
            var trimEmail = user.Email.Substring(0, user.Email.IndexOf("@"));
            marketplaceTemplate.Name = $"{marketplace.ShortCode}_{marketplaceTemplate.Name}";
            var existingTemplate = await connection.QueryFirstOrDefaultAsync<MarketplaceTemplate>("SELECT * FROM marketplacetemplate WHERE LOWER(Name) = LOWER(@Name)", new { Name = marketplaceTemplate.Name });
            if (existingTemplate != null)
            {
                throw new Exception($"Template '{marketplaceTemplate.Name}' already exists");
            }

            var action = this.GetAction<MarketplaceTemplate>();
            await action.AddOrUpdate(marketplaceTemplate);

            var templateId = marketplaceTemplate.Id;
            var marketplaceId = marketplaceTemplate.MarketplaceId;
            var productDepartmentId = marketplaceTemplate.ProductDepartmentId;
            var productAttributeIds = await connection.QueryAsync<long>($"SELECT Id FROM productattribute WHERE divisionid = {marketplaceTemplate.DivisionId}");
            foreach (var productAttributeId in productAttributeIds)
            {
                await connection.ExecuteAsync("INSERT INTO marketplacetemplateattribute (marketplacetemplateid, productattributeid, marketplaceid, productdepartmentid,createdby,updatedby) VALUES (@Id, @ProductAttributeId, @MarketplaceId, @ProductDepartmentId,1,1)", new { Id = templateId, ProductAttributeId = productAttributeId, MarketplaceId = marketplaceId, ProductDepartmentId = productDepartmentId });
            }
        }

        return marketplaceTemplate;
    }

    public async Task<MarketplaceTemplateAttribute> AddOrUpdateTemplateAttribute(
        MarketplaceTemplateAttribute marketplaceTemplateAttribute)
    {
        var action = this.GetAction<MarketplaceTemplateAttribute>();
        return await action.AddOrUpdate(marketplaceTemplateAttribute);
    }

    public async Task<SearchResult<Marketplace>> SearchMarketplaces(string searchKey, int pageNumber, int pageSize,
        bool isActive)
    {
        return await this.Search(searchKey, pageNumber, pageSize, isActive: isActive);
    }

    public async Task<SearchResult<MarketplaceTemplate>> SearchMarketplaceTemplatescam(string searchKey, int pageNumber,
        int pageSize, long marketPlaceId, long? divisionId,
        long? productDepartmentId, bool isActive)
    {
        var dynamicParams = new DynamicParameters();
        dynamicParams.Add("marketPlaceId", marketPlaceId);
        var action = this.GetAction<MarketplaceTemplate>();
        string query = "marketplaceId = @marketPlaceId";
        if (divisionId != 0)
        {
            query += " AND divisionId = @divisionId";
            dynamicParams.Add("divisionId", divisionId);
        }

        ////if (divisionId != -1)
        ////{
        ////    query += " AND divisionId = @divisionId";
        ////    dynamicParams.Add("divisionId", divisionId);
        ////}

        if (productDepartmentId != 0)
        {
            query += " AND (productDepartmentId IS NULL OR productDepartmentId =@departmentId)";
            dynamicParams.Add("departmentId", productDepartmentId);
        }

        return await action.Search(searchKey, pageNumber, pageSize, query, dynamicParams, isActive: isActive);
    }

    public async Task<SearchResult<T>> SearchMarketplaceTemplates<T>(string searchKey, int pageNumber,
    int pageSize, long marketPlaceId, long[]? divisionId,
    long[]? productDepartmentId, bool isActive)
    {
        using var connection = this.GetConnection();

        var parameters = new
        {
            marketPlaceId,
            divisionId,
            productDepartmentId
        };

        string query = "SELECT * FROM MarketplaceTemplate WHERE marketplaceId = @marketPlaceId";

        if (divisionId != null || divisionId.Length > 0)
        {
            query += " AND divisionId = any(array[@divisionId]::integer[])";
        }

        if (productDepartmentId != null && productDepartmentId.Length > 0)
        {
            query += " AND (productDepartmentId IS NULL OR productDepartmentId = any(array[@productDepartmentId]::bigint[])) AND isactive = true ORDER BY DateCreated ASC NULLS LAST";
        }
        else
        {
            query += " AND isactive = true ORDER BY DateCreated ASC NULLS LAST";
        }

        return new SearchResult<T>
        {
            Records = (await connection.QueryAsync<T>(query, parameters))
                   .ToList()
        };
    }

    public async Task<SearchResult<MarketplaceAttribute>> SearchMarketplaceAttributes(string searchKey, int pageNumber,
        int pageSize, long marketPlaceId, long divisionId)
    {
        var dynamicParams = new DynamicParameters();
        dynamicParams.Add("marketPlaceId", marketPlaceId);
        dynamicParams.Add("divisionId", divisionId);
        var action = this.GetAction<MarketplaceAttribute>();
        return await action.Search(searchKey, pageNumber, pageSize,
            " marketplaceId = @marketPlaceId and divisionId = @divisionId", dynamicParams);
    }

    public async Task<SearchResult<MarketplaceTemplateAttribute>> SearchMarketplaceTemplateAttributes(string? searchKey,
        int pageNumber, int pageSize, long marketPlaceTemplateId)
    {
        var dynamicParams = new DynamicParameters();
        dynamicParams.Add("marketPlaceTemplateId", marketPlaceTemplateId);
        var action = this.GetAction<MarketplaceTemplateAttribute>();
        return await action.Search(searchKey, pageNumber, pageSize, " marketPlaceTemplateId = @marketPlaceTemplateId",
            dynamicParams);
    }

    public async Task<List<MarketplaceTemplateAttribute>> GetMarketplaceTemplateAttributes(string searchKey, int pageNumber, int pageSize, long marketPlaceTemplateId)
    {
        using var connection = this.GetConnection();
        var query = "SELECT marketplacetemplateattribute.* FROM marketplacetemplateattribute join productattribute on productattribute.id= productattributeid WHERE marketplacetemplateid = @MarketPlaceTemplateId ORDER BY productattribute.name";
        var parameters = new { MarketPlaceTemplateId = marketPlaceTemplateId };
        var result = await connection.QueryAsync<MarketplaceTemplateAttribute>(query, parameters);
        return result.ToList();
    }

    public async Task<List<MarketplacetemplateDiv>> GetMarketplaceTemplateAttributesDiv(long marketPlaceTemplateId)
    {
        using var connection = this.GetConnection();
        var query = "SELECT * FROM marketplacetemplate where id= @MarketPlaceTemplateId";
        var parameters = new { MarketPlaceTemplateId = marketPlaceTemplateId };
        var result = await connection.QueryAsync<MarketplacetemplateDiv>(query, parameters);
        return result.ToList();
    }

    public async Task RemoveMarketplaces(long[] ids)
    {
        await this.Remove(ids);
    }

    public async Task RemoveMarketplaceAttributes(long[] ids)
    {
        var action = this.GetAction<MarketplaceAttribute>();
        await action.Remove(ids);
    }

    public async Task RemoveMarketplaceTemplates(long[] ids)
    {
        var action = this.GetAction<MarketplaceTemplate>();
        await action.Remove(ids);
    }

    public async Task RemoveMarketplaceTemplateAttributes(long[] ids, string? templateid)
    {
        using var connection = this.GetConnection();
        var query = $"UPDATE MarketplaceTemplateAttribute SET MarketplaceAttribute = NULL, ColumnOrder = NULL WHERE Id IN ({string.Join(",", ids)})";
        var parameters = await connection.QueryAsync(query);
        var dept = $"select productdepartmentid,productattributeid from MarketplaceTemplateAttribute WHERE MarketplaceTemplateId= {templateid} limit 1";
        var productId = await connection.QueryFirstOrDefaultAsync<MarketplaceTemplateAttribute>(dept);
        var division = "SELECT divisionid FROM productattribute WHERE id = @Id limit 1";
        var parameter = new
        {
            Id = productId.ProductAttributeId
        };
        var productDivision = await connection.QueryFirstOrDefaultAsync<int>(division, parameter);
        var marketplaceAttributeData = "SELECT DISTINCT id, name " +
                                              "FROM productattribute " +
                                              "WHERE id IN (SELECT DISTINCT productattributeid FROM marketplacetemplateattribute " +
                                             $"WHERE  datatype IS NOT NULL AND marketplaceattribute IS NOT NULL and  marketplacetemplateid = {templateid}) " +
                                              "ORDER BY id ASC";

        var marketplaceAttributeNames = new List<string>();
        var result = await connection.QueryAsync<ProductAttribute>(marketplaceAttributeData, new { MarketplaceTemplateId = templateid });
        marketplaceAttributeNames.AddRange(result.Select(attr => attr.Name));
        var query1 = $"SELECT p.uid, p.columndata " +
                            $"FROM product p " +
                            $"JOIN itemcodestaging ic ON p.uid = ic.uidcode where p.columndata->>'Division Code'= '{productDivision}' and p.columndata->>'Dept Code'= '{productId.ProductDepartmentId}'";
        var products = await connection.QueryAsync<Product>(query1);

        foreach (var product in products)
        {
            var uid = product.Uid;
            if (result.Count() == 0)
            {
                await connection.ExecuteAsync($"UPDATE product SET statusfilter = '0' WHERE uid = '{uid}' and columndata->>'Division Code'= '{productDivision}'   and columndata->>'Dept Code'= '{productId.ProductDepartmentId}'");
            }
            else
            {
                var columnData = product.ColumnData;
                var keysToSearch = columnData.Keys.ToList();
                bool allKeysFound = marketplaceAttributeNames.All(attrName => keysToSearch.Contains(attrName));
                long statusFilter = allKeysFound ? 1 : 0;
                await connection.ExecuteAsync($"UPDATE product SET statusfilter = '{statusFilter}' WHERE uid = '{uid}' and columndata->>'Division Code'= '{productDivision}'   and columndata->>'Dept Code'= '{productId.ProductDepartmentId}'");
            }
        }
    }

    public async Task ActivateMarketplaces(long[] ids)
    {
        await this.Activate(ids);
    }

    public async Task ActivateMarketplaceAttributes(long[] ids)
    {
        var action = this.GetAction<MarketplaceAttribute>();
        await action.Activate(ids);
    }

    public async Task ActivateMarketplaceTemplates(long[] ids)
    {
        var action = this.GetAction<MarketplaceTemplate>();
        await action.Activate(ids);
    }

    public async Task ActivateMarketplaceTemplateAttributes(long[] ids)
    {
        var action = this.GetAction<MarketplaceTemplateAttribute>();
        await action.Activate(ids);
    }

    public async Task DeactivateMarketplaces(long[] ids)
    {
        await this.Deactivate(ids);
    }

    public async Task DeactivateMarketplaceAttributes(long[] ids)
    {
        var action = this.GetAction<MarketplaceAttribute>();
        await action.Deactivate(ids);
    }

    public async Task DeactivateMarketplaceTemplates(long[] ids)
    {
        var action = this.GetAction<MarketplaceTemplate>();
        await action.Deactivate(ids);
    }

    public async Task DeactivateMarketplaceTemplateAttributes(long[] ids)
    {
        var action = this.GetAction<MarketplaceTemplateAttribute>();
        await action.Deactivate(ids);
    }

    public async Task ProcessUploadedMarketplaceAttributes(long marketplaceId, long divisionId, string blobUrl)
    {
        var tempFile = await _blobRepo.DownloadFile(blobUrl);
        using var wb = new XLWorkbook(tempFile, XLEventTracking.Disabled);
        var ws = wb.Worksheet(1);
        var dataTable = ws.Tables.Any()
            ? ws.Tables.First().AsNativeDataTable()
            : ws.RangeUsed().AsTable().AsNativeDataTable();
        if (dataTable.Columns.Contains("MarketplaceId"))
        {
            dataTable.Columns.Remove("MarketplaceId");
        }

        var userId = await this.GetUserId();
        dataTable.Columns.Add("MarketplaceId", typeof(long));
        dataTable.Columns.Add("CreatedBy", typeof(long));
        dataTable.Columns.Add("UpdatedBy", typeof(long));
        dataTable.Columns.Add("DivisionId", typeof(long));

        using var connection = this.GetConnection();
        using var transaction = connection.BeginTransaction();

        foreach (DataRow dataRow in dataTable.Rows)
        {
            dataRow["MarketplaceId"] = marketplaceId;
            dataRow["CreatedBy"] = userId;
            dataRow["UpdatedBy"] = userId;
            dataRow["DivisionId"] = divisionId;

            var attribute = new MarketplaceAttribute()
            {
                Name = dataRow["Name"].ToString()
            };
            var existing = await connection.QueryAsync<MarketplaceAttribute>("SELECT * FROM MarketplaceAttribute WHERE LOWER(Name) = LOWER(@Name)", attribute);
            if (existing.Any())
            {
                throw new HandledException($"Attribute '{attribute.Name}' already exists");
            }
        }

        transaction.BulkInsert(nameof(MarketplaceAttribute), dataTable);
        transaction.Commit();
    }

    public async Task<string> DownloadMarketplaceAttributeTemplate()
    {
        var columns = new[] { "Name", "Description", "DataType", "IsRequired" };
        var tempFile = Path.GetTempFileName();
        using var wb = new XLWorkbook();
        var sheet = wb.AddWorksheet();
        using var stream = new MemoryStream();

        var keys = columns.Distinct().ToArray();

        const int r = 1;
        var cH = 1;

        foreach (var key in keys)
        {
            sheet.Cell(r, cH).Value = key;
            cH++;
        }

        if (sheet.Tables.Any())
        {
            sheet.Tables.Table(0).ShowAutoFilter = false;
            sheet.Tables.Table(0).SetShowAutoFilter(false);
        }

        wb.SaveAs(stream);
        await using var fs = File.OpenWrite(tempFile);
        stream.Position = 0;
        await stream.CopyToAsync(fs);
        return tempFile;
    }

    public async IAsyncEnumerable<MarketplaceAttribute> AddOrUpdateAttributes(
        IEnumerable<MarketplaceAttribute> attributes)
    {
        foreach (var attribute in attributes)
        {
            yield return await this.AddOrUpdateAttribute(attribute);
        }
    }

    public async Task<byte[]> MarketPlaceExportExcel(bool? isactive)
    {
        using var connection = this.GetConnection();
        var result = new List<ExportMarketPlaceModel>();

        if (isactive is null)
        {
            result = (await connection.QueryAsync<ExportMarketPlaceModel>($"select name as Marketplace,TO_CHAR(datecreated, 'DD/MM/YYYY') AS DateCreated from marketplace order by name asc")).ToList();
        }
        else
        {
            result = (await connection.QueryAsync<ExportMarketPlaceModel>($"select name as Marketplace,TO_CHAR(datecreated, 'DD/MM/YYYY') AS DateCreated from marketplace where isactive = '{isactive}' order by name asc")).ToList();
        }

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("MarketPlace Records");

        if (result.Any())
        {
            var headers = new[] { "MarketPlace", "DateCreated" };
            var headerBackgroundColor = System.Drawing.ColorTranslator.FromHtml("#5787C5");
            var headerTextColor = System.Drawing.Color.White;
            var borderColor = System.Drawing.ColorTranslator.FromHtml("#5787C5");

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cells[1, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.Color.SetColor(headerTextColor);
                cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
                cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(headerBackgroundColor);
                cell.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                cell.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                cell.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                cell.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                cell.Style.Border.Top.Color.SetColor(borderColor);
                cell.Style.Border.Left.Color.SetColor(borderColor);
                cell.Style.Border.Right.Color.SetColor(borderColor);
                cell.Style.Border.Bottom.Color.SetColor(borderColor);
            }

            var row = 2;
            foreach (var record in result)
            {
                worksheet.Cells[row, 1].Value = record.MarketPlace;
                worksheet.Cells[row, 2].Value = record.DateCreated;

                worksheet.Cells[row, 1, row, 15].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;

                for (int col = 1; col <= 2; col++)
                {
                    var dataCell = worksheet.Cells[row, col];
                    dataCell.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    dataCell.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    dataCell.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    dataCell.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    dataCell.Style.Border.Top.Color.SetColor(borderColor);
                    dataCell.Style.Border.Left.Color.SetColor(borderColor);
                    dataCell.Style.Border.Right.Color.SetColor(borderColor);
                    dataCell.Style.Border.Bottom.Color.SetColor(borderColor);
                }

                row++;
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }

        using var memoryStream = new MemoryStream();
        package.SaveAs(memoryStream);
        return memoryStream.ToArray();
    }

    public async Task DeleteMarketPlace(IEnumerable<long> ids)
    {
        using var connection = this.GetConnection();
        foreach (var id in ids)
        {
            await connection.ExecuteAsync($"Delete from marketplace where id = {id}");
        }
    }

    public async IAsyncEnumerable<MarketplaceTemplateAttribute> AddOrUpdateTemplateAttributes(IEnumerable<MarketplaceTemplateAttribute> attributes)
    {
        using var connection = this.GetConnection();
        var validationErrors = new List<string>();

        foreach (var attribute in attributes)
        {
            var productAttribute = await GetProductAttributeById(attribute.ProductAttributeId);

            if (productAttribute == null)
            {
                validationErrors.Add($"Product attribute with ID {attribute.ProductAttributeId} not found.");
                continue;
            }

            // if (productAttribute.DataType == null || attribute.Datatype == null)
            // {
            //    continue;
            // }

            // string expectedDataType = MapDataType((long)productAttribute.DataType);
            // string receivedDataType = MapDataType((long)attribute.Datatype);

            // if (expectedDataType != receivedDataType)
            // {
            //    validationErrors.Add($"Data type mismatch : '{productAttribute.Name}'. Should be {expectedDataType}, not {receivedDataType}");
            // }
            // else
            // {
            yield return await this.AddOrUpdateTemplateAttribute(attribute);
            var marketplaceAttributeData = "SELECT DISTINCT id, name " +
                                              "FROM productattribute " +
                                              "WHERE id IN (SELECT DISTINCT productattributeid FROM marketplacetemplateattribute " +
                                              "WHERE datatype IS NOT NULL AND marketplaceattribute IS NOT NULL " +
                                              "AND marketplacetemplateid = @MarketplaceTemplateId) " +
                                              "ORDER BY id ASC";

            var marketplaceAttributeNames = new List<string>();

            var result = await connection.QueryAsync<ProductAttribute>(marketplaceAttributeData, new { MarketplaceTemplateId = attribute.MarketplaceTemplateId });

            marketplaceAttributeNames.AddRange(result.Select(attr => attr.Name));
            var division = "SELECT divisionid FROM productattribute WHERE id = @Id limit 1";
            var deptcode = attribute.ProductDepartmentId;
            var parameters = new
            {
                Id = attribute.ProductAttributeId
            };
            var productDivision = await connection.QueryFirstOrDefaultAsync<int>(division, parameters);
            var query = $"SELECT p.uid, p.columndata " +
                             $"FROM product p " +
                             $"JOIN itemcodestaging ic ON p.uid = ic.uidcode where p.columndata->>'Division Code'= '{productDivision}' and p.columndata->>'Dept Code'= '{deptcode}'";
            var products = await connection.QueryAsync<Product>(query);

            foreach (var product in products)
            {
                var uid = product.Uid;
                var columnData = product.ColumnData;
                var keysToSearch = columnData.Keys.ToList();
                bool allKeysFound = marketplaceAttributeNames.All(attrName => keysToSearch.Contains(attrName));
                long statusFilter = allKeysFound ? 1 : 0;
                var itemcode = await connection.QueryFirstOrDefaultAsync<ItemCodeStaging>("Select * from itemcodestaging where Uidcode=@Uid", new { Uid = uid });
                await connection.ExecuteAsync($"UPDATE product SET statusfilter = '{statusFilter}',itemcodeid={itemcode.Id} WHERE uid = '{uid}' and columndata->>'Division Code'= '{productDivision}'   and columndata->>'Dept Code'= '{deptcode}'");
            }
        }

        if (validationErrors.Count > 0)
        {
            throw new Exception(string.Join("\n", validationErrors));
        }
    }

    private string MapDataType(long dataType)
    {
        switch (dataType)
        {
            case 0:
                return "Text";
            case 10:
                return "Number";
            case 20:
                return "DateTime";
            case 30:
                return "Lookup";
            case 40:
                return "Boolean";
            default:
                return "Unknown";
        }
    }

    private async Task<ProductAttribute> GetProductAttributeById(long productAttributeId)
    {
        using var connection = this.GetConnection();
        var query = "SELECT name, datatype FROM productattribute WHERE id = @Id";
        var parameters = new
        {
            Id = productAttributeId
        };
        var productAttribute = await connection.QuerySingleOrDefaultAsync<ProductAttribute>(query, parameters);
        return productAttribute;
    }
}