using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Dapper;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.DomainModel;
using HomeCentre.PIM.DAL.Master;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;

namespace HomeCentre.PIM.BAL.Implementations;

public class PgProductManager : PgEntityAction<ProductAttribute>, IProductManager
{
    private readonly IDbCache _cache;
    private readonly IBlobRepo _blobRepo;
    private readonly IUploadManager _uploadManager;
    private readonly object worksheetLock = new object();

    public PgProductManager(IUserContext userContext, IDbConnectionFactory connectionFactory,
        IConfiguration configuration, IDbCache cache, IBlobRepo blobRepo, IUploadManager uploadManager)
        : base(userContext, connectionFactory, configuration)
    {
        _blobRepo = blobRepo;
        _cache = cache;
        _uploadManager = uploadManager;
    }

    public async Task AddOrUpdateAttribute(ProductAttribute attribute)
    {
        // attribute.Name = Regex.Replace(attribute.Name.Trim(), "[^A-Za-z0-9]", "_").Trim('_').ToLower();
        attribute.Name = attribute.Name.Trim();
        attribute.Label = attribute.Label.Trim();
        var attributes = await this._cache.GetAttributes(attribute.DivisionId.GetValueOrDefault());
        var action = this.GetAction<ProductAttribute>();
        using var conncetion = this.GetConnection();
        var division = (await conncetion.QueryAsync<Division>($"select name from division where id = {attribute.DivisionId}")).FirstOrDefault();
        if (attribute.Id == 0)
        {
            if (attributes.Any(x => x.Name.Trim() == attribute.Name.Trim() && x.DivisionId == attribute.DivisionId))
            {
                throw new HandledException(
                    $"Attribute '{attribute.Name}' for Division '{division.Name}' already exists.");
            }

            if (attributes.Any(x => x.Label.Trim() == attribute.Label.Trim() && x.DivisionId == attribute.DivisionId))
            {
                throw new HandledException(
                    $"Attribute '{attribute.Label}' for Division '{division.Name}' already exists.");
            }
        }
        else
        {
            if (attributes.Any(x =>
                    x.Name.Trim() == attribute.Name.Trim() && x.DivisionId == attribute.DivisionId &&
                    x.Id != attribute.Id))
            {
                throw new HandledException(
                    $"Attribute '{attribute.Name}' for Division '{division.Name}' already exists.");
            }

            if (attributes.Any(x =>
                    x.Label.Trim() == attribute.Label.Trim() && x.DivisionId == attribute.DivisionId &&
                    x.Id != attribute.Id))
            {
                throw new HandledException(
                    $"Attribute '{attribute.Label}' for Division '{division.Name}' already exists.");
            }
        }

        await action.AddOrUpdate(attribute);
        await this._cache.Clear();
    }

    public async Task<IEnumerable<ProductAttribute>> ListAttributes(long? divisionId, bool isActive)
    {
        var action = this.GetAction<ProductAttribute>();
        if (divisionId == null)
        {
            return await action.GetAll("where isactive = @isActive order by name asc", new
            {
                isActive,
            });
        }

        return await action.GetAll("where isactive = @isActive and divisionid = @divisionId order by name asc", new
        {
            isActive,
            divisionId
        });
    }

    public async Task MoveAttribute(long attributeId, long departmentId)
    {
        await this.DoUpdate(new
        {
            id = attributeId,
            departmentId
        }, "productattribute");
        await this._cache.Clear();
    }

    public async Task ActivateAttributes(long[] attributeIds)
    {
        await this.Activate(attributeIds);
        await this._cache.Clear();
    }

    public async Task DeactivateAttributes(long[] attributeIds)
    {
        await this.Deactivate(attributeIds);
        using var connection = this.GetConnection();
        await connection.ExecuteAsync("delete from templateattribute where attributeid = any(@attributeIds)",
            new { attributeIds });
        await this._cache.Clear();
    }

    public async Task<SearchResult<Product>> SearchProducts(string searchKey, int pageNumber, int pageCount,
        long? qcStatusId)
    {
        if (qcStatusId == null)
        {
            return await this.Search<Product>(searchKey, pageNumber, pageCount, "product");
        }

        var dynParams = new DynamicParameters();
        dynParams.Add("qcStatusId", qcStatusId);
        return await this.Search<Product>(searchKey, pageNumber, pageCount, "product", "qcStatusId = @qcStatusId", dynParams);
    }

    public async Task<string> DownloadProducts()
    {
        var records = (await this.SearchProducts(string.Empty, 0, 0, null)).Records;
        var tempFile = Path.GetTempFileName();
        var uidTemplate = (await this._cache.GetTemplates()).First(x => x.IsUidTemplate);
        var templateAttributes = await this._cache.GetTemplateAttributes(uidTemplate.Id);
        using var wb = new XLWorkbook();
        var sheet = wb.AddWorksheet();
        using var stream = new MemoryStream();

        if (records.Count > 0)
        {
            // var keys = templateAttributes.Select(x => x.Name).Where(x => x != "uid").Distinct();
            var keys = new[] { "planogram_code", "season", "product_desc", "class_code", "div_code", "dept_code" };

            var r = 1;
            var cH = 1;

            sheet.Cell(r, cH).Value = "id";
            cH++;
            sheet.Cell(r, cH).Value = "uid";
            cH++;
            foreach (var key in keys)
            {
                sheet.Cell(r, cH).Value = key;
                cH++;
            }

            r++;
            foreach (var record in records)
            {
                var c = 1;
                sheet.Cell(r, c).Value = record.Id.ToString();
                c++;
                sheet.Cell(r, c).Value = record.Uid.ToString();
                c++;
                foreach (var key in keys)
                {
                    sheet.Cell(r, c).Value = record.ColumnData.ContainsKey(key) ? record.ColumnData[key] : string.Empty;
                    c++;
                }

                r++;
            }
        }

        sheet.RangeUsed().CreateTable();
        sheet.Tables.Table(0).Theme = XLTableTheme.TableStyleLight9;
        sheet.Tables.Table(0).ShowAutoFilter = false;
        sheet.Tables.Table(0).SetShowAutoFilter(false);
        wb.SaveAs(stream);
        await using var fs = File.OpenWrite(tempFile);
        stream.Position = 0;
        await stream.CopyToAsync(fs);
        return tempFile;
    }

    public async Task<List<string>> GetUids()
    {
        using var connection = this.GetConnection();
        return (await connection.QueryAsync<string>("select uid from product where isactive = true")).ToList();
    }

    public async Task<List<ItemCodeProducts>> ListProducts(DateTime? fromdate, DateTime? todate, string status, long deptcode)
    {
        using var connection = this.GetConnection();
        var query = "SELECT *,status as Status FROM product";
        var startDates = fromdate ?? DateTime.MinValue;
        var endDates = todate ?? DateTime.MaxValue;
        var formattedStartDate = startDates.ToString("yyyy/MM/dd");
        var formattedEndDate = endDates.ToString("yyyy/MM/dd");
        query += $" WHERE to_char(datecreated::date::timestamp with time zone, 'yyyy/MM/dd'::text)between '{formattedStartDate}' AND '{formattedEndDate}'";

        if (deptcode != 0)
        {
            if (query.Contains("WHERE"))
            {
                query += $" AND (columndata->>'Dept Code' = '{deptcode}'::text) AND status IS NOT NULL ";
            }
            else
            {
                query += $" WHERE (columndata->>'Dept Code' = '{deptcode}'::text)";
            }
        }

        if (!string.IsNullOrEmpty(status))
        {
            if (query.Contains("WHERE"))
            {
                if (status == "0")
                {
                    query += $" AND status IS NOT NULL order by datecreated";
                }
                else
                {
                    query += $" AND (status = '{status}') order by datecreated";
                }
            }
            else
            {
                if (status == "0")
                {
                    query += $" order by datecreated";
                }
                else
                {
                    query += $" WHERE (status = '{status}') order by datecreated";
                }
            }
        }
        else
        {
            if (query.Contains("WHERE"))
            {
                query += " AND status IS NOT NULL order by datecreated";
            }
            else
            {
                query += " AND status IS NOT NULL order by datecreated";
            }
        }

        var products = await connection.QueryAsync<ItemCodeProducts>(query);
        var jsonString = await _uploadManager.ItemCodeAttributeJsonExtraction();
        return products.ToList();
    }

    public async Task<List<ItemCodeProducts>> ListProductsItem(DateTime? fromdate, DateTime? todate, string status, string? deptcode, string? division)
    {
        using var connection = this.GetConnection();
        var query = "SELECT uid as Uid,div_name as Div_Name,div_code as Div_Code,dept_code as Dept_Code,dept_name as Dept_Name,status as Status,dateCreated as DateCreated FROM vw_uid WHERE isactive = true";

        var startDates = fromdate ?? DateTime.MinValue;
        var endDates = todate ?? DateTime.MaxValue;
        var formattedStartDate = startDates.ToString("yyyy/MM/dd");
        var formattedEndDate = endDates.ToString("yyyy/MM/dd");
        if (fromdate != null || todate != null)
        {
            if (query.Contains("WHERE"))
            {
                query += $" AND to_char(datecreated::date::timestamp with time zone, 'yyyy/MM/dd'::text)between '{formattedStartDate}' AND '{formattedEndDate}' ";
            }
            else
            {
                query += $" WHERE to_char(datecreated::date::timestamp with time zone, 'yyyy/MM/dd'::text)between '{formattedStartDate}' AND '{formattedEndDate}' ";
            }
        }

        if (!string.IsNullOrEmpty(deptcode) && deptcode != "0")
        {
            string[] departments = deptcode.Split(',');
            deptcode = string.Join(", ", departments.Select(d => $"'{d.Trim()}'"));
            if (query.Contains("WHERE"))
            {
                query += $" AND dept_code in ({deptcode}) ";
            }
            else
            {
                query += $" WHERE dept_code in ({deptcode}) ";
            }
        }

        if (!string.IsNullOrEmpty(division) && division != "0")
        {
            string[] divisions = division.Split(',');
            division = string.Join(", ", divisions.Select(d => $"'{d.Trim()}'"));
            if (query.Contains("WHERE"))
            {
                query += $" AND div_code in ({division}) ";
            }
            else
            {
                query += $" WHERE div_code in ({division}) ";
            }
        }

        if (!string.IsNullOrEmpty(status))
        {
            if (query.Contains("WHERE"))
            {
                if (status == "1")
                {
                    query += $" AND (status = '{status}') order by datecreated desc ";
                }
                else if (status == "2")
                {
                    query += $" AND (status IS NULL OR status = '0') order by datecreated desc ";
                }
                else
                {
                    query += $" order by datecreated desc ";
                }
            }
            else
            {
                if (status == "1")
                {
                    query += $" WHERE (status = '{status}') order by datecreated desc ";
                }
                else if (status == "2")
                {
                    query += $" WHERE (status IS NULL OR status = '0') order by datecreated desc ";
                }
                else
                {
                    query += $" order by datecreated desc ";
                }
            }
        }

        var products = await connection.QueryAsync<ItemCodeProducts>(query);
        return products.ToList();
    }

    public async Task<List<ItemCodeProducts>> ListAllProducts(DateTime? fromdate, DateTime? todate, string status, long deptcode)
    {
        using var connection = this.GetConnection();
        var query = "SELECT *,statusfilter as Status FROM product";

        var startDates = fromdate ?? DateTime.MinValue;
        var endDates = todate ?? DateTime.MaxValue;
        var formattedStartDate = startDates.ToString("yyyy/MM/dd");
        var formattedEndDate = endDates.ToString("yyyy/MM/dd");
        query += $" WHERE to_char(datecreated::date::timestamp with time zone, 'yyyy/MM/dd'::text)between '{formattedStartDate}' AND '{formattedEndDate}'";

        if (deptcode != 0)
        {
            if (query.Contains("WHERE"))
            {
                query += $" AND (columndata->>'Dept Code' = '{deptcode}'::text)";
            }
            else
            {
                query += $" WHERE (columndata->>'Dept Code' = '{deptcode}'::text)";
            }
        }

        if (!string.IsNullOrEmpty(status))
        {
            if (query.Contains("WHERE"))
            {
                query += $" AND (statusfilter = '0' or statusfilter = '1') AND itemcodeid IS NOT NULL order by datecreated";
            }
            else
            {
                query += $" WHERE (statusfilter = '0' or statusfilter = '1') AND itemcodeid IS NOT NULL order by datecreated";
            }
        }
        else
        {
            if (query.Contains("WHERE"))
            {
                query += " AND itemcodeid IS NOT NULL order by datecreated";
            }
            else
            {
                query += " WHERE itemcodeid IS NOT NULL order by datecreated";
            }
        }

        var products = await connection.QueryAsync<ItemCodeProducts>(query);
        return products.ToList();
    }

    public async Task<string> ProductExportExcel(DateTime? fromDate, DateTime? toDate, string? season, string? dept_code, string? class_code, string? subclass_code, long[]? roleIds)
    {
        using var connection = this.GetConnection();

        // var records = (await this.SearchProducts(string.Empty, 0, 0, null)).Records;
        var startDates = fromDate ?? DateTime.MinValue;

        var endDates = toDate ?? DateTime.MaxValue;

        var seasonData = season;

        var formattedStartDate = startDates.ToString("yyyy/MM/dd");

        var formattedEndDate = endDates.ToString("yyyy/MM/dd");

        var query = "select uid AS Uid,dept_name AS Department_Name,Planogram_Family_Name AS Planogram_Family_Name," +
                        "div_name AS Division_Name,class_name AS Class_Name,subclass_name AS Subclass_Name,group_name AS Group_Name,season_name AS Season,pricegrouping AS Price_Grouping,productpurpose AS Product_Purpose,target_mrp AS Target_MRP,product_desc AS Product_Desc,pictureid from vw_uid ";
        bool isAnyConditionAdded = false;

        if (fromDate != null || toDate != null)
        {
            query += $" WHERE to_char(datecreated::date::timestamp with time zone, 'yyyy/MM/dd'::text) BETWEEN '{formattedStartDate}' AND '{formattedEndDate}'";
            isAnyConditionAdded = true;
        }

        if (roleIds != null && !roleIds.Contains(1))
        {
            query += isAnyConditionAdded ? " AND " : " WHERE ";
            query += $" isactive = true ";
            isAnyConditionAdded = true;
        }

        if ((season != null && season.Length > 0) ||
            (dept_code != null && dept_code.Length > 0) ||
            (class_code != null && class_code.Length > 0) ||
            (subclass_code != null && subclass_code.Length > 0))
        {
            query += isAnyConditionAdded ? " AND " : " WHERE ";

            if (season != null && season.Length > 0)
            {
                string[] elements = season.Split(',');
                var result = elements.Select(element => $"'{element}'");
                string seasonValues = $"{string.Join(",", result)}";
                query += $"season_name IN ({seasonValues})";
                isAnyConditionAdded = true;
            }

            if (dept_code != null && dept_code.Length > 0)
            {
                query += (season != null && season.Length > 0) ? " AND " : string.Empty;
                string[] elements = dept_code.Split(',');
                var result = elements.Select(element => $"'{element}'");
                string deptValues = $"{string.Join(",", result)}";
                query += $"dept_name IN ({deptValues})";
                isAnyConditionAdded = true;
            }

            if (class_code != null && class_code.Length > 0)
            {
                query += (season != null && season.Length > 0) || (dept_code != null && dept_code.Length > 0) ? " AND " : string.Empty;
                string[] elements = class_code.Split(',');
                var result = elements.Select(element => $"'{element}'");
                string classValues = $"{string.Join(",", result)}";
                query += $"class_name IN ({classValues})";
                isAnyConditionAdded = true;
            }

            if (subclass_code != null && subclass_code.Length > 0)
            {
                query += (season != null && season.Length > 0) || (dept_code != null && dept_code.Length > 0) || (class_code != null && class_code.Length > 0) ? " AND " : string.Empty;
                string[] elements = subclass_code.Split(',');
                var result = elements.Select(element => $"'{element}'");
                string subclassValues = $"{string.Join(",", result)}";
                query += $"subclass_name IN ({subclassValues})";
                isAnyConditionAdded = true;
            }

            query += " order by datecreated desc ";
        }

        if (fromDate == null && toDate == null && subclass_code == null && class_code == null && dept_code == null && (seasonData == null || seasonData.Length == 0))
        {
            query += $" order by datecreated desc "; ////where to_char(cd.datecreated::date::timestamp with time zone, 'yyyy/MM/dd'::text)between '{formattedStartDate}' AND '{formattedEndDate}' and
        }

        var products = await connection.QueryAsync<UidExportExcel>(query);
        var product = await connection.QueryAsync<Product>("SELECT * FROM product WHERE pictureid <> 0");
        var uploadPicture = await connection.QueryAsync<UploadPicture>("select * from uploadpicture");
        var tempFile = Path.GetTempFileName();

        // var uidTemplate = (await this._cache.GetTemplates()).First(x => x.IsUidTemplate);
        // var templateAttributes = await this._cache.GetTemplateAttributes(uidTemplate.Id);
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Sheet1");
        using var stream = new MemoryStream();
        if (products.Any())
        {
            var keys = new[] { "Uid", "Planogram Name", "Division Name", "Department Name", "Season", "Product Desc", "Group Name", "Class Name", "Subclass Name", "Target MRP", "Product Purpose", "Price Grouping", "Picture" };
            var r = 1;
            var cH = 1;
            var headerColor = System.Drawing.Color.SteelBlue;
            var textColor = System.Drawing.Color.White;

            // Write headers
            for (int i = 0; i < keys.Length; i++)
            {
                var cell = worksheet.Cells[r, cH + i];
                cell.Value = keys[i];
                cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(headerColor);
                cell.Style.Font.Color.SetColor(textColor);
                cell.Style.Font.Bold = true;

                worksheet.Column(cH + i).Width = 15;
            }

            r++;
            var picturesToAdd = new List<(int Row, int Column, string ImageUrl)>();

            var productList = products.Select((product, index) => new { Product = product, RowIndex = index + 2 }).ToList();
            var rowCount = productList.Count;
            var columnCount = keys.Count();
            var data = new ConcurrentBag<(int Index, object[] RowData)>();

            Parallel.ForEach(productList.Select((record, index) => new { record, index }), item =>
            {
                var record = item.record;
                var index = item.index;
                var product = record.Product;
                var rowData = new object[columnCount];
                var colIndex = 0;

                var pictureUrl = uploadPicture
                                .Where(up => up.Id.ToString() == product.PictureId)
                                .Select(up => up.ImageUrl)
                                .FirstOrDefault();

                if (keys.Contains("Uid"))
                {
                    rowData[colIndex++] = product.Uid.ToString();
                }

                if (keys.Contains("Planogram Name"))
                {
                    var propertyName = ConvertColumnNameToPropertyName("Planogram Family Name");
                    var propertyInfo = product.GetType().GetProperty(propertyName);
                    var columnValue = propertyInfo?.GetValue(product)?.ToString();
                    rowData[colIndex++] = columnValue;
                }

                if (keys.Contains("Division Name"))
                {
                    var propertyName = ConvertColumnNameToPropertyName("Division Name");
                    var propertyInfo = product.GetType().GetProperty(propertyName);
                    var columnValue = propertyInfo?.GetValue(product)?.ToString();
                    rowData[colIndex++] = columnValue;
                }

                if (keys.Contains("Department Name"))
                {
                    var propertyName = ConvertColumnNameToPropertyName("Department Name");
                    var propertyInfo = product.GetType().GetProperty(propertyName);
                    var columnValue = propertyInfo?.GetValue(product)?.ToString();
                    rowData[colIndex++] = columnValue;
                }

                if (keys.Contains("Season"))
                {
                    var propertyName = ConvertColumnNameToPropertyName("Season");
                    var propertyInfo = product.GetType().GetProperty(propertyName);
                    var columnValue = propertyInfo?.GetValue(product)?.ToString();
                    rowData[colIndex++] = columnValue;
                }

                if (keys.Contains("Product Desc"))
                {
                    var propertyName = ConvertColumnNameToPropertyName("Product Desc");
                    var propertyInfo = product.GetType().GetProperty(propertyName);
                    var columnValue = propertyInfo?.GetValue(product)?.ToString();
                    rowData[colIndex++] = columnValue;
                }

                if (keys.Contains("Group Name"))
                {
                    var propertyName = ConvertColumnNameToPropertyName("Group Name");
                    var propertyInfo = product.GetType().GetProperty(propertyName);
                    var columnValue = propertyInfo?.GetValue(product)?.ToString();
                    rowData[colIndex++] = columnValue;
                }

                if (keys.Contains("Class Name"))
                {
                    var propertyName = ConvertColumnNameToPropertyName("Class Name");
                    var propertyInfo = product.GetType().GetProperty(propertyName);
                    var columnValue = propertyInfo?.GetValue(product)?.ToString();
                    rowData[colIndex++] = columnValue;
                }

                if (keys.Contains("Subclass Name"))
                {
                    var propertyName = ConvertColumnNameToPropertyName("Subclass Name");
                    var propertyInfo = product.GetType().GetProperty(propertyName);
                    var columnValue = propertyInfo?.GetValue(product)?.ToString();
                    rowData[colIndex++] = columnValue;
                }

                if (keys.Contains("Target MRP"))
                {
                    var propertyName = ConvertColumnNameToPropertyName("Target MRP");
                    var propertyInfo = product.GetType().GetProperty(propertyName);
                    var columnValue = propertyInfo?.GetValue(product)?.ToString();
                    rowData[colIndex++] = columnValue;
                }

                if (keys.Contains("Product Purpose"))
                {
                    var propertyName = ConvertColumnNameToPropertyName("Product Purpose");
                    var propertyInfo = product.GetType().GetProperty(propertyName);
                    var columnValue = propertyInfo?.GetValue(product)?.ToString();
                    rowData[colIndex++] = columnValue;
                }

                if (keys.Contains("Price Grouping"))
                {
                    var propertyName = ConvertColumnNameToPropertyName("Price Grouping");
                    var propertyInfo = product.GetType().GetProperty(propertyName);
                    var columnValue = propertyInfo?.GetValue(product)?.ToString();
                    rowData[colIndex++] = columnValue;
                }

                if (keys.Contains("Picture"))
                {
                    if (!string.IsNullOrEmpty(pictureUrl))
                    {
                        picturesToAdd.Add((record.RowIndex, colIndex + 1, pictureUrl));
                    }
                    else
                    {
                        lock (worksheetLock)
                        {
                            worksheet.Cells[record.RowIndex, colIndex + 1].Value = string.Empty;
                        }
                    }

                    colIndex++;
                }

                data.Add((index, rowData));
            });

            var finalDataArray = data.OrderBy(d => d.Index).Select(d => d.RowData).ToArray();

            worksheet.Cells[2, 1].LoadFromArrays(finalDataArray);

            // var rows = new List<ExcelRow>();
            // package.DoAdjustDrawings = false;
            var rowsWithPictures = picturesToAdd.Select(p => p.Row).Distinct();
            foreach (var rowNumber in rowsWithPictures)
            {
                worksheet.Row(rowNumber).Height = 50;
            }

            foreach (var pictureInfo in picturesToAdd)
            {
                var (rowNumber, columnNumber, imageUrl) = pictureInfo;
                var filePath = _blobRepo.DownloadFile(imageUrl).Result;

                if (File.Exists(filePath))
                {
                    using (var imageStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        var image = Image.FromStream(imageStream);
                        var picture = worksheet.Drawings.AddPicture(Guid.NewGuid().ToString(), image);
                        picture.SetPosition(rowNumber - 1, 0, columnNumber - 1, 0);
                        picture.SetSize(60, 60);
                        worksheet.Column(columnNumber).Width = 10;

                        // rows.Add(worksheet.Row(rowNumber));
                    }
                }
            }

            // foreach (var r in rows)
            // {
            //    r.Height = 50;
            // }
        }

        package.SaveAs(new FileInfo(tempFile));
        return tempFile;
    }

    public async Task<string> AttributesExcel(DateTime? fromDate, DateTime? toDate, string? season, string? dept_code, string? class_code, string? subclass_code, long[]? roleIds)
    {
        using var connection = this.GetConnection();
        var seasonData = season;
        var startDates = fromDate ?? DateTime.MinValue;
        var endDates = toDate ?? DateTime.MaxValue;
        var formattedStartDate = startDates.ToString("yyyy/MM/dd");
        var formattedEndDate = endDates.ToString("yyyy/MM/dd");

        var tempFile = Path.GetTempFileName();

        using var package = new ExcelPackage();

        // Create the Excel worksheet
        var worksheet = package.Workbook.Worksheets.Add("Sheet1");

        // Build the query
        var query = string.Empty;
        if (fromDate == null && toDate == null && season == null && dept_code == null && class_code == null && subclass_code == null)
        {
            query = "select * from vw_product_details";
        }
        else
        {
            query = "select cd.*,planogram_family_name AS Planogram_Family_Name,supplier_name as Supplier_Name,uom as UOM,season_name as Season_Name, product_purpose as Product_Purpose, " +
                    "price_grouping as Price_Grouping,product_cbm as Product_CBM from vw_product_details cd ";

            bool isAnyConditionAdded = false;
            if (fromDate != null || toDate != null)
            {
                query += $" WHERE to_char(cd.datecreated::date::timestamp with time zone, 'yyyy/MM/dd'::text) BETWEEN '{formattedStartDate}' AND '{formattedEndDate}'";
                isAnyConditionAdded = true;
            }

            if (roleIds != null && !roleIds.Contains(1))
            {
                query += isAnyConditionAdded ? " AND " : " WHERE ";
                query += $" isactive = true ";
                isAnyConditionAdded = true;
            }

            if ((season != null && season.Length > 0) ||
                (dept_code != null && dept_code.Length > 0) ||
                (class_code != null && class_code.Length > 0) ||
                (subclass_code != null && subclass_code.Length > 0))
            {
                query += isAnyConditionAdded ? " AND (" : " WHERE (";

                if (season != null && season.Length > 0)
                {
                    string[] elements = season.Split(',');
                    var result = elements.Select(element => $"'{element}'");
                    string seasonValues = $"{string.Join(",", result)}";
                    query += $"season_name IN ({seasonValues})";
                    isAnyConditionAdded = true;
                }

                if (dept_code != null && dept_code.Length > 0)
                {
                    query += (season != null && season.Length > 0) ? " AND " : string.Empty;
                    string[] elements = dept_code.Split(',');
                    var result = elements.Select(element => $"'{element}'");
                    string deptValues = $"{string.Join(",", result)}";
                    query += $"dept_name IN ({deptValues})";
                    isAnyConditionAdded = true;
                }

                if (class_code != null && class_code.Length > 0)
                {
                    query += (season != null && season.Length > 0) || (dept_code != null && dept_code.Length > 0) ? " AND " : string.Empty;
                    string[] elements = class_code.Split(',');
                    var result = elements.Select(element => $"'{element}'");
                    string classValues = $"{string.Join(",", result)}";
                    query += $"class_name IN ({classValues})";
                    isAnyConditionAdded = true;
                }

                if (subclass_code != null && subclass_code.Length > 0)
                {
                    query += (season != null && season.Length > 0) || (dept_code != null && dept_code.Length > 0) || (class_code != null && class_code.Length > 0) ? " AND " : string.Empty;
                    string[] elements = subclass_code.Split(',');
                    var result = elements.Select(element => $"'{element}'");
                    string subclassValues = $"{string.Join(",", result)}";
                    query += $"subclass_name IN ({subclassValues})";
                    isAnyConditionAdded = true;
                }

                query += ")";
            }

            if (query.ToString().Contains("WHERE"))
            {
                query += "AND ";
            }
            else
            {
                query += " WHERE ";
            }

            query += " uid IS NOT NULL order by cd.datecreated desc ";
        }

        var products = await connection.QueryAsync<AllAttributesData>(query);

        var keys = await connection.QueryAsync<string>("SELECT distinct name FROM productattribute where isactive = true and divisionid = 61  order by name asc");
        var productImages = await connection.QueryAsync<UploadPicture>("SELECT up.imageurl, p.uid as Name FROM product p JOIN uploadpicture up ON p.pictureid = up.id WHERE p.pictureid <> 0");

        try
        {
            if (keys.Any())
            {
                var desiredColumnOrder = new List<string>
            {
                "Uid", "Planogram/Family Code", "Planogram Family Name", "Season Code", "Season Name", "Division Code", "Division Name",
                "Dept Code", "Dept Name", "Group Code", "Group Name", "Class Code", "Class Name", "Subclass Code", "Subclass Name",
                "Brand Code", "Brand Name"
            };

                var mergedColumns = desiredColumnOrder.Concat(keys.Except(desiredColumnOrder)).ToList();

                var headerColor = System.Drawing.Color.SteelBlue;
                var textColor = System.Drawing.Color.White; // White text color
                for (int cH = 1; cH <= mergedColumns.Count; cH++)
                {
                    var cell = worksheet.Cells[1, cH];
                    cell.Value = mergedColumns[cH - 1];
                    cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(headerColor);
                    cell.Style.Font.Color.SetColor(textColor);
                    cell.Style.Font.Bold = true;

                    worksheet.Column(cH).Width = 15;
                }

                var picturesToAdd = new List<(int Row, int Column, string ImageUrl)>();
                var productList = products.Select((product, index) => new { Product = product, RowIndex = index + 2 }).ToList();
                var rowCount = productList.Count;
                var columnCount = mergedColumns.Count;
                var data = new ConcurrentBag<(int Index, object[] RowData)>();

                Parallel.ForEach(productList.Select((record, index) => new { record, index }), item =>
                {
                    var record = item.record;
                    var index = item.index;
                    var product = record.Product;
                    var rowData = new object[columnCount];
                    var colIndex = 0;
                    var pictures = productImages.Where(a => a.Name == product.Uid).Select(a => a.ImageUrl).FirstOrDefault();

                    if (mergedColumns.Contains("Uid"))
                    {
                        rowData[colIndex++] = product.Uid.ToString();
                    }

                    if (mergedColumns.Contains("Planogram/Family Code"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Planogram/Family Code");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Planogram Family Name"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Planogram Family Name");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Season Code"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Season Code");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Season Name"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Season Name");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Division Code"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Division Code");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Division Name"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Division Name");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Dept Code"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Dept Code");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Dept Name"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Dept Name");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Group Code"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Group Code");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Group Name"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Group Name");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Class Code"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Class Code");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Class Name"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Class Name");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Subclass Code"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Subclass Code");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Subclass Name"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Subclass Name");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Brand Code"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Brand Code");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Brand Name"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Brand Name");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Aesthetic"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Aesthetic");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Assembly Required"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Assembly Required");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("BAU Discount"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("BAU Discount");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Brand EAN code"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Brand EAN code");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Brand Type"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Brand Type");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Buyers Thought"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Buyers Thought");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Care Instruction 1 Cleaning"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Care Instruction 1 Cleaning");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Care Instruction 2 Caring Dos"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Care Instruction 2 Caring Dos");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Care Instruction 3 Caring Don't"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Care Instruction 3 Caring Don't");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Care Instruction 4 Storage Instructions"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Care Instruction 4 Storage Instructions");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Care Instruction 5"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Care Instruction 5");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Care Instruction 6"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Care Instruction 6");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Color Code"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Color Code");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Color Desc"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Color Desc");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Cost Based MRP Ind"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Cost Based MRP Ind");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Country Of Origin"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Country Of Origin");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Currency"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Currency");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Customer Centric Size"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Customer Centric Size");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Diff Group2 Val"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Diff Group2 Val");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("EAN Code"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("EAN Code");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("EXW FOB Cost"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("EXW FOB Cost");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("FAB Based on Finish Process"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("FAB Based on Finish Process");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("FAB Based on Material"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("FAB Based on Material");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("First Not Before Date"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("First Not Before Date");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();

                        DateTime parsedDate;
                        string formattedDate = null;
                        string[] dateFormats = { "dd/MM/yyyy", "dd-MM-yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "dd MMM yyyy", "yyyy-MM-ddTHH:mm:ss" };

                        if (DateTime.TryParseExact(columnValue, dateFormats, null, System.Globalization.DateTimeStyles.None, out parsedDate))
                        {
                            formattedDate = parsedDate.ToString("dd/MM/yyyy");
                        }
                        else if (DateTime.TryParse(columnValue, out parsedDate))
                        {
                            formattedDate = parsedDate.ToString("dd/MM/yyyy");
                        }

                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("GST %"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("GST %");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("HSN Code"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("HSN Code");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Ideal For"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Ideal For");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Initital Product Size (H) (CM)"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Initital Product Size (H) (CM)");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Initital Product Size (L) (CM)"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Initital Product Size (L) (CM)");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Initital Product Size (W) (CM)"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Initital Product Size (W) (CM)");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Inner Carton Code"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Inner Carton Code");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Inner Carton Desc"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Inner Carton Desc");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Inner Pack Carton CBM"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Inner Pack Carton CBM");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Inner Pack Dimensions H (CM)"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Inner Pack Dimensions H (CM)");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Inner Pack Dimensions L (CM)"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Inner Pack Dimensions L (CM)");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Inner Pack Dimensions W (CM)"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Inner Pack Dimensions W (CM)");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Inner Pack Gross Weight"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Inner Pack Gross Weight");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Is Fragile"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Is Fragile");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Item Code"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Item Code");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Item Desc"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Item Desc");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Item Shape"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Item Shape");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Item Type code"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Item Type code");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Item Type Desc"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Item Type Desc");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Launch Date"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Launch Date");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();

                        DateTime parsedDate;
                        string formattedDate = null;
                        string[] dateFormats = { "dd/MM/yyyy", "dd-MM-yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "dd MMM yyyy", "yyyy-MM-ddTHH:mm:ss" };

                        if (DateTime.TryParseExact(columnValue, dateFormats, null, System.Globalization.DateTimeStyles.None, out parsedDate))
                        {
                            formattedDate = parsedDate.ToString("dd/MM/yyyy");
                        }
                        else if (DateTime.TryParse(columnValue, out parsedDate))
                        {
                            formattedDate = parsedDate.ToString("dd/MM/yyyy");
                        }

                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Lead Time Days"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Lead Time Days");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Legal Disclaimer"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Legal Disclaimer");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Loipl Shoot Required"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Loipl Shoot Required");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Made Of Value"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Made Of Value");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Margin %"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Margin %");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Margin Applied On"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Margin Applied On");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Master Carton Code"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Master Carton Code");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Master Carton Desc"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Master Carton Desc");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Master Carton Dimensions H (CM)"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Master Carton Dimensions H (CM)");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Master Carton Dimensions L (CM)"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Master Carton Dimensions L (CM)");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Master Carton Dimensions W (CM)"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Master Carton Dimensions W (CM)");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Master Carton Gross Weight"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Master Carton Gross Weight");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Material Description"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Material Description");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("MB CBM"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("MB CBM");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("MOQ"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("MOQ");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("MOQ Remark 1"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("MOQ Remark 1");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("MOQ Remark 2"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("MOQ Remark 2");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("MRP"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("MRP");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Net Qty As Per MRP Label"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Net Qty As Per MRP Label");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Net Weight Inner Carton"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Net Weight Inner Carton");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("No Of Stores Planned"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("No Of Stores Planned");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Packaging Image"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Packaging Image");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Packaging Product size (H)"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Packaging Product size (H)");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Packaging Product size (L)"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Packaging Product size (L)");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Packaging Product size (W)"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Packaging Product size (W)");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Packaging Type"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Packaging Type");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Packcode"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Packcode");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Pack Contents"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Pack Contents");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("PD Packaging Size H (CM)"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("PD Packaging Size H (CM)");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("PD Packaging Size L (CM)"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("PD Packaging Size L (CM)");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("PD Packaging Size W (CM)"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("PD Packaging Size W (CM)");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("PD Packaging Type"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("PD Packaging Type");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Planned Discount"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Planned Discount");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Planogram Fitment"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Planogram Fitment");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("PLC Code"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("PLC Code");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("PLC Desc"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("PLC Desc");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Plotting Remark"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Plotting Remark");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Price Grouping"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Price Grouping");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Primary Material"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Primary Material");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Primary Packaging"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Primary Packaging");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Primary Selling Feature"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Primary Selling Feature");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Primary Selling Feature Value"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Primary Selling Feature Value");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Process Techniques"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Process Techniques");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Product Capacity Volume (ML)"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Product Capacity Volume (ML)");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Product CBM"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Product CBM");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Product Classification code"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Product Classification code");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Product Classification Desc"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Product Classification Desc");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Product Desc"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Product Desc");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Product Dimension H (CM)"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Product Dimension H (CM)");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Product Dimension L (CM)"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Product Dimension L (CM)");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Product Dimension W (CM)"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Product Dimension W (CM)");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Product Gross Weight (GMS)"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Product Gross Weight (GMS)");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Product Grouping"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Product Grouping");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Product Net Weight (GMS)"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Product Net Weight (GMS)");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Product Picture"))
                    {
                        if (!string.IsNullOrEmpty(pictures))
                        {
                            picturesToAdd.Add((record.RowIndex, colIndex + 1, pictures));
                        }
                        else
                        {
                            lock (worksheetLock)
                            {
                                worksheet.Cells[record.RowIndex, colIndex + 1].Value = string.Empty;
                            }
                        }

                        colIndex++;
                    }

                    if (mergedColumns.Contains("Product Purpose"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Product Purpose");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Product Sub Type 1"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Product Sub Type 1");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Product Sub Type 2"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Product Sub Type 1");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Product Type 1"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Product Type 1");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Product Type 2"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Product Type 2");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Product Validation Code"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Product Validation Code");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Product Vendor Code"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Product Vendor Code");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Purchase Type"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Purchase Type");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Purchase Type Desc"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Purchase Type Desc");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Ref SKU 1"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Ref SKU 1");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Ref SKU 2"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Ref SKU 2");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Ref Sku For Floor Display"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Ref Sku For Floor Display");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Ref SKU Remarks"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Ref SKU Remarks");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("RMS Season Code"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("RMS Season Code");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("RMS Season Name"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("RMS Season Name");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("RMS Supplier Code"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("RMS Supplier Code");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("RRP"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("RRP");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Safety Instruction"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Safety Instruction");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Secondary Selling Feature"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Secondary Selling Feature");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Secondary Selling Feature Value"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Secondary Selling Feature Value");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Selling Tips"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Selling Tips");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Shelf Edge Description"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Shelf Edge Description");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Specific Product Name"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Specific Product Name");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Store Category"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Store Category");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Style Name"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Style Name");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Sub Range Collection Name"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Sub Range Collection Name");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Supplier Name"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Supplier Name");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Target MRP"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Target MRP");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Tax Category"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Tax Category");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Tertiary Selling Feature"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Tertiary Selling Feature");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Tertiary Selling Feature Value"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Tertiary Selling Feature Value");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("UID Status"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("UID Status");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Unit Sale Price"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Unit Sale Price");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("UOM"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("UOM");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Utility Functional Usage USP"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Utility Functional Usage USP");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    if (mergedColumns.Contains("Warranty"))
                    {
                        var propertyName = ConvertColumnNameToPropertyName("Warranty");
                        var propertyInfo = product.GetType().GetProperty(propertyName);
                        var columnValue = propertyInfo?.GetValue(product)?.ToString();
                        rowData[colIndex++] = columnValue;
                    }

                    data.Add((index, rowData));
                });

                var finalDataArray = data.OrderBy(d => d.Index).Select(d => d.RowData).ToArray();

                worksheet.Cells[2, 1].LoadFromArrays(finalDataArray);

                // var rows = new List<ExcelRow>();
                // package.DoAdjustDrawings = false;
                var rowsWithPictures = picturesToAdd.Select(p => p.Row).Distinct();
                foreach (var rowNumber in rowsWithPictures)
                {
                    worksheet.Row(rowNumber).Height = 50;
                }

                foreach (var pictureInfo in picturesToAdd)
                {
                    var (rowNumber, columnNumber, imageUrl) = pictureInfo;
                    var filePath = _blobRepo.DownloadFile(imageUrl).Result;

                    if (File.Exists(filePath))
                    {
                        using (var imageStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                        {
                            var image = Image.FromStream(imageStream);
                            var picture = worksheet.Drawings.AddPicture(Guid.NewGuid().ToString(), image);
                            picture.SetPosition(rowNumber - 1, 0, columnNumber - 1, 0);
                            picture.SetSize(60, 60);
                            worksheet.Column(columnNumber).Width = 10;

                            // rows.Add(worksheet.Row(rowNumber));
                        }
                    }
                }

                // foreach (var r in rows)
                // {
                //    r.Height = 50;
                // }
            }

            package.SaveAs(new FileInfo(tempFile));
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }

        return tempFile;
    }

    public async Task<string> AllTemplateExcel(DateTime? fromDate, DateTime? toDate, string? season, string? dept_code, string? class_code, string? subclass_code, string templateType, long[]? roleIds)
    {
        using var connection = this.GetConnection();
        var seasonData = season;
        var startDates = fromDate ?? DateTime.MinValue;
        var endDates = toDate ?? DateTime.MaxValue;
        var formattedStartDate = startDates.ToString("yyyy/MM/dd");
        var formattedEndDate = endDates.ToString("yyyy/MM/dd");
        var tempFile = Path.GetTempFileName();

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Sheet1");

        // var query = "select distinct cd.uid, cd.*,TRIM(BOTH ' ' FROM CONCAT(u.planogram, ' ', u.familyname)) AS Planogram_Family_Name,sc.name as Supplier_Name, uom.name as UOM, s.name as Season_Code,pp.name as Product_Purpose,pg.name as Price_Grouping,cd.product_cbm as Product_CBM from vw_columndata AS cd inner join vw_uid AS d ON cd.uid = d.uid " +
        //   "inner join season s on cd.season_code = s.code " +
        //   "inner join uidinitiation u on cd.planogram_family_code  = u.planocode " +
        //   "inner join productpurpose pp on cd.product_purpose = pp.code " +
        //   "inner join pricegrouping pg on cd.price_grouping  = pg.code " +
        //   "LEFT JOIN mtrsuppliercode sc ON cd.rms_supplier_code = sc.code::text " +
        //   "LEFT JOIN mtrsetpcs uom ON cd.UOM = uom.code::text LEFT JOIN product pro ON cd.uid = pro.uid ";
        var query = "select cd.*,planogram_family_name AS Planogram_Family_Name,supplier_name as Supplier_Name,uom as UOM,season_name as Season_Code, product_purpose as Product_Purpose, " +
                    "price_grouping as Price_Grouping,product_cbm as Product_CBM from vw_product_details cd ";

        // var query = "select cd.*,ui.planogram_family_name AS Planogram_Family_Name,cd.rms_supplier_name as Supplier_Name, cd.uom_name as UOM, ui.season_name as Season_Name, ui.productpurpose as Product_Purpose,ui.pricegrouping as Price_Grouping,cd.product_cbm as Product_CBM from vw_columndata as cd " +
        //       "INNER JOIN vw_uid ui ON ui.uid = cd.uid ";
        bool isAnyConditionAdded = false;
        if (fromDate != null || toDate != null)
        {
            query += $" WHERE to_char(cd.datecreated::date::timestamp with time zone, 'yyyy/MM/dd'::text) BETWEEN '{formattedStartDate}' AND '{formattedEndDate}'";
            isAnyConditionAdded = true;
        }

        if (roleIds != null && !roleIds.Contains(1))
        {
            query += isAnyConditionAdded ? " AND " : " WHERE ";
            query += $" isactive = true ";
            isAnyConditionAdded = true;
        }

        if ((season != null && season.Length > 0) ||
            (dept_code != null && dept_code.Length > 0) ||
            (class_code != null && class_code.Length > 0) ||
            (subclass_code != null && subclass_code.Length > 0))
        {
            query += isAnyConditionAdded ? " AND (" : " WHERE (";

            if (season != null && season.Length > 0)
            {
                string[] elements = season.Split(',');
                var result = elements.Select(element => $"'{element}'");
                string seasonValues = $"{string.Join(",", result)}";
                query += $"season_name IN ({seasonValues})";
                isAnyConditionAdded = true;
            }

            if (dept_code != null && dept_code.Length > 0)
            {
                query += (season != null && season.Length > 0) ? " AND " : string.Empty;
                string[] elements = dept_code.Split(',');
                var result = elements.Select(element => $"'{element}'");
                string deptValues = $"{string.Join(",", result)}";
                query += $"dept_name IN ({deptValues})";
                isAnyConditionAdded = true;
            }

            if (class_code != null && class_code.Length > 0)
            {
                query += (season != null && season.Length > 0) || (dept_code != null && dept_code.Length > 0) ? " AND " : string.Empty;
                string[] elements = class_code.Split(',');
                var result = elements.Select(element => $"'{element}'");
                string classValues = $"{string.Join(",", result)}";
                query += $"class_name IN ({classValues})";
                isAnyConditionAdded = true;
            }

            if (subclass_code != null && subclass_code.Length > 0)
            {
                query += (season != null && season.Length > 0) || (dept_code != null && dept_code.Length > 0) || (class_code != null && class_code.Length > 0) ? " AND " : string.Empty;
                string[] elements = subclass_code.Split(',');
                var result = elements.Select(element => $"'{element}'");
                string subclassValues = $"{string.Join(",", result)}";
                query += $"subclass_name IN ({subclassValues})";
                isAnyConditionAdded = true;
            }

            query += ")";
        }

        if (query.ToString().Contains("WHERE"))
        {
            query += "AND ";
        }
        else
        {
            query += " WHERE ";
        }

        query += " cd.uid IS NOT NULL  order by cd.datecreated desc ";
        IEnumerable<string> mergedColumns;
        var tempid = 12;
        var tempid1 = 6;
        switch (templateType)
        {
            case "4":
                mergedColumns = await connection.QueryAsync<string>($"SELECT buyercolumndata FROM (SELECT jsonb_array_elements(jsonvalue)->>'text' AS buyercolumndata FROM appsettings WHERE value = '{templateType}' AND jsonvalue IS NOT NULL ) AS subquery WHERE buyercolumndata != 'text'");
                break;
            case "6,12":
                mergedColumns = await connection.QueryAsync<string>($"SELECT DISTINCT name,template_order, \"order\" FROM (SELECT name, templateattribute.order, CASE WHEN templateattribute.templateid = '{tempid}' THEN 1  ELSE 0  END as template_order FROM templateattribute JOIN productattribute ON templateattribute.attributeid = productattribute.id WHERE templateattribute.templateid = '{tempid1}' UNION ALL SELECT name, templateattribute.order, CASE  WHEN templateattribute.templateid = '{tempid}' THEN 1 ELSE 0  END as template_order FROM templateattribute JOIN productattribute ON templateattribute.attributeid = productattribute.id WHERE templateattribute.templateid = '{tempid}' AND templateattribute.attributeid NOT IN (SELECT attributeid FROM templateattribute WHERE templateid = '{tempid1}' )) AS subquery ORDER BY template_order, \"order\" ");
                break;
            case "8":
                mergedColumns = await connection.QueryAsync<string>($"SELECT distinct productattribute.name,templateattribute.order FROM templateattribute JOIN productattribute ON templateattribute.attributeid = productattribute.id WHERE templateid={templateType} ORDER BY templateattribute.order");
                break;
            case "10":
                mergedColumns = await connection.QueryAsync<string>($"SELECT distinct productattribute.name,templateattribute.order FROM templateattribute JOIN productattribute ON templateattribute.attributeid = productattribute.id WHERE templateid={templateType} ORDER BY templateattribute.order");
                break;
            default:
                throw new ArgumentException("Invalid templateType");
        }

        var products = await connection.QueryAsync<AllAttributesData>(query);
        var product = await connection.QueryAsync<Product>("SELECT * FROM product WHERE pictureid <> 0");
        var uploadPicture = await connection.QueryAsync<UploadPicture>("SELECT up.imageurl, p.uid as Name FROM product p JOIN uploadpicture up ON p.pictureid = up.id WHERE p.pictureid <> 0");

        try
        {
            if (mergedColumns.Any())
            {
                var r = 1;
                var cH = 1;
                var headerColor = System.Drawing.Color.SteelBlue;
                var textColor = System.Drawing.Color.White;

                var columnArray = mergedColumns.ToArray();
                for (int i = 0; i < columnArray.Length; i++)
                {
                    var columnName = columnArray[i];
                    var cell = worksheet.Cells[r, cH]; // Reference to the cell

                    cell.Value = columnName;
                    cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;

                    // Set the cell style
                    cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(headerColor);
                    cell.Style.Font.Color.SetColor(textColor);
                    cell.Style.Font.Bold = true;

                    // Set column width
                    worksheet.Column(cH).Width = 15;

                    cH++;
                }

                int dataRow = 1;
                var picturesToAdd = new List<(int Row, int Column, string ImageUrl)>();
                var productList = products.Select((product, index) => new { Product = product, RowIndex = index + 2 }).ToList();
                Parallel.ForEach(productList, record =>
                {
                    var product = record.Product;
                    var rowNumber = record.RowIndex;
                    var c = 1;
                    var pictures = uploadPicture.Where(a => a.Name == product.Uid).Select(a => a.ImageUrl).FirstOrDefault();

                    if (templateType == "4")
                    {
                        if (mergedColumns.Contains("Uid"))
                        {
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = product.Uid.ToString();
                            }
                        }

                        if (mergedColumns.Contains("Planogram Family Name"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Planogram Family Name");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("RMS Supplier Code"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("RMS Supplier Code");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("RMS Season Code"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("RMS Season Code");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("RMS Season Name"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("RMS Season Name");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Product Picture"))
                        {
                            if (!string.IsNullOrEmpty(pictures))
                            {
                                picturesToAdd.Add((rowNumber, c++, pictures));
                            }
                            else
                            {
                                lock (worksheetLock)
                                {
                                    worksheet.Cells[rowNumber, c++].Value = string.Empty;
                                }
                            }
                        }

                        if (mergedColumns.Contains("Brand Type"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Brand Type");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Brand Code"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Brand Code");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                var cell = worksheet.Cells[rowNumber, c++];
                                cell.Value = columnValue;
                                cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                            }
                        }

                        if (mergedColumns.Contains("Launch Date"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Launch Date");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();

                            DateTime parsedDate;
                            string formattedDate = null;
                            string[] dateFormats = { "dd/MM/yyyy", "dd-MM-yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "dd MMM yyyy", "yyyy-MM-ddTHH:mm:ss" };

                            if (DateTime.TryParseExact(columnValue, dateFormats, null, System.Globalization.DateTimeStyles.None, out parsedDate))
                            {
                                formattedDate = parsedDate.ToString("dd/MM/yyyy");
                            }
                            else if (DateTime.TryParse(columnValue, out parsedDate))
                            {
                                formattedDate = parsedDate.ToString("dd/MM/yyyy");
                            }

                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = formattedDate ?? columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Purchase Type"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Purchase Type");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Purchase Type Desc"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Purchase Type Desc");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Country Of Origin"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Country Of Origin");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Currency"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Currency");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Product Vendor Code"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Product Vendor Code");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Style Name"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Style Name");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Item Desc"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Item Desc");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Specific Product Name"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Specific Product Name");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Net Qty As Per MRP Label"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Net Qty As Per MRP Label");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("UOM"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("UOM");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Pack Contents"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Pack Contents");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Initital Product Size (L) (CM)"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Initital Product Size (L) (CM)");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Initital Product Size (W) (CM)"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Initital Product Size (W) (CM)");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Initital Product Size (H) (CM)"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Initital Product Size (H) (CM)");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Color Code"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Color Code");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Color Desc"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Color Desc");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("MRP"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("MRP");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Unit Sale Price"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Unit Sale Price");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("EXW FOB Cost"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("EXW FOB Cost");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("HSN Code"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("HSN Code");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("GST %"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("GST %");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Tax Category"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Tax Category");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Planned Discount"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Planned Discount");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("BAU Discount"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("BAU Discount");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("RRP"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("RRP");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Inner Carton Code"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Inner Carton Code");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Inner Carton Desc"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Inner Carton Desc");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Master Carton Code"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Master Carton Code");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Master Carton Desc"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Master Carton Desc");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Ref SKU 1"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Ref SKU 1");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Ref SKU 2"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Ref SKU 2");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Ref SKU Remarks"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Ref SKU Remarks");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("MOQ"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("MOQ");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("MOQ Remark 1"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("MOQ Remark 1");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("MOQ Remark 2"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("MOQ Remark 2");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Lead Time Days"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Lead Time Days");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Product Validation Code"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Product Validation Code");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Store Category"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Store Category");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Product Grouping"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Product Grouping");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Item Type code"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Item Type code");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Item Type Desc"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Item Type Desc");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("PLC Code"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("PLC Code");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("PLC Desc"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("PLC Desc");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Product Classification code"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Product Classification code");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Product Classification Desc"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Product Classification Desc");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("No Of Stores Planned"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("No Of Stores Planned");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Plotting Remark"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Plotting Remark");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Planogram Fitment"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Planogram Fitment");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Diff Group2 Val"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Diff Group2 Val");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Made Of Value"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Made Of Value");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Cost Based MRP Ind"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Cost Based MRP Ind");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Margin %"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Margin %");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Margin Applied On"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Margin Applied On");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Brand EAN code"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Brand EAN code");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }
                    }
                    else if (templateType == "6,12")
                    {
                        if (mergedColumns.Contains("Uid"))
                        {
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = product.Uid.ToString();
                            }
                        }

                        if (mergedColumns.Contains("Planogram Family Name"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Planogram Family Name");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Sub Range Collection Name"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Sub Range Collection Name");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Customer Centric Size"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Customer Centric Size");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Product Type 1"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Product Type 1");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Product Sub Type 1"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Product Sub Type 1");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Product Type 2"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Product Type 2");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Product Sub Type 2"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Product Sub Type 1");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Primary Selling Feature"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Primary Selling Feature");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Primary Selling Feature Value"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Primary Selling Feature Value");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Secondary Selling Feature"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Secondary Selling Feature");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Secondary Selling Feature Value"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Secondary Selling Feature Value");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Tertiary Selling Feature"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Tertiary Selling Feature");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Tertiary Selling Feature Value"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Tertiary Selling Feature Value");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Primary Material"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Primary Material");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Material Description"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Material Description");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Ideal For"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Ideal For");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Aesthetic"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Aesthetic");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Item Shape"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Item Shape");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Process Techniques"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Process Techniques");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Is Fragile"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Is Fragile");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Legal Disclaimer"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Legal Disclaimer");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Warranty"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Warranty");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Buyers Thought"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Buyers Thought");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Selling Tips"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Selling Tips");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Loipl Shoot Required"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Loipl Shoot Required");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Ref Sku For Floor Display"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Ref Sku For Floor Display");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("First Not Before Date"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("First Not Before Date");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();

                            DateTime parsedDate;
                            string formattedDate = null;
                            string[] dateFormats = { "dd/MM/yyyy", "dd-MM-yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "dd MMM yyyy", "yyyy-MM-ddTHH:mm:ss" };

                            if (DateTime.TryParseExact(columnValue, dateFormats, null, System.Globalization.DateTimeStyles.None, out parsedDate))
                            {
                                formattedDate = parsedDate.ToString("dd/MM/yyyy");
                            }
                            else if (DateTime.TryParse(columnValue, out parsedDate))
                            {
                                formattedDate = parsedDate.ToString("dd/MM/yyyy");
                            }

                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = formattedDate ?? columnValue;
                            }
                        }

                        if (mergedColumns.Contains("FAB Based on Material"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("FAB Based on Material");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("FAB Based on Finish Process"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("FAB Based on Finish Process");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Utility Functional Usage USP"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Utility Functional Usage USP");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }
                    }
                    else if (templateType == "8")
                    {
                        if (mergedColumns.Contains("Uid"))
                        {
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = product.Uid.ToString();
                            }
                        }

                        if (mergedColumns.Contains("Planogram Family Name"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Planogram Family Name");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Care Instruction 1 Cleaning"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Care Instruction 1 Cleaning");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Care Instruction 2 Caring Dos"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Care Instruction 2 Caring Dos");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Care Instruction 3 Caring Don't"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Care Instruction 3 Caring Don't");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Care Instruction 4 Storage Instructions"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Care Instruction 4 Storage Instructions");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Care Instruction 5"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Care Instruction 5");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Care Instruction 6"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Care Instruction 6");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Safety Instruction"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Safety Instruction");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Packaging Type"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Packaging Type");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Product Dimension L (CM)"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Product Dimension L (CM)");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Product Dimension W (CM)"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Product Dimension W (CM)");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Product Dimension H (CM)"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Product Dimension H (CM)");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Product Capacity Volume (ML)"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Product Capacity Volume (ML)");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Product Net Weight (GMS)"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Product Net Weight (GMS)");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Product Gross Weight (GMS)"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Product Gross Weight (GMS)");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Packaging Product size (L)"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Packaging Product size (L)");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Packaging Product size (W)"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Packaging Product size (W)");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Packaging Product size (H)"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Packaging Product size (H)");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Inner Pack Dimensions L (CM)"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Inner Pack Dimensions L (CM)");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Inner Pack Dimensions W (CM)"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Inner Pack Dimensions W (CM)");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Inner Pack Dimensions H (CM)"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Inner Pack Dimensions H (CM)");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Net Weight Inner Carton"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Net Weight Inner Carton");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Inner Pack Gross Weight"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Inner Pack Gross Weight");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Master Carton Dimensions L (CM)"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Master Carton Dimensions L (CM)");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Master Carton Dimensions W (CM)"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Master Carton Dimensions W (CM)");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Master Carton Dimensions H (CM)"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Master Carton Dimensions H (CM)");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Assembly Required"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Assembly Required");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Primary Packaging"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Primary Packaging");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }
                    }
                    else if (templateType == "10")
                    {
                        if (mergedColumns.Contains("Uid"))
                        {
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = product.Uid.ToString();
                            }
                        }

                        if (mergedColumns.Contains("Planogram Family Name"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Planogram Family Name");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("Packaging Image"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("Packaging Image");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("PD Packaging Type"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("PD Packaging Type");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("PD Packaging Size L (CM)"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("PD Packaging Size L (CM)");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("PD Packaging Size W (CM)"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("PD Packaging Size W (CM)");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }

                        if (mergedColumns.Contains("PD Packaging Size H (CM)"))
                        {
                            var propertyName = ConvertColumnNameToPropertyName("PD Packaging Size H (CM)");
                            var propertyInfo = product.GetType().GetProperty(propertyName);
                            var columnValue = propertyInfo?.GetValue(product)?.ToString();
                            lock (worksheetLock)
                            {
                                worksheet.Cells[rowNumber, c++].Value = columnValue;
                            }
                        }
                    }
                });

                // var rows = new List<ExcelRow>();
                // package.DoAdjustDrawings = false;
                var rowsWithPictures = picturesToAdd.Select(p => p.Row).Distinct();
                foreach (var rowNumber in rowsWithPictures)
                {
                    worksheet.Row(rowNumber).Height = 50;
                }

                foreach (var pictureInfo in picturesToAdd)
                {
                    var (rowNumber, columnNumber, imageUrl) = pictureInfo;
                    var filePath = _blobRepo.DownloadFile(imageUrl).Result;

                    if (File.Exists(filePath))
                    {
                        using (var imageStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                        {
                            var image = Image.FromStream(imageStream);
                            var picture = worksheet.Drawings.AddPicture(Guid.NewGuid().ToString(), image);
                            picture.SetPosition(rowNumber - 1, 0, columnNumber - 1, 0);
                            picture.SetSize(60, 60);
                            worksheet.Column(columnNumber).Width = 10;

                            // rows.Add(worksheet.Row(rowNumber));
                        }
                    }
                }

                // foreach (var r in rows)
                // {
                //    r.Height = 50;
                // }
            }

            package.SaveAs(new FileInfo(tempFile));
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }

        return tempFile;
    }

    public async Task<List<dynamic>> SearchUidProducts(UidExportModel uidExport)
    {
        try
        {
            using var connection = this.GetConnection();
            var query = "select uid,TO_CHAR(datecreated, 'MM/DD/YYYY') AS datecreated,dept_name AS DeptCode,Planogram_Family_Name AS Planogram_FamilyCode," +
                        "div_name AS DivisionCode,class_name AS ClassCode,subclass_name AS SubclassCode,group_name AS GroupCode,season_name AS SeasonCode,pricegrouping AS PriceGrouping,productpurpose AS ProductPurpose,target_mrp,product_desc,pictureid, isactive AS IsActive from vw_uid";
            var products = await connection.QueryAsync<dynamic>(query);
            return products.ToList();
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }

    public async Task UpdateUidIsactive(string uid, bool isactive)
    {
        try
        {
            using var connection = this.GetConnection();
            if (isactive is true)
            {
                await connection.ExecuteAsync("UPDATE product SET isactive = @isactive, columndata = jsonb_set(columndata::jsonb, '{UID Status}', '\"Active\"') WHERE uid = @uid", new { isactive, uid });
            }
            else
            {
                await connection.ExecuteAsync("UPDATE product SET isactive = @isactive, columndata = jsonb_set(columndata::jsonb, '{UID Status}', '\"In-Active\"') WHERE uid = @uid", new { isactive, uid });
            }
        }
        catch
        {
            throw new Exception($"Status not Updated.");
        }
    }

    public async Task<List<MerchandiseHierarchy>> GetHierarchicalMasters()
    {
        using var connection = this.GetConnection();
        var query = await connection.QueryAsync<MerchandiseHierarchy>("select distinct dept,dept_name,class,class_name,subclass,sub_name,division,div_name,group_no,group_name from merchandise_hierarchy");
        return query.ToList();
    }

    public async Task<List<ProductdeptMaster>> GetProductdeptMasters()
    {
        using var connection = this.GetConnection();
        var query = await connection.QueryAsync<ProductdeptMaster>("select distinct name ,code ,id ,division from productdepartment join merchandise_hierarchy on dept::text=productdepartment.code");
        return query.ToList();
    }

    public async Task<List<RmsStaging>> GetRms()
    {
        using var connection = this.GetConnection();
        var query = await connection.QueryAsync<RmsStaging>("select * from rmsstaging");
        return query.ToList();
    }

    public async Task<List<ProductPurpose>> GetProductPurpose()
    {
        using var connection = this.GetConnection();
        var query = await connection.QueryAsync<ProductPurpose>("select * from productpurpose");
        return query.ToList();
    }

    public async Task<List<PriceGrouping>> GetPriceGrouping()
    {
        using var connection = this.GetConnection();
        var query = await connection.QueryAsync<PriceGrouping>("select * from pricegrouping");
        return query.ToList();
    }

    public async Task<string> DownloadImage(string imageUrl)
    {
        try
        {
            using var connection = this.GetConnection();
            var image = imageUrl.ToLower();
            var existingId = connection.QueryFirstOrDefault<UploadPicture>("SELECT * FROM uploadpicture WHERE lower(name) = @imageurl", new { imageurl = image });
            string downloadedData = await _blobRepo.DownloadFile(existingId.ImageUrl);
            if (!string.IsNullOrEmpty(downloadedData))
            {
                return downloadedData;
            }
            else
            {
                throw new Exception("Downloaded data is empty or null.");
            }
        }
        catch (HttpRequestException)
        {
            throw;
        }
    }

    public async Task<string> GetImageName(string imageUrl)
    {
        try
        {
            using var connection = this.GetConnection();
            var image = imageUrl.ToLower();
            var existingId = connection.QueryFirstOrDefault<UploadPicture>("SELECT * FROM uploadpicture WHERE lower(name) = @imageurl", new { imageurl = image });
            if (existingId != null)
            {
                return existingId.Name;
            }
            else
            {
                throw new Exception("Image not exist.");
            }
        }
        catch (HttpRequestException)
        {
            throw;
        }
    }

    public async Task<List<dynamic>> TotalCountViewColumnData()
    {
        using var connection = this.GetConnection();
        var query = $"select count(*) as totalcountVW from vw_columndata";
        var products = await connection.QueryAsync(query);
        return products.ToList();
    }

    public async Task<List<dynamic>> TotalCountJsonFormatData()
    {
        using var connection = this.GetConnection();
        var query = $"select count(*) as totalcountJson from product";
        var products = await connection.QueryAsync(query);
        return products.ToList();
    }

    public async Task<List<dynamic>> UidColumndataCheck(string uid)
    {
        if (string.IsNullOrWhiteSpace(uid))
        {
            throw new ArgumentException("UID cannot be null or whitespace.", nameof(uid));
        }

        using var connection = this.GetConnection();

        var uidExists = await connection.ExecuteScalarAsync<bool>($"SELECT COUNT(1) FROM product WHERE uid = '{uid.Trim()}'");
        if (!uidExists)
        {
            throw new Exception($"The  UID '{uid}' is invalid.");
        }

        var product = await connection.QueryAsync<JsonColumndata>($"SELECT columndata FROM product WHERE uid = '{uid.Trim()}'");
        var productData = product.FirstOrDefault();

        var columndata = await connection.QueryAsync<UidComparisonModel>($"SELECT * FROM vw_columndata WHERE uid = '{uid.Trim()}'");
        var columndataList = columndata.ToList();
        var columnDataMapping = new Dictionary<string, string>();

        if (productData != null)
        {
            foreach (var kvp in productData.ColumnData)
            {
                var fieldName = kvp.Key;
                var fieldValue = kvp.Value.ToString();

                if (fieldName == "GST %" || fieldName == "Margin %")
                {
                    var underscoreFieldName = fieldName.Replace("%", string.Empty);
                    columnDataMapping[underscoreFieldName.Trim()] = fieldName;
                }
                else if (fieldName == "Care_Instruction_3_Caring_Don't")
                {
                    var underscoreFieldName = fieldName.Replace("'", string.Empty);
                    columnDataMapping[underscoreFieldName.Trim()] = fieldName;
                }
                else if (fieldName.Contains("(") && fieldName.Contains(")"))
                {
                    int startIndex = fieldName.IndexOf("(");
                    int endIndex = fieldName.LastIndexOf(")");
                    if (startIndex < endIndex)
                    {
                        string textInParentheses = fieldName.Substring(startIndex + 1, endIndex - startIndex - 1);
                        var underscoreFieldName = fieldName.Replace("(" + textInParentheses + ")", textInParentheses.Replace("(", string.Empty).Replace(")", string.Empty));
                        var lastName = underscoreFieldName.Replace(" ", "_");
                        columnDataMapping[lastName] = fieldName;
                    }
                }
                else
                {
                    var underscoreFieldName = fieldName.Replace("/", "_").Replace(" ", "_");
                    columnDataMapping[underscoreFieldName] = fieldName;
                }
            }

            var combinedData = new List<dynamic>();
            var errorMessages = new List<string>();

            foreach (var kvp in productData.ColumnData)
            {
                var fieldName = kvp.Key;
                var fieldValue = kvp.Value.ToString();

                string underscoreFieldName;
                if (fieldName.Contains("/"))
                {
                    underscoreFieldName = fieldName.Replace("/", "_").Replace(" ", "_");
                }
                else if (fieldName.Contains("%"))
                {
                    underscoreFieldName = fieldName.Replace("%", string.Empty).Trim();
                }
                else if (fieldName.Contains("'"))
                {
                    underscoreFieldName = fieldName.Replace("'", string.Empty);
                }
                else if (fieldName.Contains("(") && fieldName.Contains(")"))
                {
                    int startIndex = fieldName.IndexOf("(");
                    int endIndex = fieldName.LastIndexOf(")");

                    if (startIndex < endIndex)
                    {
                        string textInParentheses = fieldName.Substring(startIndex + 1, endIndex - startIndex - 1);
                        var lastName = fieldName.Replace("(" + textInParentheses + ")", textInParentheses.Replace("(", string.Empty).Replace(")", string.Empty));
                        underscoreFieldName = lastName.Replace(" ", "_");
                    }
                    else
                    {
                        underscoreFieldName = fieldName.Replace(" ", "_");
                    }
                }
                else
                {
                    underscoreFieldName = fieldName.Replace(" ", "_");
                }

                if (columnDataMapping.ContainsKey(underscoreFieldName))
                {
                    var originalFieldName = columnDataMapping[underscoreFieldName];

                    foreach (var item in columndataList)
                    {
                        var property = item.GetType().GetProperty(underscoreFieldName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                        if (property != null)
                        {
                            var itemValue = property.GetValue(item)?.ToString();

                            if (itemValue != fieldValue)
                            {
                                errorMessages.Add($"Data is Mismatched in '{originalFieldName}'");
                            }
                        }
                        else
                        {
                            errorMessages.Add($"Field '{fieldName}' not found in vw_columndata.");
                        }
                    }
                }
                else
                {
                    errorMessages.Add($"The given key '{fieldName}' was not present in the dictionary.");
                }
            }

            combinedData.Add(productData);
            combinedData.AddRange(columndataList);

            if (errorMessages.Any())
            {
                combinedData.AddRange(errorMessages);
            }

            return combinedData;
        }

        return null;
    }

    public async Task<List<ProductDExcel>> UidComparison(string uid)
    {
        using var connection = this.GetConnection();
        var product = await connection.QueryAsync<ProductDExcel>($"select columndata from product where uid = '{uid}'");
        return product.ToList();
    }

    public async Task<byte[]> AttributesExportExcel(bool? isActive, string? divisionId)
    {
        using var connection = this.GetConnection();
        var result = new List<ExportAttributesModel>();
        string cleanedInput = divisionId.Replace("'", " ");
        string[] numberStrings = cleanedInput.Split(',');
        long[] numbers = numberStrings.Select(long.Parse).ToArray();
        string numberList = string.Join(",", numbers);
        if (isActive is not null)
        {
            result = (await connection.QueryAsync<ExportAttributesModel>($"SELECT p.name AS Attribute,d.name AS Division, CASE WHEN datatype = 0 THEN 'Text' " +
                                                                    $"WHEN datatype = 10 THEN 'Number' " +
                                                                    $"WHEN datatype = 20 THEN 'Date time' " +
                                                                    $"WHEN datatype = 30 THEN 'Look up' " +
                                                                    $"WHEN datatype = 40 THEN 'Boolean' " +
                                                                    $"ELSE CAST(datatype AS VARCHAR) " +
                                                                    $"END AS datatype, p.isactive, divisionid, " +
                                                                    $"CASE WHEN isrequired = true THEN 'Mandatory' WHEN isrequired = false THEN 'Optional' " +
                                                                    $"END AS FieldType FROM productattribute p JOIN division d ON d.id = p.divisionid " +
                                                                    $"WHERE divisionid IN ({numberList})  and p.isactive = '{isActive}' order by p.name asc")).ToList();
        }
        else
        {
            result = (await connection.QueryAsync<ExportAttributesModel>($"SELECT p.name AS Attribute,d.name AS Division, CASE WHEN datatype = 0 THEN 'Text' " +
                                                                    $"WHEN datatype = 10 THEN 'Number' " +
                                                                    $"WHEN datatype = 20 THEN 'Date time' " +
                                                                    $"WHEN datatype = 30 THEN 'Look up' " +
                                                                    $"WHEN datatype = 40 THEN 'Boolean' " +
                                                                    $"ELSE CAST(datatype AS VARCHAR) " +
                                                                    $"END AS datatype, p.isactive, divisionid, " +
                                                                    $"CASE WHEN isrequired = true THEN 'Mandatory' WHEN isrequired = false THEN 'Optional' " +
                                                                    $"END AS FieldType FROM productattribute p JOIN division d ON d.id = p.divisionid " +
                                                                    $"WHERE divisionid IN ({numberList}) order by p.name asc")).ToList();
        }

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Attributes Records");

        if (result.Any())
        {
            var headers = new[] { "Attribute", "DataType", "FieldType", "Division", "IsActive" };
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
                worksheet.Cells[row, 1].Value = record.Attribute;
                worksheet.Cells[row, 2].Value = record.Datatype;
                worksheet.Cells[row, 3].Value = record.FieldType;
                worksheet.Cells[row, 4].Value = record.Division;
                worksheet.Cells[row, 5].Value = record.IsActive;

                worksheet.Cells[row, 1, row, 15].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;

                for (int col = 1; col <= 5; col++)
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

    public async Task DeleteAttribute(IEnumerable<long> ids)
    {
        using var connection = this.GetConnection();
        foreach (var id in ids)
        {
            await connection.ExecuteAsync($"Delete from productattribute where id = {id}");
        }
    }

    private DataTable ToDataTable<T>(List<T> items)
    {
        DataTable dataTable = new DataTable(typeof(T).Name);
        PropertyInfo[] props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (PropertyInfo prop in props)
        {
            dataTable.Columns.Add(prop.Name);
        }

        foreach (T item in items)
        {
            var values = new object[props.Length];
            for (int i = 0; i < props.Length; i++)
            {
                values[i] = props[i].GetValue(item, null);
            }

            dataTable.Rows.Add(values);
        }

        return dataTable;
    }

    private string ConvertColumnNameToPropertyName(string columnName)
    {
        columnName = columnName.Replace(" ", "_");

        columnName = columnName.Replace("/", "_");

        if (columnName == "Margin_%" || columnName == "GST_%")
        {
            columnName = columnName.Replace("_", " ");
            columnName = columnName.Replace("%", string.Empty);
        }

        if (columnName.Contains("(") && columnName.Contains(")"))
        {
            int startIndex = columnName.IndexOf("(");
            int endIndex = columnName.LastIndexOf(")");
            if (startIndex < endIndex)
            {
                string textInParentheses = columnName.Substring(startIndex + 1, endIndex - startIndex - 1);
                columnName = columnName.Replace("(" + textInParentheses + ")", textInParentheses.Replace("(", string.Empty).Replace(")", string.Empty));
            }
        }

        if (columnName == "Care_Instruction_3_Caring_Don't")
        {
            columnName = columnName.Replace("'", string.Empty);
        }

        columnName = columnName.Trim();
        return columnName;
    }
}