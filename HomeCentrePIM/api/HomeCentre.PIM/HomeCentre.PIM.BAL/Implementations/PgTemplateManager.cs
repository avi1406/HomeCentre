using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Dapper;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Office2010.PowerPoint;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ExcelDataReader;
using FluentEmail.Core;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.BAL.Model.Geo;
using HomeCentre.PIM.BAL.Model.Template;
using HomeCentre.PIM.BAL.Model.Uid;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.DomainModel;
using HomeCentre.PIM.DAL.Master;
using HomeCentre.PIM.DAL.Staging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NetBox.Extensions;
using Newtonsoft.Json;
using Npgsql;
using OfficeOpenXml;
using OfficeOpenXml.DataValidation;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using OfficeOpenXml.Style;
using OfficeOpenXml.Table;
using Org.BouncyCastle.Asn1.Cms;

namespace HomeCentre.PIM.BAL.Implementations;

public class PgTemplateManager : PgEntityAction<Template>, ITemplateManager
{
    private readonly IDbCache _cache;
    private readonly IBlobRepo _blobRepo;
    private readonly IUploadManager _uploadManager;
    private readonly IProductManager _productManager;

    public PgTemplateManager(IUserContext userContext, IDbConnectionFactory connectionFactory,
        IConfiguration configuration, IProductManager productManager, IUploadManager uploadManager, IBlobRepo blobRepo,
        IDbCache cache)
        : base(userContext, connectionFactory, configuration)
    {
        _cache = cache;
        _blobRepo = blobRepo;
        _uploadManager = uploadManager;
        _productManager = productManager;
    }

    public async Task AddOrUpdateTemplate(Template template)
    {
        var action = this.GetAction<TemplateAttribute>();
        var templateRole = this.GetAction<TemplateRole>();
        var templates = await _cache.GetTemplates();
        if (templates.Any(t => t.Name.ToLower() == template.Name.ToLower() && t.DivisionId == template.DivisionId && t.Id != template.Id))
        {
            throw new HandledException($"Template '{template.Name}' Already Exists");
        }

        if (template.Id == 0)
        {
            template.TemplateStatusId = 1; // draft
        }

        var roleIds = template.RoleIds;
        template = await this.AddOrUpdate(template);
        template.RoleIds = roleIds;
        if (template.IsActive == false)
        {
            template.IsActive = false;
        }

        var existingRoles = await templateRole.GetAll("where templateid = @templateId", new
        {
            templateId = template.Id
        });

        foreach (var existingRole in existingRoles)
        {
            if (!template.RoleIds.Contains(existingRole.RoleId))
            {
                await templateRole.Remove(existingRole.Id);
            }
            else
            {
                template.RoleIds = template.RoleIds.Except(new[] { existingRole.RoleId }).ToArray();
            }
        }

        foreach (var templateRoleId in template.RoleIds)
        {
            await templateRole.AddOrUpdate(new TemplateRole
            {
                TemplateId = template.Id,
                RoleId = templateRoleId
            });
        }

        var uidAttribute =
            (await this._cache.GetAttributes(template.DivisionId.GetValueOrDefault())).FirstOrDefault(x =>
                x.Name.ToLower() == "uid" && template.DivisionId == x.DivisionId);
        if (uidAttribute != null && template.Id == 0)
        {
            await action.AddOrUpdate(new TemplateAttribute
            {
                AttributeId = uidAttribute.Id,
                TemplateId = template.Id
            });
        }

        await this._cache.Clear();
    }

    public async Task DeactivateTemplates(long[] ids)
    {
        await this.Deactivate(ids);
        await this._cache.Clear();
    }

    public async Task<SearchResult<Template>> SearchTemplates(string key, int pageNumber, int pageCount,
        long? departmentId, bool isActive)
    {
        var dynParams = new DynamicParameters();
        dynParams.Add("departmentId", departmentId);
        return await this.Search(key, pageNumber, pageCount, isActive: isActive);
    }

    public async Task<IEnumerable<TemplateAttributeModel>> ListTemplateAttributes(long templateId)
    {
        using var connection = this.GetConnection();
        return await connection.QueryAsync<TemplateAttributeModel>(
            "select pa.name, pa.datatype, t.id as templateattributeid, t.templateid, t.attributeid,pa.isdatalist from productattribute pa join templateattribute t on pa.id = t.attributeid where t.templateid = @templateId order by \"order\"",
            new
            {
                templateId
            });
    }

    public async Task DeactivateAttribute(long[] ids)
    {
        using var connection = this.GetConnection();
        var action = this.GetAction<ProductAttribute>();
        await action.Deactivate(ids);
        await connection.ExecuteAsync("delete from templateattribute where attributeid = any(@ids)", new { ids });
    }

    // public async Task<byte[]> DownloadTemplate(long templateId, string format)
    // {
    //    using var connection = this.GetConnection();
    //    var attributes = await this.ListTemplateAttributes(templateId);
    //    var dt = new DataTable();
    //    dt.TableName = "Data";
    //    List<string> fiveAttributes = new List<string> { "Brand Type", "Currency", "Item Type code", "Loipl Shoot Required", "Primary Packaging" };

    // var isList = false;
    //    var col = new DataColumn();
    //    var listStringInputs = new List<string[]>();
    //    var listCells = new List<int>();
    //    var newRow = dt.NewRow();
    //    var index = 0;

    // var leadDropdownValues = await connection.QueryAsync<MtrLeadTime>("select name from mtrleadtime");
    //    var leadNames = leadDropdownValues.Select(item => item.Name).ToArray();

    // var itemTypeCodeDropdownValues = await connection.QueryAsync<MtrItemTypeCode>("select code from mtritemtypecode");
    //    var itemtype = itemTypeCodeDropdownValues.Select(item => item.Code.ToString()).ToArray();

    // var itemshape = await connection.QueryAsync<MtrItemShape>("select name from mtritemshape");
    //    var itemshapNames = itemshape.Select(item => item.Name).ToArray();

    // var currency = await connection.QueryAsync<MtrCurrency>("select name from mtrcurrency");
    //    var currencyNames = currency.Select(item => item.Name).ToArray();

    // var brandType = await connection.QueryAsync<MtrBrandType>("select name from mtrbrandtype");
    //    var brandTypeNames = brandType.Select(item => item.Name).ToArray();

    // Dictionary<string, List<string>> columnDropdownValues = new Dictionary<string, List<string>>
    //    {
    //        { "Brand Type", new List<string>(brandTypeNames) },
    //        { "Currency", new List<string>(currencyNames) },
    //        { "Item Type code", new List<string>(itemtype) },
    //        { "Loipl Shoot Required", new List<string> { "Yes", "No" } },
    //        { "Primary Packaging", new List<string> { "Yes", "No" } },
    //        { "Lead Time Days", new List<string>(leadNames) },
    //        {"Item Shape", new List<string>(itemshapNames) }
    //    };
    //    var listValuesForAttributes = (await connection.QueryAsync<HierarchyValidation>($"select b.name as attribute, a.templateid, (select * from attributevalue(b.lookuptablecolumn, b.lookuptable)) as value from public.templateattribute a inner join public.productattribute b ON a.attributeid=b.id where templateid = {templateId} and b.datatype = 30 and b.isdatalist = true;")).ToList();
    //    foreach (var value in listValuesForAttributes)
    //    {
    //        if (!columnDropdownValues.ContainsKey(value.Attribute))
    //        {
    //            columnDropdownValues[value.Attribute] = new List<string>();
    //        }

    // columnDropdownValues[value.Attribute].Add(value.Value.ToString());
    //    }

    // foreach (var attribute in attributes)
    //    {
    //        string[] stringArray = null;
    //        col = dt.Columns.Add(attribute.Name);
    //        index++;
    //        if (attribute.IsDataList)
    //        {
    //            if (columnDropdownValues.ContainsKey(attribute.Name))
    //            {
    //                stringArray = columnDropdownValues[attribute.Name].ToArray();
    //            }
    //            else
    //            {
    //                stringArray = listValuesForAttributes
    //                    .Where(v => v.Attribute == attribute.Name)
    //                    .Select(v => v.Value.ToString())
    //                    .ToArray();
    //            }

    // listStringInputs.Add(stringArray);
    //            listCells.Add(index - 1);
    //            isList = true;
    //        }
    //    }

    // dt.Rows.Add(newRow);

    // using var package = new ExcelPackage();
    //    var worksheet = package.Workbook.Worksheets.Add("Sheet1");

    // for (int i = 0; i < dt.Columns.Count; i++)
    //    {
    //        var colHeader = dt.Columns[i].ColumnName;
    //        worksheet.Cells[1, i + 1].Value = colHeader;

    // var color = await connection.ExecuteScalarAsync<bool?>("SELECT pa.isrequired FROM productattribute pa join template t on pa.divisionid = t.divisionid " +
    //            "WHERE pa.name = @colHeader and t.id = @templateid", new { colHeader, templateId });

    // var colLetter = ExcelCellBase.GetAddress(1, i + 1);

    // if (colHeader == "Brand Type" || colHeader == "Currency" || colHeader == "Item Type code" || colHeader == "Loipl Shoot Required" || colHeader == "Primary Packaging" || colHeader == "Lead Time Days" || colHeader == "Item Shape")
    //        {
    //            worksheet.Cells[colLetter].Style.Font.Bold = true;
    //            worksheet.Cells[colLetter].Style.Font.Color.SetColor(System.Drawing.Color.White);
    //            worksheet.Cells[colLetter].Style.Fill.PatternType = ExcelFillStyle.Solid;
    //            int darkerRed = (int)(System.Drawing.Color.Yellow.R * 0.8);
    //            int darkerGreen = (int)(System.Drawing.Color.Yellow.G * 0.8);
    //            int darkerBlue = (int)(System.Drawing.Color.Yellow.B * 0.8);
    //            System.Drawing.Color darkerYellow = System.Drawing.Color.FromArgb(darkerRed, darkerGreen, darkerBlue);
    //            worksheet.Cells[colLetter].Style.Fill.BackgroundColor.SetColor(darkerYellow);
    //        }
    //        else if (color.HasValue && color.Value)
    //        {
    //            worksheet.Cells[colLetter].Style.Font.Bold = true;
    //            worksheet.Cells[colLetter].Style.Font.Color.SetColor(System.Drawing.Color.White);
    //            worksheet.Cells[colLetter].Style.Fill.PatternType = ExcelFillStyle.Solid;
    //            worksheet.Cells[colLetter].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LimeGreen);
    //        }
    //        else
    //        {
    //            worksheet.Cells[colLetter].Style.Font.Bold = true;
    //            worksheet.Cells[colLetter].Style.Font.Color.SetColor(System.Drawing.Color.White);
    //            worksheet.Cells[colLetter].Style.Fill.PatternType = ExcelFillStyle.Solid;
    //            worksheet.Cells[colLetter].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SteelBlue);
    //        }

    // if (colHeader == "Currency")
    //        {
    //            if (columnDropdownValues.ContainsKey(colHeader))
    //            {
    //                var values = columnDropdownValues[colHeader];

    // var hiddenWorksheet = worksheet.Workbook.Worksheets.Add("HiddenSheet");
    //                hiddenWorksheet.Hidden = eWorkSheetHidden.VeryHidden;

    // for (int j = 0; j < values.Count; j++)
    //                {
    //                    hiddenWorksheet.Cells[j + 2, 1].Value = values[j];
    //                }

    // var namedRange = worksheet.Names.Add("CurrencyList", hiddenWorksheet.Cells[2, 1, values.Count + 1, 1]);

    // for (int rowIndex = 2; rowIndex <= 101; rowIndex++)
    //                {
    //                    var cellAddress = worksheet.Cells[rowIndex, i + 1].Address;
    //                    var validation = worksheet.DataValidations.AddListValidation(cellAddress);
    //                    validation.Formula.ExcelFormula = "CurrencyList";
    //                    validation.ShowErrorMessage = true;
    //                    validation.ErrorTitle = "Invalid Value";
    //                    validation.Error = "Please enter a valid value from the dropdown list";
    //                }
    //            }
    //        }
    //        else if (columnDropdownValues.ContainsKey(colHeader))
    //        {
    //            for (int rowIndex = 2; rowIndex <= 101; rowIndex++)
    //            {
    //                var validation = worksheet.DataValidations.AddListValidation(worksheet.Cells[rowIndex, i + 1, rowIndex, i + 1].ToString());
    //                validation.Formula.ExcelFormula = "\"" + string.Join(",", columnDropdownValues[colHeader]) + "\"";
    //                validation.ShowErrorMessage = true;
    //                validation.ErrorTitle = "Invalid Value";
    //                validation.Error = "Please enter a valid value from the dropdown list";
    //            }
    //        }
    //        else if (fiveAttributes.Contains(colHeader))
    //        {
    //            for (int rowIndex = 2; rowIndex <= Math.Min(101, dt.Rows.Count + 1); rowIndex++)
    //            {
    //                var validation = worksheet.DataValidations.AddListValidation(worksheet.Cells[rowIndex, i + 1, rowIndex, i + 1].ToString());
    //                validation.Formula.ExcelFormula = "\"" + string.Join(",", listStringInputs[listCells.IndexOf(i)]) + "\"";
    //            }
    //        }
    //    }

    // var memoryStream = new MemoryStream();
    //    package.SaveAs(memoryStream);
    //    return memoryStream.ToArray();
    // }
    public async Task<byte[]> DownloadTemplate(long templateId, string format)
    {
        using var connection = this.GetConnection();
        var attributes = await this.ListTemplateAttributes(templateId);
        var dt = new DataTable();
        dt.TableName = "Data";
        List<string> fiveAttributes = new List<string> { "Brand Type", "Currency", "Item Type code", "Loipl Shoot Required", "Primary Packaging" };

        var isList = false;
        var col = new DataColumn();
        var listStringInputs = new List<string[]>();
        var listCells = new List<int>();
        var newRow = dt.NewRow();
        var index = 0;

        var leadDropdownValues = await connection.QueryAsync<MtrLeadTime>("select name from mtrleadtime");
        var leadNames = leadDropdownValues.Select(item => item.Name).ToArray();

        var itemTypeCodeDropdownValues = await connection.QueryAsync<MtrItemTypeCode>("select code from mtritemtypecode");
        var itemtype = itemTypeCodeDropdownValues.Select(item => item.Code.ToString()).ToArray();

        var itemshape = await connection.QueryAsync<MtrItemShape>("select name from mtritemshape");
        var itemshapNames = itemshape.Select(item => item.Name).ToArray();

        var currency = await connection.QueryAsync<MtrCurrency>("select name from mtrcurrency");
        var currencyNames = currency.Select(item => item.Name).ToArray();

        var brandType = await connection.QueryAsync<MtrBrandType>("select name from mtrbrandtype");
        var brandTypeNames = brandType.Select(item => item.Name).ToArray();

        // Dictionary<string, List<string>> columnDropdownValues = new Dictionary<string, List<string>>
        // {
        //    { "Brand Type", new List<string>(brandTypeNames) },
        //    { "Currency", new List<string>(currencyNames) },
        //    { "Item Type code", new List<string>(itemtype) },
        //    { "Loipl Shoot Required", new List<string> { "Yes", "No" } },
        //    { "Primary Packaging", new List<string> { "Yes", "No" } },
        //    { "Lead Time Days", new List<string>(leadNames) },
        //    {"Item Shape", new List<string>(itemshapNames) }
        // };
        var listValuesForAttributes = (await connection.QueryAsync<HierarchyValidation>($"select b.name as attribute, a.templateid, (select * from attributevalue(b.lookuptablecolumn, b.lookuptable)) as value from public.templateattribute a inner join public.productattribute b ON a.attributeid=b.id where templateid = {templateId} and b.datatype = 30 and b.isdatalist = true;")).ToList();

        // foreach (var value in listValuesForAttributes)
        // {
        //    if (!columnDropdownValues.ContainsKey(value.Attribute))
        //    {
        //        columnDropdownValues[value.Attribute] = new List<string>();
        //    }

        // columnDropdownValues[value.Attribute].Add(value.Value.ToString());
        // }
        foreach (var attribute in attributes)
        {
            string[] stringArray = null;
            col = dt.Columns.Add(attribute.Name);
            index++;
            if (attribute.IsDataList)
            {
                // if (columnDropdownValues.ContainsKey(attribute.Name))
                // {
                //    stringArray = columnDropdownValues[attribute.Name].ToArray();
                // }
                // else
                // {
                stringArray = listValuesForAttributes
                    .Where(v => v.Attribute == attribute.Name)
                    .Select(v => v.Value.ToString())
                    .ToArray();

                // }
                listStringInputs.Add(stringArray);
                listCells.Add(index - 1);
                isList = true;
            }
        }

        dt.Rows.Add(newRow);

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Sheet1");

        for (int i = 0; i < dt.Columns.Count; i++)
        {
            var colHeader = dt.Columns[i].ColumnName;
            worksheet.Cells[1, i + 1].Value = colHeader;

            var color = await connection.ExecuteScalarAsync<bool?>("SELECT pa.isrequired FROM productattribute pa join template t on pa.divisionid = t.divisionid " +
                "WHERE pa.name = @colHeader and t.id = @templateid", new { colHeader, templateId });

            var colLetter = ExcelCellBase.GetAddress(1, i + 1);
            if (color.HasValue && color.Value)
            {
                worksheet.Cells[colLetter].Style.Font.Bold = true;
                worksheet.Cells[colLetter].Style.Font.Color.SetColor(System.Drawing.Color.White);
                worksheet.Cells[colLetter].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[colLetter].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LimeGreen);
            }
            else
            {
                worksheet.Cells[colLetter].Style.Font.Bold = true;
                worksheet.Cells[colLetter].Style.Font.Color.SetColor(System.Drawing.Color.White);
                worksheet.Cells[colLetter].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[colLetter].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.SteelBlue);
            }

            // if (colHeader == "Currency")
            // {
            //    if (columnDropdownValues.ContainsKey(colHeader))
            //    {
            //        var values = columnDropdownValues[colHeader];

            // var hiddenWorksheet = worksheet.Workbook.Worksheets.Add("HiddenSheet");
            //        hiddenWorksheet.Hidden = eWorkSheetHidden.VeryHidden;

            // for (int j = 0; j < values.Count; j++)
            //        {
            //            hiddenWorksheet.Cells[j + 2, 1].Value = values[j];
            //        }

            // var namedRange = worksheet.Names.Add("CurrencyList", hiddenWorksheet.Cells[2, 1, values.Count + 1, 1]);

            // for (int rowIndex = 2; rowIndex <= 101; rowIndex++)
            //        {
            //            var cellAddress = worksheet.Cells[rowIndex, i + 1].Address;
            //            var validation = worksheet.DataValidations.AddListValidation(cellAddress);
            //            validation.Formula.ExcelFormula = "CurrencyList";
            //            validation.ShowErrorMessage = true;
            //            validation.ErrorTitle = "Invalid Value";
            //            validation.Error = "Please enter a valid value from the dropdown list";
            //        }
            //    }
            // }
            // else if (columnDropdownValues.ContainsKey(colHeader))
            // {
            //    for (int rowIndex = 2; rowIndex <= 101; rowIndex++)
            //    {
            //        var validation = worksheet.DataValidations.AddListValidation(worksheet.Cells[rowIndex, i + 1, rowIndex, i + 1].ToString());
            //        validation.Formula.ExcelFormula = "\"" + string.Join(",", columnDropdownValues[colHeader]) + "\"";
            //        validation.ShowErrorMessage = true;
            //        validation.ErrorTitle = "Invalid Value";
            //        validation.Error = "Please enter a valid value from the dropdown list";
            //    }
            // }
            // else if (fiveAttributes.Contains(colHeader))
            // {
            //    for (int rowIndex = 2; rowIndex <= Math.Min(101, dt.Rows.Count + 1); rowIndex++)
            //    {
            //        var validation = worksheet.DataValidations.AddListValidation(worksheet.Cells[rowIndex, i + 1, rowIndex, i + 1].ToString());
            //        validation.Formula.ExcelFormula = "\"" + string.Join(",", listStringInputs[listCells.IndexOf(i)]) + "\"";
            //    }
            // }
        }

        var memoryStream = new MemoryStream();
        package.SaveAs(memoryStream);
        return memoryStream.ToArray();
    }

    public async Task<byte[]> DownloadItemCodeTemplate(DateTime? fromdate, DateTime? toDate, string status, string? deptCode, string? division)
    {
        using var connection = this.GetConnection();
        fromdate = (fromdate == DateTime.MinValue) ? (DateTime?)null : fromdate;
        toDate = (toDate == DateTime.MinValue) ? (DateTime?)null : toDate;
        var dataFromFilter = await _productManager.ListProductsItem(fromdate, toDate, status, deptCode, division);
        var dt = new DataTable();
        dt.TableName = "ItemCode";

        foreach (var data in dataFromFilter)
        {
            if (data.Uid == null)
            {
                throw new Exception("uid is invalid");
            }

            var product = (await connection.QueryAsync<Product>($"select columndata, (SELECT name FROM mtrsetpcs WHERE code::text = columndata->>'UOM' LIMIT 1) as Uid from product where uid = '{data.Uid}'")).FirstOrDefault();

            // added by Avinash
            // var jsonString = await _uploadManager.ItemCodeAttributeJsonExtraction();
            if (product == null || product.ColumnData == null)
            {
                throw new Exception("Product data is missing.");
            }

            // Determine which JSON extraction function to call
            List<ItemCodeCreation> jsonString = new List<ItemCodeCreation>();

            if (product.ColumnData.ContainsKey("Division Code") && product.ColumnData["Division Code"].ToString() == "61" && product.ColumnData.ContainsKey("Brand Code") && product.ColumnData["Brand Code"].ToString() != "1435")
            {
                // Console.WriteLine("ItemCodeAttributeJsonExtractionHBrand Called");
                jsonString = await _uploadManager.ItemCodeAttributeJsonExtraction();

                // Date for Margin from date
                string plcCode = string.Empty;
                string priceGrouping = string.Empty;

                if (product.ColumnData.ContainsKey("PLC Code") && product.ColumnData["PLC Code"] != null)
                {
                    plcCode = product.ColumnData["PLC Code"].ToString();
                }

                if (product.ColumnData.ContainsKey("Price Grouping") && product.ColumnData["Price Grouping"] != null)
                {
                    priceGrouping = product.ColumnData["Price Grouping"].ToString();
                }

                foreach (var item in jsonString)
                {
                    if (item.AttributeName == "MARGIN_FRM_DT")
                    {
                        item.Value = DateTime.Now.ToString("dd-MM-yyyy"); // Format: dd-MM-yyyy
                    }

                    if (item.AttributeName == "UDA_CONCATENATED")
                    {
                        item.Value = $"37|{plcCode}|34|{priceGrouping}";
                    }
                }
            }
            else if (product.ColumnData.ContainsKey("Division Code") && product.ColumnData["Division Code"].ToString() == "61" && product.ColumnData.ContainsKey("Brand Code") && product.ColumnData["Brand Code"].ToString() == "1435" && product.ColumnData.ContainsKey("Purchase Type") && product.ColumnData["Purchase Type"].ToString() == "1")
            {
                // Console.WriteLine("ItemCodeAttributeJsonExtractionHPDomestic Called");
                jsonString = await _uploadManager.ItemCodeAttributeJsonExtractionHPD();

                string masterCartonCode = string.Empty;
                string plcCode = string.Empty;
                string itemTypeCode = string.Empty;
                string planoCode = string.Empty;
                string priceGrouping = string.Empty;
                string prodClassCode = string.Empty;

                if (product.ColumnData.ContainsKey("Master Carton Code") && product.ColumnData["Master Carton Code"] != null)
                {
                    masterCartonCode = product.ColumnData["Master Carton Code"].ToString();
                }

                if (product.ColumnData.ContainsKey("PLC Code") && product.ColumnData["PLC Code"] != null)
                {
                    plcCode = product.ColumnData["PLC Code"].ToString();
                }

                if (product.ColumnData.ContainsKey("Item Type code") && product.ColumnData["Item Type code"] != null)
                {
                    itemTypeCode = product.ColumnData["Item Type code"].ToString();
                }

                if (product.ColumnData.ContainsKey("Planogram/Family Code") && product.ColumnData["Planogram/Family Code"] != null)
                {
                    planoCode = product.ColumnData["Planogram/Family Code"].ToString();
                }

                if (product.ColumnData.ContainsKey("Price Grouping") && product.ColumnData["Price Grouping"] != null)
                {
                    priceGrouping = product.ColumnData["Price Grouping"].ToString();
                }

                if (product.ColumnData.ContainsKey("Product Classification code") && product.ColumnData["Product Classification code"] != null)
                {
                    prodClassCode = product.ColumnData["Product Classification code"].ToString();
                }

                foreach (var item in jsonString)
                {
                    if (item.AttributeName == "UDA_CONCATENATED")
                    {
                        item.Value = $"5|{masterCartonCode}|37|{plcCode}|6|{itemTypeCode}|35|{planoCode}|34|{priceGrouping}|33|{prodClassCode}";
                    }
                }
            }
            else if (product.ColumnData.ContainsKey("Division Code") && product.ColumnData["Division Code"].ToString() == "61" && product.ColumnData.ContainsKey("Brand Code") && product.ColumnData["Brand Code"].ToString() == "1435" && product.ColumnData.ContainsKey("Purchase Type") && product.ColumnData["Purchase Type"].ToString() == "2")
            {
                // Console.WriteLine("ItemCodeAttributeJsonExtractionHPImport Called");
                jsonString = await _uploadManager.ItemCodeAttributeJsonExtractionHPI();

                string masterCartonCode = string.Empty;
                string plcCode = string.Empty;
                string itemTypeCode = string.Empty;
                string planoCode = string.Empty;
                string priceGrouping = string.Empty;
                string prodClassCode = string.Empty;

                if (product.ColumnData.ContainsKey("Master Carton Code") && product.ColumnData["Master Carton Code"] != null)
                {
                    masterCartonCode = product.ColumnData["Master Carton Code"].ToString();
                }

                if (product.ColumnData.ContainsKey("PLC Code") && product.ColumnData["PLC Code"] != null)
                {
                    plcCode = product.ColumnData["PLC Code"].ToString();
                }

                if (product.ColumnData.ContainsKey("Item Type code") && product.ColumnData["Item Type code"] != null)
                {
                    itemTypeCode = product.ColumnData["Item Type code"].ToString();
                }

                if (product.ColumnData.ContainsKey("Planogram/Family Code") && product.ColumnData["Planogram/Family Code"] != null)
                {
                    planoCode = product.ColumnData["Planogram/Family Code"].ToString();
                }

                if (product.ColumnData.ContainsKey("Price Grouping") && product.ColumnData["Price Grouping"] != null)
                {
                    priceGrouping = product.ColumnData["Price Grouping"].ToString();
                }

                if (product.ColumnData.ContainsKey("Product Classification code") && product.ColumnData["Product Classification code"] != null)
                {
                    prodClassCode = product.ColumnData["Product Classification code"].ToString();
                }

                foreach (var item in jsonString)
                {
                    if (item.AttributeName == "UDA_CONCATENATED")
                    {
                        item.Value = $"5|{masterCartonCode}|37|{plcCode}|6|{itemTypeCode}|35|{planoCode}|34|{priceGrouping}|33|{prodClassCode}";
                    }
                }

                if (product.ColumnData.ContainsKey("HSN Code") && !string.IsNullOrWhiteSpace(product.ColumnData["HSN Code"].ToString()))
                {
                    string hsnCode = product.ColumnData["HSN Code"].ToString();

                    if (hsnCode.Length >= 2)
                    {
                        string firstTwoDigits = hsnCode.Substring(0, 2); // Extract first 2 digits

                        foreach (var item in jsonString)
                        {
                            if (item.AttributeName == "QUOTA_CATEGORY")
                            {
                                item.Value = firstTwoDigits; // Update QUOTA_CATEGORY with HSN Code's first two digits
                                break; // Stop after first match
                            }
                        }
                    }
                }
            }
            else if (product.ColumnData.ContainsKey("Division Code") && (product.ColumnData["Division Code"].ToString() == "4" || product.ColumnData["Division Code"].ToString() == "65") && product.ColumnData.ContainsKey("Purchase Type") && product.ColumnData["Purchase Type"].ToString() == "1")
            {
                // Console.WriteLine("ItemCodeAttributeJsonExtractionFPDomestic Called");
                jsonString = await _uploadManager.ItemCodeAttributeJsonExtractionFPD();
           
                string plcCode = string.Empty;
                string itemTypeCode = string.Empty;
                string planoCode = string.Empty;
                string priceGrouping = string.Empty;
                string prodClassCode = string.Empty;

                if (product.ColumnData.ContainsKey("PLC Code") && product.ColumnData["PLC Code"] != null)
                {
                    plcCode = product.ColumnData["PLC Code"].ToString();
                }

                if (product.ColumnData.ContainsKey("Item Type code") && product.ColumnData["Item Type code"] != null)
                {
                    itemTypeCode = product.ColumnData["Item Type code"].ToString();
                }

                if (product.ColumnData.ContainsKey("Planogram/Family Code") && product.ColumnData["Planogram/Family Code"] != null)
                {
                    planoCode = product.ColumnData["Planogram/Family Code"].ToString();
                }

                if (product.ColumnData.ContainsKey("Price Grouping") && product.ColumnData["Price Grouping"] != null)
                {
                    priceGrouping = product.ColumnData["Price Grouping"].ToString();
                }

                if (product.ColumnData.ContainsKey("Product Classification code") && product.ColumnData["Product Classification code"] != null)
                {
                    prodClassCode = product.ColumnData["Product Classification code"].ToString();
                }

                foreach (var item in jsonString)
                {
                    if (item.AttributeName == "UDA_CONCATENATED")
                    {
                        item.Value = $"6|{itemTypeCode}|37|{plcCode}|34|{priceGrouping}|15||35|{planoCode}";
                    }
                }
            }
            else if (product.ColumnData.ContainsKey("Division Code") && (product.ColumnData["Division Code"].ToString() == "4" || product.ColumnData["Division Code"].ToString() == "65") && product.ColumnData.ContainsKey("Purchase Type") && product.ColumnData["Purchase Type"].ToString() == "2")
            {
                // Console.WriteLine("ItemCodeAttributeJsonExtractionFPImport Called");
                jsonString = await _uploadManager.ItemCodeAttributeJsonExtractionFPI();

                string plcCode = string.Empty;
                string itemTypeCode = string.Empty;
                string planoCode = string.Empty;
                string priceGrouping = string.Empty;
                string prodClassCode = string.Empty;

                if (product.ColumnData.ContainsKey("PLC Code") && product.ColumnData["PLC Code"] != null)
                {
                    plcCode = product.ColumnData["PLC Code"].ToString();
                }

                if (product.ColumnData.ContainsKey("Item Type code") && product.ColumnData["Item Type code"] != null)
                {
                    itemTypeCode = product.ColumnData["Item Type code"].ToString();
                }

                if (product.ColumnData.ContainsKey("Planogram/Family Code") && product.ColumnData["Planogram/Family Code"] != null)
                {
                    planoCode = product.ColumnData["Planogram/Family Code"].ToString();
                }

                if (product.ColumnData.ContainsKey("Price Grouping") && product.ColumnData["Price Grouping"] != null)
                {
                    priceGrouping = product.ColumnData["Price Grouping"].ToString();
                }

                if (product.ColumnData.ContainsKey("Product Classification code") && product.ColumnData["Product Classification code"] != null)
                {
                    prodClassCode = product.ColumnData["Product Classification code"].ToString();
                }

                foreach (var item in jsonString)
                {
                    if (item.AttributeName == "UDA_CONCATENATED")
                    {
                        item.Value = $"6|{itemTypeCode}|37|{plcCode}|34|{priceGrouping}|15||35|{planoCode}";
                    }
                }

                if (product.ColumnData.ContainsKey("HSN Code") && !string.IsNullOrWhiteSpace(product.ColumnData["HSN Code"].ToString()))
                {
                    string hsnCode = product.ColumnData["HSN Code"].ToString();

                    if (hsnCode.Length >= 2)
                    {
                        string firstTwoDigits = hsnCode.Substring(0, 2); // Extract first 2 digits

                        foreach (var item in jsonString)
                        {
                            if (item.AttributeName == "QUOTA_CATEGORY")
                            {
                                item.Value = firstTwoDigits; // Update QUOTA_CATEGORY with HSN Code's first two digits
                                break; // Stop after first match
                            }
                        }
                    }
                }
            }

            var rw = dt.NewRow();
            dt.Rows.Add(rw);

            foreach (var item in jsonString)
            {
                if (item.Color.ToLower() == "r")
                {
                    if (!dt.Columns.Contains(item.AttributeName))
                    {
                        dt.Columns.Add(item.AttributeName);
                    }

                    rw[item.AttributeName] = item.Value;
                }

                if (item.Color == "g")
                {
                    if (item.Value == null || item.Value == string.Empty)
                    {
                        if (!dt.Columns.Contains(item.AttributeName))
                        {
                            dt.Columns.Add(item.AttributeName);
                        }
                    }
                }

                if (item.Color == "w")
                {
                    if (item.Value != null && item.Value != string.Empty)
                    {
                        var attrValue = product.ColumnData[item.Value];
                        if (!dt.Columns.Contains(item.AttributeName))
                        {
                            dt.Columns.Add(item.AttributeName);
                        }

                        rw[item.AttributeName] = attrValue;
                    }
                    else
                    {
                        var attrValue = product.ColumnData[item.Attribute];
                        if (!dt.Columns.Contains(item.AttributeName))
                        {
                            dt.Columns.Add(item.AttributeName);
                        }

                        if (item.AttributeName == "UOM")
                        {
                            attrValue = product.Uid;
                        }

                        rw[item.AttributeName] = attrValue;
                    }
                }
            }
        }

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("ItemCode");

        worksheet.Cells["A1"].LoadFromDataTable(dt, true);

        var headerBackgroundColor = System.Drawing.ColorTranslator.FromHtml("#5787C5");
        var headerTextColor = System.Drawing.Color.White;
        var borderColor = System.Drawing.ColorTranslator.FromHtml("#5787C5");

        for (int i = 0; i < dt.Columns.Count; i++)
        {
            var cell = worksheet.Cells[1, i + 1];
            cell.Value = dt.Columns[i].ColumnName;
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

        for (int row = 2; row <= dt.Rows.Count + 1; row++)
        {
            for (int col = 1; col <= dt.Columns.Count; col++)
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
        }

        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

        using var memoryStream = new MemoryStream();
        package.SaveAs(memoryStream);
        return memoryStream.ToArray();
    }

    public async Task<byte[]> DownloadAllItemCodeTemplate(DateTime? fromdate, DateTime? toDate, string status, string? deptCode, string? division)
    {
        using var connection = this.GetConnection();
        fromdate = (fromdate == DateTime.MinValue) ? (DateTime?)null : fromdate;
        toDate = (toDate == DateTime.MinValue) ? (DateTime?)null : toDate;
        var dataFromFilter = await _productManager.ListProductsItem(fromdate, toDate, status, deptCode, division);

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Item Codes");

        if (dataFromFilter != null && dataFromFilter.Any())
        {
            var headers = new[] { "Uid", "Division", "Department", "Date Created", "Status" };
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
            foreach (var record in dataFromFilter)
            {
                if (record.Uid != null)
                {
                    worksheet.Cells[row, 1].Value = record.Uid;
                    worksheet.Cells[row, 2].Value = record.Div_Name;
                    worksheet.Cells[row, 3].Value = record.Dept_Name;
                    DateTime dateCreated = record.DateCreated;
                    string formattedDate = dateCreated.ToString("dd/MM/yyyy");
                    worksheet.Cells[row, 4].Value = formattedDate;
                    worksheet.Cells[row, 5].Value = record.Status != null && record.Status.Contains("1") ? "Ready" : "Not Ready";

                    row++;
                }
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }

        worksheet.Column(1).Width = 22;
        worksheet.Column(2).Width = 22;
        worksheet.Column(3).Width = 32;
        worksheet.Column(4).Width = 18;
        worksheet.Column(5).Width = 16;

        using var memoryStream = new MemoryStream();
        package.SaveAs(memoryStream);
        return memoryStream.ToArray();
    }

    public async Task<byte[]> ItemcodeAllDownload(long marketplaceTemplateId, DateTime? fromDate, DateTime? toDate, string? division, string? deptcode, string? status, string? planogramname)
    {
        using var connection = this.GetConnection();
        var startDates = fromDate ?? DateTime.MinValue;
        var endDates = toDate ?? DateTime.MaxValue;
        var formattedStartDate = startDates.ToString("yyyy/MM/dd");
        var formattedEndDate = endDates.ToString("yyyy/MM/dd");

        if (marketplaceTemplateId == 0)
        {
            throw new Exception("There is No data for Selected Marketplace,Division and  Category");
        }

        var itemcodeQuery = "SELECT ic.eancode, ic.uidcode AS Uid, v.planogram_family_name AS PlanoOrFamilyName, ic.code AS itemcode,ic.sourcedatecreated AS datecreated,p.columndata->>'Division Name' AS Division,p.columndata->>'Dept Name' AS DeptName, p.statusfilter AS Statusfilter " +
            "FROM itemcodestaging ic " +
            "LEFT JOIN product p ON ic.Uidcode = p.uid " +
            "LEFT JOIN vw_uid v ON p.uid = v.uid " +
            "WHERE v.isactive = true and to_char(ic.sourcedatecreated::date::timestamp with time zone, 'yyyy/MM/dd') BETWEEN @FormattedStartDate AND @FormattedEndDate ";
        if (!string.IsNullOrEmpty(planogramname))
        {
            var planogramName = planogramname.Replace("'", "''");
            itemcodeQuery += $"AND LOWER(p.columndata->>'Planogram Family Name') LIKE LOWER('%{planogramName}%') ";
        }

        if (status == "1")
        {
            itemcodeQuery += "AND p.statusfilter = '1' ";
        }
        else if (status == "2")
        {
            itemcodeQuery += "AND (p.statusfilter ='0' OR p.statusfilter IS NULL) ";
        }

        if (!string.IsNullOrEmpty(division))
        {
            string[] divisionIds = division.Split(',');
            itemcodeQuery += $"AND p.columndata->>'Division Code' IN ('{string.Join("','", divisionIds)}') ";
        }

        if (!string.IsNullOrEmpty(deptcode))
        {
            string[] departmentIds = deptcode.Split(',');
            itemcodeQuery += $"AND p.columndata->>'Dept Code' IN ('{string.Join("','", departmentIds)}') ";
        }

        itemcodeQuery += "UNION ALL " +
            "SELECT NULL AS itemcode, NULL AS datecreated, NULL AS Division,NULL AS PlanoOrFamilyName ,NULL AS DeptName,Null AS eancode,NULL AS Uid,NULL AS status " +
            "FROM productattribute pa " +
            "JOIN marketplacetemplateattribute mta ON pa.id = mta.productattributeid " +
            "JOIN marketplacetemplate mt ON mta.marketplacetemplateid = mt.id " +
            "WHERE mt.id = @MarketplaceTemplateId  ORDER BY datecreated DESC";

        var parameters = new
        {
            FormattedStartDate = formattedStartDate,
            FormattedEndDate = formattedEndDate,
            MarketplaceTemplateId = marketplaceTemplateId
        };
        var results = await connection.QueryAsync<ItemCodeDownload>(itemcodeQuery, parameters);

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("ItemCodes");

        if (results.Any())
        {
            var headers = new[] { "Uid", "PlanoOrFamily Name", "Item Code", "EAN Code", "Division", "Department", "Date Created", "Status" };
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

            int row = 2;
            foreach (var record in results)
            {
                if (record.ItemCode != 0)
                {
                    worksheet.Cells[row, 1].Value = record.Uid;
                    worksheet.Cells[row, 2].Value = record.PlanoOrFamilyName;
                    worksheet.Cells[row, 3].Value = record.ItemCode.ToString();
                    worksheet.Cells[row, 4].Value = record.Eancode.ToString();
                    worksheet.Cells[row, 5].Value = record.Division;
                    worksheet.Cells[row, 6].Value = record.DeptName;
                    worksheet.Cells[row, 7].Value = record.DateCreated.ToString("dd/MM/yyyy");
                    worksheet.Cells[row, 8].Value = record.Statusfilter != null && record.Statusfilter.Contains("1") ? "Ready" : "Not Ready";

                    for (int col = 1; col <= 8; col++)
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
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }

        worksheet.Column(1).Width = 22;
        worksheet.Column(2).Width = 22;
        worksheet.Column(3).Width = 22;
        worksheet.Column(4).Width = 22;
        worksheet.Column(5).Width = 22;
        worksheet.Column(6).Width = 30;
        worksheet.Column(7).Width = 16;
        worksheet.Column(8).Width = 14;

        using var memoryStream = new MemoryStream();
        package.SaveAs(memoryStream);
        return memoryStream.ToArray();
    }

    public async Task<FileContentResult> ItemCodeDownload(DateTime? fromDate, DateTime? toDate, long marketplaceTemplateId, string? division, string? deptcode, string? planogramname)
    {
        using var connection = this.GetConnection();
        var startDates = fromDate ?? DateTime.MinValue;
        var endDates = toDate ?? DateTime.MaxValue;
        var formattedStartDate = startDates.ToString("yyyy/MM/dd");
        var formattedEndDate = endDates.ToString("yyyy/MM/dd");

        var query = "SELECT ic.id AS itemcode, NULL AS productattributename, NULL AS productattributeid, ic.uidcode, NULL AS marketplaceattribute " +
   "FROM itemcodestaging ic " +
   "LEFT JOIN product p ON ic.uidcode = p.uid " +
   "WHERE to_char(ic.sourcedatecreated::date::timestamp with time zone, 'yyyy/MM/dd') BETWEEN @FormattedStartDate AND @FormattedEndDate AND p.statusfilter = '1' ";
        if (!string.IsNullOrEmpty(planogramname))
        {
            var planogramName = planogramname.Replace("'", "''");
            query += $"AND LOWER(p.columndata->>'Planogram Family Name') LIKE LOWER('%{planogramName}%') ";
        }

        if (!string.IsNullOrEmpty(division))
        {
            string[] divisionIds = division.Split(',');
            query += $"AND p.columndata->>'Division Code' IN ('{string.Join("','", divisionIds)}') ";
        }

        if (!string.IsNullOrEmpty(deptcode))
        {
            string[] departmentIds = deptcode.Split(',');
            query += $"AND p.columndata->>'Dept Code' IN ('{string.Join("','", departmentIds)}') ";
        }

        var query1 = "SELECT NULL AS itemcode, pa.name AS productattributename, pa.id AS productattributeid, NULL AS name, mta.marketplaceattribute " +
    "FROM productattribute pa " +
    "JOIN marketplacetemplateattribute mta ON pa.id = mta.productattributeid " +
    "JOIN marketplacetemplate mt ON mta.marketplacetemplateid = mt.id " +
    "WHERE mt.id = @MarketplaceTemplateId " +
    "ORDER BY CASE WHEN mta.columnorder = '' THEN NULL ELSE CAST(mta.columnorder AS INT) END, productattributeid ASC";

        var parameters = new
        {
            FormattedStartDate = formattedStartDate,
            FormattedEndDate = formattedEndDate,
        };

        var parameters1 = new
        {
            MarketplaceTemplateId = marketplaceTemplateId
        };

        var results = await connection.QueryAsync(query, parameters);

        var results1 = await connection.QueryAsync(query1, parameters1);

        var itemCodes = results.Select(r => r.itemcode).ToList();
        var names = results1.Select(r => r.marketplaceattribute).Where(n => n != null).ToList();

        var productAttributeNames = results1
            .Where(r => r.productattributename != null)
            .Select(r => r.productattributename)
            .ToList();

        var marketplaceAttributeNames = results1
            .Where(r => r.marketplaceattribute != null)
            .Select(r => r.marketplaceattribute.Trim().TrimStart())
            .ToList();

        var product = @"
                 SELECT pa.name
                 FROM marketplacetemplateattribute AS mta
                 JOIN productattribute AS pa ON mta.productattributeid = pa.id
                 WHERE mta.marketplacetemplateid = @MarketplaceTemplateId
                 AND mta.datatype IS NOT NULL
                 AND mta.marketplaceattribute IS NOT NULL
                 order by CASE WHEN mta.columnorder = '' THEN NULL ELSE CAST(mta.columnorder AS INT) END, pa.id asc";

        var parameter = new { MarketplaceTemplateId = marketplaceTemplateId };

        var result = await connection.QueryAsync<string>(product, parameter);

        var validProductAttributeNames = result
            .Where(name => !string.IsNullOrEmpty(name))
            .ToList();

        using (var package = new ExcelPackage())
        {
            var sheetname = "Itemcode";
            var marketplaceName = await connection.QueryFirstOrDefaultAsync<string>($"SELECT name FROM public.marketplacetemplate WHERE id = {marketplaceTemplateId}");

            if (marketplaceName != null)
            {
                sheetname = marketplaceName;
            }

            var worksheet = package.Workbook.Worksheets.Add(sheetname);
            var rowIndex = 1;

            worksheet.Cells[rowIndex, 1].Value = "ItemCode";

            var columnIndex = 2;

            foreach (var name in marketplaceAttributeNames)
            {
                worksheet.Cells[rowIndex, columnIndex].Value = name;
                columnIndex++;
            }

            rowIndex++;

            foreach (var itemCode in itemCodes)
            {
                var itemCodeValue = itemCode != null ? GetItemCodeName(itemCode) : string.Empty;
                var hasMatches = false;
                columnIndex = 2;

                foreach (var name in validProductAttributeNames)
                {
                    if (itemCode == null)
                    {
                        worksheet.Cells[rowIndex, columnIndex].Value = string.Empty;
                    }
                    else
                    {
                        var productQuery = string.Empty;
                        if (name == "Care Instruction 3 Caring Don't")
                        {
                            productQuery = $"SELECT columndata->> 'Care Instruction 3 Caring Don''t' AS value FROM product WHERE itemcodeid =  @ItemCodeId AND columndata->> 'Care Instruction 3 Caring Don''t' IS NOT NULL;";
                        }
                        else
                        {
                            productQuery = $"SELECT columndata->> '{name}' AS value FROM product WHERE itemcodeid = @ItemCodeId AND columndata->> '{name}' IS NOT NULL";
                        }

                        var productParameters = new { ItemCodeId = itemCode };
                        var productValues = await connection.QueryAsync<string>(productQuery, productParameters);
                        var values = string.Join(", ", productValues);
                        var discription = string.Empty;

                        if (name == "UOM" && !string.IsNullOrEmpty(values))
                        {
                            var uomquery = $"SELECT name FROM mtrsetpcs WHERE code::text = @code";
                            var uomParameters = new { code = values };
                            var disvalue = await connection.QueryAsync<string>(uomquery, uomParameters);
                            values = string.Join(", ", disvalue);
                        }

                        if (!string.IsNullOrEmpty(values))
                        {
                            hasMatches = true;
                        }

                        worksheet.Cells[rowIndex, columnIndex].Value = values;
                    }

                    columnIndex++;
                }

                if (hasMatches)
                {
                    worksheet.Cells[rowIndex, 1].Value = itemCodeValue;
                    rowIndex++;
                }
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            var content = package.GetAsByteArray();
            var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            var fileName = "CAM.xlsx";

            return new FileContentResult(content, contentType)
            {
                FileDownloadName = fileName
            };
        }
    }

    public async Task<FileContentResult> ItemCodeSearchDownload(long[] itemcode, long marketplaceTemplateId)
    {
        using var connection = this.GetConnection();

        var query = "SELECT ic.id AS itemcode, NULL AS productattributename, NULL AS productattributeid, ic.uidcode, NULL AS marketplaceattribute " +
    "FROM itemcodestaging ic " +
    "LEFT JOIN product p ON ic.id = p.itemcodeid " +
    "WHERE ic.code = ANY(ARRAY[@itemcode]) and p.statusfilter = '1' ";
        var query1 = "SELECT NULL AS itemcode, pa.name AS productattributename, pa.id AS productattributeid, NULL AS name, mta.marketplaceattribute " +
 "FROM productattribute pa " +
 "JOIN marketplacetemplateattribute mta ON pa.id = mta.productattributeid " +
 "JOIN marketplacetemplate mt ON mta.marketplacetemplateid = mt.id " +
 "WHERE mt.id = @MarketplaceTemplateId " +
 "ORDER BY productattributeid ASC";

        var parameters = new
        {
            itemcode = itemcode
        };

        var parameters1 = new
        {
            MarketplaceTemplateId = marketplaceTemplateId
        };

        var results = await connection.QueryAsync(query, parameters);
        var results1 = await connection.QueryAsync(query1, parameters1);

        var itemCodes = results.Select(r => r.itemcode).ToList();
        var names = results.Select(r => r.marketplaceattribute).Where(n => n != null).ToList();

        var productAttributeNames = results1
            .Where(r => r.productattributename != null)
            .Select(r => r.productattributename)
            .ToList();

        var marketplaceAttributeNames = results1
            .Where(r => r.marketplaceattribute != null)
            .Select(r => r.marketplaceattribute)
            .ToList();

        var product = @"
                    SELECT pa.name
                    FROM marketplacetemplateattribute AS mta
                    JOIN productattribute AS pa ON mta.productattributeid = pa.id
                    WHERE mta.marketplacetemplateid = @MarketplaceTemplateId
                    AND mta.datatype IS NOT NULL
                    AND mta.marketplaceattribute IS NOT NULL
                    order by pa.id asc";

        var parameter = new { MarketplaceTemplateId = marketplaceTemplateId };

        var result = await connection.QueryAsync<string>(product, parameter);

        var validProductAttributeNames = result
            .Where(name => !string.IsNullOrEmpty(name))
            .ToList();

        using (var package = new ExcelPackage())
        {
            var worksheet = package.Workbook.Worksheets.Add("Itemcode");
            var rowIndex = 1;

            worksheet.Cells[rowIndex, 1].Value = "ItemCode";

            var columnIndex = 2;

            foreach (var name in marketplaceAttributeNames)
            {
                worksheet.Cells[rowIndex, columnIndex].Value = name;
                columnIndex++;
            }

            rowIndex++;

            foreach (var itemCode in itemCodes)
            {
                var itemCodeValue = itemCode != null ? GetItemCodeName(itemCode) : string.Empty;
                var hasMatches = false;
                columnIndex = 2;

                foreach (var name in validProductAttributeNames)
                {
                    if (itemCode == null)
                    {
                        worksheet.Cells[rowIndex, columnIndex].Value = string.Empty;
                    }
                    else
                    {
                        var productQuery = $"SELECT columndata->> '{name}' AS value FROM product WHERE itemcodeid = @ItemCodeId AND columndata->> '{name}' IS NOT NULL";
                        var productParameters = new { ItemCodeId = itemCode };
                        var productValues = await connection.QueryAsync<string>(productQuery, productParameters);
                        var values = string.Join(", ", productValues);

                        if (!string.IsNullOrEmpty(values))
                        {
                            hasMatches = true;
                        }

                        worksheet.Cells[rowIndex, columnIndex].Value = values;
                    }

                    columnIndex++;
                }

                if (hasMatches)
                {
                    worksheet.Cells[rowIndex, 1].Value = itemCodeValue;
                    rowIndex++;
                }
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            var content = package.GetAsByteArray();
            var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            var fileName = "CAM.xlsx";

            return new FileContentResult(content, contentType)
            {
                FileDownloadName = fileName
            };
        }
    }

    public async Task<List<ItemCodeMultiSearch>> GetItemCodeMultiSearch(long[] itemcodes)
    {
        using var connection = this.GetConnection();
        var query = "SELECT ic.id AS itemcode, ic.eancode, ic.uidcode as Uid, v.planogram_family_name as PlanogramName, ic.sourcedatecreated as datecreated, p.statusfilter, p.columndata->>'Division Code' as Division, p.columndata->>'Dept Code' as Deptcode " +
    "FROM itemcodestaging ic " +
    "LEFT JOIN product p ON ic.Uidcode = p.uid " +
    "LEFT JOIN vw_uid v ON p.uid = v.uid " +
    "WHERE ";

        for (int i = 0; i < itemcodes.Length; i++)
        {
            query += $"ic.code = {itemcodes[i]}";

            if (i < itemcodes.Length - 1)
            {
                query += " or ";
            }
        }

        var results = await connection.QueryAsync(query);
        var itemCodeData = new List<ItemCodeMultiSearch>();
        foreach (var item in results)
        {
            var itemCodeRow = new ItemCodeMultiSearch
            {
                ItemCode = item.itemcode != null ? GetItemCodeName(item.itemcode) : string.Empty,
                Statusfilter = new List<string> { item.statusfilter },
                DateCreated = item.datecreated,
                Division = item.division,
                Deptcode = item.deptcode,
                Eancode = item.eancode,
                Uid = item.uid,
                PlanogramName = item.planogramname,
            };

            itemCodeData.Add(itemCodeRow);
        }

        return itemCodeData;
    }

    public async Task<List<ItemCodeSearch>> GetItemCode(ItemCodeData itemData)
    {
        using var connection = this.GetConnection();
        var startDates = itemData.FromDate ?? DateTime.MinValue;
        var endDates = itemData.ToDate ?? DateTime.MaxValue;
        var formattedStartDate = startDates.ToString("yyyy/MM/dd");
        var formattedEndDate = endDates.ToString("yyyy/MM/dd");

        if (itemData.MarketplaceTemplateId == 0)
        {
            throw new Exception("There is No data for Selected Marketplace,Division and  Category");
        }

        var query = "SELECT ic.id AS itemcode, ic.datecreated, p.statusfilter " +
            "FROM itemcodestaging ic " +
            "LEFT JOIN product p ON ic.Uidcode = p.uid " +
            "WHERE to_char(ic.datecreated::date::timestamp with time zone, 'yyyy/MM/dd') BETWEEN @FormattedStartDate AND @FormattedEndDate ";
        if (itemData.Status == "1")
        {
            query += "AND p.statusfilter = @Status ";
        }

        query += "UNION ALL " +
            "SELECT NULL AS itemcode, NULL AS datecreated, NULL AS status " +
            "FROM productattribute pa " +
            "JOIN marketplacetemplateattribute mta ON pa.id = mta.productattributeid " +
            "JOIN marketplacetemplate mt ON mta.marketplacetemplateid = mt.id " +
            "WHERE mt.id = @MarketplaceTemplateId ";
        object parameters = null;
        if (itemData.Status == "1")
        {
            parameters = new
            {
                FormattedStartDate = formattedStartDate,
                FormattedEndDate = formattedEndDate,
                MarketplaceTemplateId = itemData.MarketplaceTemplateId,
                Status = itemData.Status
            };
        }

        if (itemData.Status == "0")
        {
            parameters = new
            {
                FormattedStartDate = formattedStartDate,
                FormattedEndDate = formattedEndDate,
                MarketplaceTemplateId = itemData.MarketplaceTemplateId
            };
        }

        var results = await connection.QueryAsync(query, parameters);

        var itemCodeData = new List<ItemCodeSearch>();

        foreach (var item in results)
        {
            if (item.statusfilter != null)
            {
                if (itemData.Status == "1")
                {
                    if (item.statusfilter == itemData.Status)
                    {
                        var itemCodeRow = new ItemCodeSearch
                        {
                            ItemCode = item.itemcode != null ? GetItemCodeName(item.itemcode) : string.Empty,
                            Statusfilter = new List<string> { item.statusfilter },
                            DateCreated = item.datecreated
                        };

                        itemCodeData.Add(itemCodeRow);
                    }
                }

                if (itemData.Status == "0")
                {
                    var itemCodeRow = new ItemCodeSearch
                    {
                        ItemCode = item.itemcode != null ? GetItemCodeName(item.itemcode) : string.Empty,
                        Statusfilter = new List<string> { item.statusfilter },
                        DateCreated = item.datecreated
                    };

                    itemCodeData.Add(itemCodeRow);
                }
            }
        }

        return itemCodeData;
    }

    public async Task<List<GetCAMData>> GetItemCodeCAM(CAMSearch itemData)
    {
        using var connection = this.GetConnection();
        var startDates = itemData.FromDate ?? DateTime.MinValue;
        var endDates = itemData.ToDate ?? DateTime.MaxValue;
        var formattedStartDate = startDates.ToString("yyyy/MM/dd");
        var formattedEndDate = endDates.ToString("yyyy/MM/dd");

        if (itemData.MarketplaceTemplateId == "0")
        {
            throw new Exception("There is No data for Selected Marketplace,Division and  Category");
        }

        var query = "SELECT ic.code AS itemcode, ic.eancode, ic.uidcode as Uid, v.planogram_family_name as PlanogramName, ic.sourcedatecreated as datecreated, p.statusfilter, p.columndata->>'Division Code' as Division, p.columndata->>'Dept Code' as Deptcode " +
    "FROM itemcodestaging ic " +
    "LEFT JOIN product p ON ic.Uidcode = p.uid " +
    "LEFT JOIN vw_uid v ON p.uid = v.uid " +
    "WHERE v.isactive = true AND to_char(ic.sourcedatecreated::date::timestamp with time zone, 'yyyy/MM/dd') BETWEEN @FormattedStartDate AND @FormattedEndDate ";

        if (!string.IsNullOrEmpty(itemData.PlanogramName))
        {
            var planogramName = itemData.PlanogramName.Replace("'", "''");
            query += $"AND LOWER(p.columndata->>'Planogram Family Name') LIKE LOWER('%{planogramName}%') ";
        }

        if (itemData.Status == "1")
        {
            query += "AND p.statusfilter = '1' ";
        }
        else if (itemData.Status == "2")
        {
            query += "AND (p.statusfilter ='0' or p.statusfilter is null) ";
        }
        else
        {
            query += "AND (p.statusfilter is not null or p.statusfilter is null) ";
        }

        if (!string.IsNullOrEmpty(itemData.Division))
        {
            string[] divisionIds = itemData.Division.Split(',');
            query += $"AND p.columndata->>'Division Code' IN ('{string.Join("','", divisionIds)}') ";
        }

        if (!string.IsNullOrEmpty(itemData.DeparmentIDs))
        {
            string[] departmentIds = itemData.DeparmentIDs.Split(',');
            query += $"AND p.columndata->>'Dept Code' IN ('{string.Join("','", departmentIds)}') ";
        }

        query += "UNION ALL " +
            "SELECT NULL AS itemcode,NULL AS Division,NULL AS Deptcode, NULL AS datecreated,NULL AS PlanogramName, NULL AS eancode, NULL AS Uid, NULL AS status " +
            "FROM productattribute pa " +
            "JOIN marketplacetemplateattribute mta ON pa.id = mta.productattributeid " +
            "JOIN marketplacetemplate mt ON mta.marketplacetemplateid = mt.id ";
        if (!string.IsNullOrEmpty(itemData.MarketplaceTemplateId))
        {
            string[] marketplaceTemplateIdIds = itemData.MarketplaceTemplateId.Split(',');
            query += $"WHERE mt.id IN ('{string.Join("','", marketplaceTemplateIdIds)}') ";
        }

        query += "ORDER BY datecreated DESC";
        object parameters = new
        {
            FormattedStartDate = formattedStartDate,
            FormattedEndDate = formattedEndDate,
            MarketplaceTemplateId = itemData.MarketplaceTemplateId
        };
        var results = await connection.QueryAsync(query, parameters);
        var itemCodeData = new List<GetCAMData>();

        foreach (var item in results)
        {
            if (item.statusfilter != null)
            {
                if (itemData.Status == "1")
                {
                    if (item.statusfilter == itemData.Status)
                    {
                        string itemCodeValue = item.itemcode != null ? item.itemcode.ToString() : string.Empty;
                        var itemCodeRow = new GetCAMData
                        {
                            ItemCode = itemCodeValue,
                            Statusfilter = new List<string> { item.statusfilter },
                            DateCreated = item.datecreated,
                            Division = item.division,
                            Deptcode = item.deptcode,
                            Eancode = item.eancode,
                            Uid = item.uid,
                            PlanogramName = item.planogramname
                        };

                        itemCodeData.Add(itemCodeRow);
                    }
                }
                else
                {
                    string itemCodeValue = item.itemcode != null ? item.itemcode.ToString() : string.Empty;
                    var itemCodeRow = new GetCAMData
                    {
                        ItemCode = itemCodeValue,
                        Statusfilter = new List<string> { item.statusfilter },
                        DateCreated = item.datecreated,
                        Division = item.division,
                        Deptcode = item.deptcode,
                        Eancode = item.eancode,
                        Uid = item.uid,
                        PlanogramName = item.planogramname
                    };

                    itemCodeData.Add(itemCodeRow);
                }
            }
            else
            {
                if (item.itemcode != null)
                {
                    string itemCodeValue = item.itemcode != null ? item.itemcode.ToString() : string.Empty;
                    item.statusfilter = "0";
                    var itemCodeRow = new GetCAMData
                    {
                        ItemCode = itemCodeValue,
                        Statusfilter = new List<string> { item.statusfilter },
                        DateCreated = item.datecreated,
                        Division = item.division,
                        Deptcode = item.deptcode,
                        Eancode = item.eancode,
                        Uid = item.uid,
                        PlanogramName = item.planogramname
                    };

                    itemCodeData.Add(itemCodeRow);
                }
            }
        }

        return itemCodeData;
    }

    public async Task<List<ItemCodeSearch>> GetItemCodeSearch(long? marketplaceTemplateId)
    {
        using var connection = this.GetConnection();
        var query = "SELECT A.CODE,A.DATECREATED,B.STATUSFILTER FROM itemcodestaging A JOIN PRODUCT B ON A.UIDCODE=B.UID";
        query += " UNION ALL " +
            "SELECT NULL AS itemcode, NULL AS datecreated, NULL AS status " +
            "FROM productattribute pa " +
            "JOIN marketplacetemplateattribute mta ON pa.id = mta.productattributeid " +
            "JOIN marketplacetemplate mt ON mta.marketplacetemplateid = mt.id " +
            "WHERE mt.id = @MarketplaceTemplateId";
        var parameters = new
        {
            MarketplaceTemplateId = marketplaceTemplateId,
        };
        var results = await connection.QueryAsync(query, parameters);

        var itemCodeData = new List<ItemCodeSearch>();

        foreach (var item in results)
        {
            if (item.code != null)
            {
                var itemCodeRow = new ItemCodeSearch
                {
                    ItemCode = item.code != null ? item.code.ToString() : string.Empty,
                    Statusfilter = new List<string> { item.statusfilter },
                    DateCreated = item.datecreated
                };
                itemCodeData.Add(itemCodeRow);
            }
        }

        return itemCodeData;
    }

    public async Task LinkAttributeToTemplate(TemplateAttribute templateAttribute)
    {
        var action = this.GetAction<TemplateAttribute>();
        await action.AddOrUpdate(templateAttribute);
    }

    public async Task LinkAttributesToTemplate(List<TemplateAttribute> templateAttributes)
    {
        using var connection = this.GetConnection();
        using var transaction = connection.BeginTransaction();
        var userId = await this.GetUserId();
        templateAttributes.ForEach(x =>
        {
            x.CreatedBy = userId;
            x.UpdatedBy = userId;
            x.DateCreated = DateTimeOffset.UtcNow;
            x.DateModified = DateTimeOffset.UtcNow;
        });
        transaction.BulkInsert(nameof(TemplateAttribute), templateAttributes.ToArray().GetDataTableFromObjects());
        transaction.Commit();
    }

    public async Task RemoveAttributeFromTemplate(long templateAttributeId)
    {
        var action = this.GetAction<TemplateAttribute>();
        await action.Remove(templateAttributeId);
        await this._cache.Clear();
    }

    public async Task RemoveAttributesFromTemplate(long[] templateAttributeIds)
    {
        var action = this.GetAction<TemplateAttribute>();
        await action.Remove(templateAttributeIds);
        await this._cache.Clear();
    }

    public async Task<Template> GetTemplate(long templateId)
    {
        return (await _cache.GetTemplates()).FirstOrDefault(x => x.Id == templateId);
    }

    public async Task<IEnumerable<Template>> GetTemplates()
    {
        return await this.GetAll(" where isactive = true", new
        {
            userId = await this.GetUserId()
        });
    }

    public async Task<IEnumerable<Template>> GetUserTemplates()
    {
        using var connection = this.GetConnection();
        return await connection.QueryAsync<Template>(
            "select * from vw_templates where isactive = true and appuserId = @userId order by \"order\"", new
            {
                userId = await this.GetUserId()
            });
    }

    public async Task<IEnumerable<TemplateRole>> GetTemplateRoles(long templateId)
    {
        var action = this.GetAction<TemplateRole>();
        return await action.GetAll(" where templateId = @templateId and isactive = true", new
        {
            templateId
        });
    }

    public async Task ActivateTemplate(long[] templateIds)
    {
        await this.Activate(templateIds);
        await this._cache.Clear();
    }

    public async Task PublishTemplate(long[] templateIds)
    {
        var allTemplates = await _cache.GetTemplates();
        var allTemplateAttributes = (await Task.WhenAll(allTemplates
                .GroupBy(e => e.Id)
                .Select(e => e.First())
                .Select(async e => new { e.Id, Data = await _cache.GetTemplateAttributes(e.Id) })))
            .ToDictionary(e => e.Id, e => e.Data);
        var divisions = await _cache.GetMaster<Division>();
        foreach (var templateId in templateIds)
        {
            var currentTemplate = allTemplates.Find(x => x.Id == templateId);
            var currentTemplateAttributes = allTemplateAttributes[templateId].Select(x => x.Name);
            var divisionTemplates = allTemplates.Where(x => x.DivisionId == currentTemplate.DivisionId);
            var division = divisions.First(x => x.Id == currentTemplate.DivisionId);
            foreach (var divisionTemplate in divisionTemplates.Where(x => x.Id != templateId))
            {
                var uidKeys = division.UidKeys?.Split(",") ?? Array.Empty<string>();
                var attributes = allTemplateAttributes[divisionTemplate.Id].Select(x => x.Name);
                if (attributes.Where(x => !uidKeys.Contains(x)).Any(x => currentTemplateAttributes.Contains(x)))
                {
                    throw new HandledException(
                        $"Template: {currentTemplate.Name} has attributes used in {divisionTemplate.Name}");
                }
            }
        }

        foreach (var templateId in templateIds)
        {
            await this.DoUpdate(
                new
                {
                    id = templateId,
                    templateStatusId = 2 // published
                }, nameof(Template));
        }

        await this._cache.Clear();
    }

    public async Task<byte[]> BulkDownload(long marketplacetemplateid)
    {
        using var connection = this.GetConnection();
        var query = "SELECT mta.marketplaceid, mp.name, mta.productdepartmentid, pd.name as departmentname, mta.productattributeid, pa.name as productattributename, mta.marketplaceattribute, pa.datatype, mta.columnorder FROM marketplacetemplateattribute mta" +
            " INNER JOIN marketplace mp ON mta.marketplaceid = mp.id" +
            " INNER JOIN productdepartment pd ON mta.productdepartmentid = pd.id" +
            " INNER JOIN productattribute pa ON mta.productattributeid = pa.id " +
            "WHERE mta.marketplacetemplateid = @marketplacetemplateid order by pa.name";
        var parameters = new
        {
            marketplacetemplateid = marketplacetemplateid
        };
        var dataTypeMapping = new Dictionary<long, string>
        {
            { 0, "Text" },
            { 10, "Number" },
            { 20, "DateTime" },
            { 30, "Lookup" },
            { 40, "Boolean" },
        };

        var results = await connection.QueryAsync(query, parameters);
        if (results.Count() == 0)
        {
            throw new Exception("There is No Data for Selected MarketPlace and Category ");
        }

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Results");

        var columnHeaders = new string[] { "Marketplace Id", "Marketplace Name", "Department Id", "Department", "Attribute Id", "Attribute Name", "Data Type", "Marketplace Attribute", "Market Place Attribute Order" };
        for (var i = 0; i < columnHeaders.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = columnHeaders[i];
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            worksheet.Cells[1, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
        }

        var row = 2;
        foreach (var result in results)
        {
            worksheet.Cells[row, 1].Value = result.marketplaceid;
            worksheet.Cells[row, 2].Value = result.name;
            worksheet.Cells[row, 3].Value = result.productdepartmentid;
            worksheet.Cells[row, 4].Value = result.departmentname;
            worksheet.Cells[row, 5].Value = result.productattributeid;
            worksheet.Cells[row, 6].Value = result.productattributename;
            worksheet.Cells[row, 7].Value = result.datatype;
            worksheet.Cells[row, 8].Value = result.marketplaceattribute;
            worksheet.Cells[row, 9].Value = result.columnorder;
            worksheet.Cells[row, 9].Style.Numberformat.Format = "@";
            worksheet.Cells[row, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

            long? dataType = result.datatype;
            if (dataType.HasValue && dataTypeMapping.TryGetValue(dataType.Value, out string dataTypeText))
            {
                worksheet.Cells[row, 7].Value = dataTypeText;
            }

            row++;
        }

        var dataTypeColumn = worksheet.Cells[2, 7, row - 1, 7];
        var dataValidation = dataTypeColumn.DataValidation.AddListDataValidation();
        dataValidation.Formula.ExcelFormula = "\"Text,Number,DateTime,Lookup,Boolean\"";
        dataValidation.ShowErrorMessage = false;
        dataValidation.ErrorStyle = ExcelDataValidationWarningStyle.stop;

        worksheet.Cells.AutoFitColumns();

        var byteArray = package.GetAsByteArray();
        return byteArray;
    }

    public async Task<List<List<string>>> MarketPlaceupload(MarketPlaceUpload marketPlaceUpload)
    {
        using var connection = this.GetConnection();
        var templateId = marketPlaceUpload.MarketplacetemplateId;
        var tempFile = await _blobRepo.DownloadFile(marketPlaceUpload.BlobUrl);
        using var wb = new XLWorkbook(tempFile, XLEventTracking.Disabled);
        var ws = wb.Worksheet(1);
        var dataTable = ws.Tables.Any() ? ws.Tables.First().AsNativeDataTable() : ws.RangeUsed().AsTable().AsNativeDataTable();

        var query = "SELECT mta.marketplaceid, mp.name, mta.productdepartmentid, pd.name as departmentname, mta.productattributeid, pa.name as productattributename, mta.marketplaceattribute, mta.datatype, mta.columnorder " +
            "FROM marketplacetemplateattribute mta " +
            "INNER JOIN marketplace mp ON mta.marketplaceid = mp.id " +
            "INNER JOIN productdepartment pd ON mta.productdepartmentid = pd.id " +
            "INNER JOIN productattribute pa ON mta.productattributeid = pa.id " +
            "WHERE mta.marketplacetemplateid = @MarketplaceTemplateId";

        var parameters = new { MarketplaceTemplateId = marketPlaceUpload.MarketplacetemplateId };
        var tableData = await connection.QueryAsync(query, parameters);

        var combinedData = new List<List<string>>();

        if (tableData != null)
        {
            foreach (var row in tableData)
            {
                var rowData = new List<string>();

                var marketplaceId = row.marketplaceid?.ToString();
                var marketplacename = row.name;
                var departmentId = row.productdepartmentid?.ToString();
                var departmentname = row.departmentname;
                var attributeId = row.productattributeid?.ToString();
                var attributename = row.productattributename;
                var marketplaceattribute = row.marketplaceattribute;
                var datatype = row.datatype?.ToString();
                var columnorder = row.columnorder?.ToString();

                rowData.Add(marketplaceId);
                rowData.Add(marketplacename);
                rowData.Add(departmentId);
                rowData.Add(departmentname);
                rowData.Add(attributeId);
                rowData.Add(attributename);
                rowData.Add(datatype);
                rowData.Add(marketplaceattribute);
                rowData.Add(columnorder);

                combinedData.Add(rowData);
            }
        }

        if (dataTable != null)
        {
            foreach (DataRow row in dataTable.Rows)
            {
                var rowData = new List<string>();

                foreach (DataColumn column in dataTable.Columns)
                {
                    var value = row[column]?.ToString();
                    rowData.Add(value);
                }

                combinedData.Add(rowData);
            }
        }

        var modifiedValues = new List<string>();

        foreach (var dataRow in combinedData)
        {
            var dbRow = tableData.FirstOrDefault(
                r => r.marketplaceid?.ToString() == dataRow[0] &&
                     r.productdepartmentid?.ToString() == dataRow[2] &&
                     r.productattributeid?.ToString() == dataRow[4]);
            var dbMarketplaceName = dbRow?.name;
            var dbDepartmentName = dbRow?.departmentname;
            var dbAttributeName = dbRow?.productattributename;

            if (dbRow != null)
            {
                var dataMarketplaceName = dataRow[1];
                var dataDepartmentName = dataRow[3];
                var dataAttributeName = dataRow[5];

                if (dbMarketplaceName != dataMarketplaceName)
                {
                    modifiedValues.Add($"Marketplace name: '{dataMarketplaceName}'");
                }

                if (dbDepartmentName != dataDepartmentName)
                {
                    modifiedValues.Add($"Department name: '{dataDepartmentName}'");
                }

                if (dbAttributeName != dataAttributeName)
                {
                    modifiedValues.Add($"Attribute name: '{dataAttributeName}'");
                }

                for (int i = 1; i < dataRow.Count; i++)
                {
                    if (i != 6 && i != 7)
                    {
                        var dbValue = dbRow.GetValue(i)?.ToString();
                        var modifiedValue = dataRow[i];

                        if (dbValue != null && modifiedValue != null && dbValue != modifiedValue)
                        {
                            modifiedValues.Add(modifiedValue);
                        }
                    }
                }
            }
            else
            {
                for (int i = 1; i < dataRow.Count; i++)
                {
                    if (i != 6 && i != 7)
                    {
                        var modifiedValue = dataRow[i];

                        if (modifiedValue != null)
                        {
                            modifiedValues.Add(modifiedValue);
                        }
                    }
                }
            }
        }

        if (modifiedValues.Count > 0)
        {
            throw new Exception("Values are Mismacthed");
        }

        bool anyRowWithValue = dataTable != null && dataTable.Rows.Cast<DataRow>().Any(row =>
        {
            var marketplaceAttribute = row[7]?.ToString();
            var dataType = row[6]?.ToString();
            var columnOrder = row[8]?.ToString();
            return !string.IsNullOrEmpty(marketplaceAttribute) || !string.IsNullOrEmpty(dataType);
        });

        if (!anyRowWithValue)
        {
            throw new Exception("At least one row must filled with values");
        }

        string firstDataType = null;
        string firstMarketplaceAttribute = null;
        var dataTypeMapping = new Dictionary<string, int>
        {
            { "Text", 0 },
            { "Number", 10 },
            { "DateTime", 20 },
            { "Lookup", 30 },
            { "Boolean", 40 }
        };
        var mapping = new Dictionary<string, AttrDataType>
        {
            { "Text", AttrDataType.Text },
            { "Number", AttrDataType.Number },
            { "DateTime", AttrDataType.DateTime },
            { "Lookup", AttrDataType.Lookup },
            { "Boolean", AttrDataType.Boolean }
        };
        var data = $"SELECT name, datatype FROM productattribute where divisionid = {marketPlaceUpload.Divisioid} and isactive = true ";
        var productAttributes = await connection.QueryAsync<ProductAttribute>(data);

        var productAttributeDictionary = new Dictionary<string, AttrDataType>();
        var attributesCount = productAttributes.Count();

        foreach (var attribute in productAttributes)
        {
            string attributeName = attribute.Name;
            AttrDataType attributeDataType = attribute.DataType ?? AttrDataType.Text;
            productAttributeDictionary[attributeName] = attributeDataType;
        }

        bool dataTypeProvided = false;
        var validationErrors = new List<string>();
        var updatedProductAttributeIds = new HashSet<string>();
        string[] columnOrderCheck = new string[0];

        foreach (DataRow row in dataTable.Rows)
        {
            var productAttributeId = row[4]?.ToString();
            var attributeName = row[5]?.ToString();
            var dataType = row[6]?.ToString();
            var marketplaceAttribute = row[7]?.ToString()?.Trim();
            var columnOrder = row[8]?.ToString();

            if (string.IsNullOrEmpty(columnOrder) && string.IsNullOrEmpty(marketplaceAttribute))
            {
                continue;
            }

            if (!productAttributeDictionary.TryGetValue(attributeName, out AttrDataType productAttributeDbDataType))
            {
                validationErrors.Add($"Invalid attribute name '{attributeName}'");
                continue;
            }

            if (string.IsNullOrEmpty(dataType))
            {
                validationErrors.Add("dataType cannot be empty.");
                continue;
            }

            if (Enum.TryParse<AttrDataType>(dataType, out AttrDataType excelDataType))
            {
                if (excelDataType != productAttributeDbDataType)
                {
                    validationErrors.Add($"Data type mismatch for attribute '{attributeName}' Expected '{productAttributeDbDataType}', but got '{excelDataType}'");
                    continue;
                }
            }
            else
            {
                validationErrors.Add($"Invalid data type value '{dataType}'");
                continue;
            }

            if (string.IsNullOrEmpty(marketplaceAttribute) && !string.IsNullOrEmpty(columnOrder))
            {
                if (!validationErrors.Contains("MarketplaceAttribute cannot be empty if MarketPlaceAttributeOrder is provided."))
                {
                    validationErrors.Add("MarketplaceAttribute cannot be empty if MarketPlaceAttributeOrder is provided.");
                    continue;
                }

                continue;
            }

            if (!string.IsNullOrEmpty(columnOrder))
            {
                int columnOrderInt;
                if (int.TryParse(columnOrder, out columnOrderInt))
                {
                    // Check if the number is within the specified range
                    if (columnOrderInt <= 0 || columnOrderInt > attributesCount)
                    {
                        if (!validationErrors.Contains($"MarketPlaceAttributeOrder should be between 1 to {attributesCount}."))
                        {
                            validationErrors.Add($"MarketPlaceAttributeOrder should be between 1 to {attributesCount}.");
                            continue;
                        }

                        continue;
                    }
                }
                else
                {
                    if (!validationErrors.Contains("MarketPlaceAttributeOrder should be in number."))
                    {
                        validationErrors.Add("MarketPlaceAttributeOrder should be in number.");
                        continue;
                    }

                    continue;
                }
            }

            if (!string.IsNullOrEmpty(columnOrder))
            {
                if (Array.Exists(columnOrderCheck, element => element == columnOrder))
                {
                    if (!validationErrors.Contains("Duplicate MarketPlaceAttributeOrders found."))
                    {
                        validationErrors.Add("Duplicate MarketPlaceAttributeOrders found.");
                        continue;
                    }

                    continue;
                }

                columnOrderCheck = columnOrderCheck.Prepend(columnOrder).ToArray();
            }

            if (!string.IsNullOrEmpty(dataType) && dataTypeMapping.TryGetValue(dataType, out int mappedDataType))
            {
                dataType = mappedDataType.ToString();
            }
            else if (!IsValidDataType(dataType))
            {
                validationErrors.Add($"Invalid dataType value '{dataType}'");
                continue;
            }

            string updateQuery;

            if (marketplaceAttribute.Contains("'") || marketplaceAttribute.Contains("\""))
            {
                string escapedMarketplaceAttribute = marketplaceAttribute.Replace("'", "''").Replace("\"", "\"\"");
                updateQuery = $"UPDATE marketplacetemplateattribute SET DataType = {dataType}, Marketplaceattribute = '{escapedMarketplaceAttribute}', Columnorder = '{columnOrder}' WHERE productattributeid = {productAttributeId} AND MarketplaceTemplateId = {marketPlaceUpload.MarketplacetemplateId}";
            }
            else
            {
                updateQuery = $"UPDATE marketplacetemplateattribute SET DataType = {dataType}, Marketplaceattribute = '{marketplaceAttribute}', Columnorder = '{columnOrder}' WHERE productattributeid = {productAttributeId} AND MarketplaceTemplateId = {marketPlaceUpload.MarketplacetemplateId}";
            }

            var results = await connection.ExecuteAsync(updateQuery);

            updatedProductAttributeIds.Add(productAttributeId);
        }

        foreach (DataRow row in dataTable.Rows)
        {
            var productAttributeId = row[4]?.ToString();
            var dataType = row[6]?.ToString();
            var marketplaceAttribute = row[7]?.ToString();
            var columnOrder = row[8]?.ToString();

            if (string.IsNullOrEmpty(marketplaceAttribute))
            {
                var removeQuery = $"UPDATE marketplacetemplateattribute SET Marketplaceattribute = NULL WHERE productattributeid = '{productAttributeId}' and MarketplaceTemplateId= {marketPlaceUpload.MarketplacetemplateId}";
                var results = await connection.ExecuteAsync(removeQuery);
                updatedProductAttributeIds.Add(productAttributeId);
            }

            if (string.IsNullOrEmpty(columnOrder))
            {
                var removeQuery = $"UPDATE marketplacetemplateattribute SET ColumnOrder = NULL WHERE productattributeid = '{productAttributeId}' and MarketplaceTemplateId= {marketPlaceUpload.MarketplacetemplateId}";
                var results = await connection.ExecuteAsync(removeQuery);
                updatedProductAttributeIds.Add(productAttributeId);
            }
        }

        if (validationErrors.Count > 0)
        {
            throw new Exception(string.Join("\n", validationErrors));
        }

        var dept = $"select productdepartmentid,productattributeid from MarketplaceTemplateAttribute WHERE MarketplaceTemplateId= {marketPlaceUpload.MarketplacetemplateId} limit 1";
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
                                             $"WHERE marketplaceattribute IS NOT NULL and  marketplacetemplateid = {marketPlaceUpload.MarketplacetemplateId}) " +
                                              "ORDER BY id ASC";

        var marketplaceAttributeNames = new List<string>();
        var result = await connection.QueryAsync<ProductAttribute>(marketplaceAttributeData, new { MarketplaceTemplateId = marketPlaceUpload.MarketplacetemplateId });
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

        return combinedData;
    }

    public async Task<byte[]> DownloadMasterDataAsExcel(long masterDataID)
    {
        using var connection = this.GetConnection();

        using (var package = new ExcelPackage())
        {
            var worksheetTemplates = package.Workbook.Worksheets.Add(GetWorksheetName(masterDataID));

            switch (masterDataID)
            {
                case 1:
                    var master = await connection.QueryAsync<MerchandiseHierarchy>("select distinct business,business_name,division,div_name,group_no,group_name,dept,dept_name,class,class_name,subclass,sub_name,active_ind from merchandise_hierarchy");
                    LoadDataAndAddTable(worksheetTemplates, master);
                    break;

                case 2:
                    var templates = await connection.QueryAsync<TemplateData>("SELECT name as TemplateName,DivisionId,isactive FROM template where isactive = 'true'");
                    LoadDataAndAddTable(worksheetTemplates, templates);
                    break;
                case 3:
                    var classname = await connection.QueryAsync<ClassNameMasterData>("select name as classname,classcode, department as departmentcode, isactive from productclass");
                    LoadDataAndAddTable(worksheetTemplates, classname);
                    break;
                case 4:
                    var department = await connection.QueryAsync<DepartmentMasterData>("select name as departmentname, departmentcode, groupcode, isactive from productdepartment where departmentcode <> 0");
                    LoadDataAndAddTable(worksheetTemplates, department);
                    break;
                case 5:
                    var division = await connection.QueryAsync<DivisionMasterData>("select  name as divisionname, code as divisioncode, isactive from division");
                    LoadDataAndAddTable(worksheetTemplates, division);
                    break;
                case 6:
                    var group = await connection.QueryAsync<GroupData>("select  name as groupname, groupcode, division as divisioncode, isactive from productgroup");
                    LoadDataAndAddTable(worksheetTemplates, group);
                    break;
                case 7:
                    var subclass = await connection.QueryAsync<SubClassData>("SELECT name as subclassname,subclasscode, department as departmentcode, classid as classcode,   isactive from productsubclass");
                    LoadDataAndAddTable(worksheetTemplates, subclass);
                    break;
                case 8:
                    var brand = await connection.QueryAsync<BrandMasterData>("select name as brandname, brandcode, isactive from brand");
                    LoadDataAndAddTable(worksheetTemplates, brand);
                    break;
                case 9:
                    var business = await connection.QueryAsync<BusinessMasterData>("select  name as businessname, businesscode, isactive from business");
                    LoadDataAndAddTable(worksheetTemplates, business);
                    break;
                case 10:
                    var attributes = await connection.QueryAsync<AttributeMasterData>("SELECT pa.name AS attributename, pa.datatype, pa.isrequired, pa.isactive, d.name AS Division FROM productattribute pa JOIN division d ON CAST(pa.divisionid AS text) = d.code");
                    LoadDataAndAddTable(worksheetTemplates, attributes);
                    break;
                case 11:
                    var productcolour = await connection.QueryAsync<ProductColourData>("SELECT p.name AS ProductColourName, p.code AS Code, p.isactive, p.divisionid FROM mtrproductcolour p ");
                    LoadDataAndAddTable(worksheetTemplates, productcolour);
                    break;
                case 12:
                    var setpcs = await connection.QueryAsync<SetpcsData>("select name as setpcsname,Code,isactive from mtrsetpcs order by code asc");
                    LoadDataAndAddTable(worksheetTemplates, setpcs);
                    break;
                case 13:
                    var leadtime = await connection.QueryAsync<LeadTimeData>("select name as leadtimename,isactive from mtrleadtime");
                    LoadDataAndAddTable(worksheetTemplates, leadtime);
                    break;
                case 14:
                    var itemshape = await connection.QueryAsync<ItemShapeData>("select name as itemshapename,isactive from mtritemshape");
                    LoadDataAndAddTable(worksheetTemplates, itemshape);
                    break;
                case 15:
                    var packagingType = await connection.QueryAsync<PackagingTypeData>("SELECT p.name AS PackagingTypeName, p.code AS PackagingTypeCode, p.isactive, p.divisionid FROM mtrpackagingtype p ");
                    LoadDataAndAddTable(worksheetTemplates, packagingType);
                    break;
                case 16:
                    var finish = await connection.QueryAsync<FinishData>("select name as finishname,isactive from mtrfinish");
                    LoadDataAndAddTable(worksheetTemplates, finish);
                    break;
                case 17:
                    var furnitureMaterial = await connection.QueryAsync<PrimaryFurnitureMaterialData>("select name as primaryfurniturematerialname,isactive from mtrprimaryfurniturematerial");
                    LoadDataAndAddTable(worksheetTemplates, furnitureMaterial);
                    break;
                case 18:
                    var secondaryFurnitureMaterial = await connection.QueryAsync<SecondaryFurnitureMaterialData>("select name as secondaryfurniturematerialname,isactive from mtrsecondaryfurniturematerial");
                    LoadDataAndAddTable(worksheetTemplates, secondaryFurnitureMaterial);
                    break;
                case 19:
                    var producttype1 = await connection.QueryAsync<Producttype1Data>("select name as producttype1name,isactive from mtrproducttype1");
                    LoadDataAndAddTable(worksheetTemplates, producttype1);
                    break;
                case 20:
                    var productSubType1 = await connection.QueryAsync<ProductSubType1Data>("select name as productsubtype1name,isactive from mtrproductsubtype1");
                    LoadDataAndAddTable(worksheetTemplates, productSubType1);
                    break;
                case 21:
                    var producttype2 = await connection.QueryAsync<Producttype2Data>("select name as producttype2name,isactive from mtrproducttype2");
                    LoadDataAndAddTable(worksheetTemplates, producttype2);
                    break;
                case 22:
                    var productSubType2 = await connection.QueryAsync<ProductSubType2Data>("select name as productsubtype2name,isactive from mtrproductsubtype2");
                    LoadDataAndAddTable(worksheetTemplates, productSubType2);
                    break;
                case 23:
                    var season = await connection.QueryAsync<SeasonData>("SELECT s.name AS SeasonName, s.code AS SeasonCode, s.isactive, s.divisionid FROM season s ");
                    LoadDataAndAddTable(worksheetTemplates, season);
                    break;
                case 24:
                    var productPurpose = await connection.QueryAsync<ProductPurposeData>("SELECT p.name AS ProductPurposeName, p.code AS Code, p.isactive, p.divisionid FROM productpurpose p ");
                    LoadDataAndAddTable(worksheetTemplates, productPurpose);
                    break;
                case 25:
                    var purchasetype = await connection.QueryAsync<PurchaseTypeData>("SELECT p.name AS PurchaseTypeName, p.code AS Code, p.isactive, p.divisionid FROM purchasetype p ");
                    LoadDataAndAddTable(worksheetTemplates, purchasetype);
                    break;
                case 26:
                    var priceGroup = await connection.QueryAsync<PriceGroupingData>("SELECT p.name AS PriceGroupingName, p.code AS Code, p.isactive, p.divisionid FROM pricegrouping p ");
                    LoadDataAndAddTable(worksheetTemplates, priceGroup);
                    break;
                case 27:
                    var itemType = await connection.QueryAsync<ItemTypeCodeData>("SELECT i.name AS ItemTypeCodeName, i.code AS Code, i.isactive, i.divisionid FROM mtritemtypecode i ");
                    LoadDataAndAddTable(worksheetTemplates, itemType);
                    break;
                case 28:
                    var productclassificationcode = await connection.QueryAsync<ProductClassificationCodeData>("SELECT p.name AS ProductClassificationCodeName, p.code AS Code, p.isactive, p.divisionid FROM mtrproductclassificationcode p ");
                    LoadDataAndAddTable(worksheetTemplates, productclassificationcode);
                    break;
                case 29:
                    var plcCode = await connection.QueryAsync<PLCCodeData>("SELECT p.name AS PLCCodeName, p.code AS Code, p.isactive, p.divisionid FROM mtrplccode p ");
                    LoadDataAndAddTable(worksheetTemplates, plcCode);
                    break;
                case 30:
                    var uniqueidstatus = await connection.QueryAsync<UniqueIdStatusData>("select name as UniqueIdStatusName ,Code,IsActive from mtruniqueidstatus");
                    LoadDataAndAddTable(worksheetTemplates, uniqueidstatus);
                    break;
                case 31:
                    var productgrouping = await connection.QueryAsync<ProductGroupingData>("SELECT p.name AS ProductGroupingName, p.code AS Code, p.isactive, p.divisionid FROM mtrproductgrouping p ");
                    LoadDataAndAddTable(worksheetTemplates, productgrouping);
                    break;
                case 32:
                    var primarysecondarysku = await connection.QueryAsync<PrimarySecondarySKUData>("select name as PrimarySecondaryName ,code,isactive from mtrprimarysecondarysku");
                    LoadDataAndAddTable(worksheetTemplates, primarysecondarysku);
                    break;
                case 33:
                    var countryoforigin = await connection.QueryAsync<CountryOfOriginData>("select name as Country_Of_Origin_Code ,description as Country_Of_Origin_Name ,isactive from mtrcountryoforigin");
                    LoadDataAndAddTable(worksheetTemplates, countryoforigin);
                    break;
                case 34:
                    var currency = await connection.QueryAsync<CurrencyData>("select name as Currency_Code ,description as Currency_Name ,isactive from mtrcurrency");
                    LoadDataAndAddTable(worksheetTemplates, currency);
                    break;
                case 35:
                    var taxcategory = await connection.QueryAsync<TaxCategoryData>("select name as TaxCategoryName,Code ,IsActive from mtrtaxcategory");
                    LoadDataAndAddTable(worksheetTemplates, taxcategory);
                    break;
                case 36:
                    var innercarton = await connection.QueryAsync<InnerCartonCodeData>("SELECT i.name AS InnerCartonCodeName, i.code AS Code, i.isactive, i.divisionid FROM mtrinnercartoncode i ");
                    LoadDataAndAddTable(worksheetTemplates, innercarton);
                    break;
                case 37:
                    var mastercarton = await connection.QueryAsync<MasterCartonCodeData>("SELECT m.name AS MasterCartonCodeName, m.code AS Code, m.isactive, m.divisionid FROM mtrmastercartoncode m ");
                    LoadDataAndAddTable(worksheetTemplates, mastercarton);
                    break;
                case 38:
                    var diff_group = await connection.QueryAsync<DiffGroupData>("select name as DiffGroupName,Code ,IsActive from mtrdiffgroup2val");
                    LoadDataAndAddTable(worksheetTemplates, diff_group);
                    break;
                case 39:
                    var madeofvalue = await connection.QueryAsync<MadeOfValueData>("select name as MadeOfValueName,Code ,IsActive from mtrmadeofvalue");
                    LoadDataAndAddTable(worksheetTemplates, madeofvalue);
                    break;
                case 40:
                    var suppliercode = await connection.QueryAsync<SupplierCodeData>("SELECT s.name AS SupplierCodeName, s.code AS Code, s.isactive, s.divisionid FROM mtrsuppliercode s ");
                    LoadDataAndAddTable(worksheetTemplates, suppliercode);
                    break;
                case 41:
                    var hsnCode = await connection.QueryAsync<HsnCodeData>("select Code, name as HsnName,IsActive from mtrhsncode");
                    LoadDataAndAddTable(worksheetTemplates, hsnCode);
                    break;
                case 42:
                    var brandType = await connection.QueryAsync<BrandTypeData>("select Code, name as BrandTypeName,IsActive from mtrbrandtype");
                    LoadDataAndAddTable(worksheetTemplates, brandType);
                    break;
                case 43:
                    var producttype = await connection.QueryAsync<ProductTypeHierarchy>("select div_code,div_name,dept_code,dept_name,class_code,class_name,subclass_code,sub_name as Subclass_Name,producttype1 as Product_Type_1,productsubtype1 as Product_Subtype_1,producttype2 as Product_Type_2,productsubtype2 as Product_Subtype_2 from mtrproductype_subtype");
                    LoadDataAndAddTable(worksheetTemplates, producttype);
                    break;
                case 44:
                    var rmsseason = await connection.QueryAsync<RMSSeason>("select name,code,isactive from mtrrmsseasoncode");
                    LoadDataAndAddTable(worksheetTemplates, rmsseason);
                    break;
                case 45:
                    var warehouse = await connection.QueryAsync<WareHouse>("select name,code,isactive from warehouse");
                    LoadDataAndAddTable(worksheetTemplates, warehouse);
                    break;
                default:
                    throw new Exception("MasterData Not Found");
                    break;
            }

            return package.GetAsByteArray();
        }
    }

    public async Task<IEnumerable> GetMasterData(long masterDataId)
    {
        using var connection = this.GetConnection();
        switch (masterDataId)
        {
            case 1:
                var master = await connection.QueryAsync<MerchandiseHierarchy>("select distinct business,business_name,division,div_name,group_no,group_name,dept,dept_name,class,class_name,subclass,sub_name,active_ind from merchandise_hierarchy");
                return master.ToList();

            case 2:
                var templates = await connection.QueryAsync<TemplateData>("SELECT name as TemplateName,DivisionId,isactive FROM template where isactive = 'true'");
                return templates.ToList();

            case 3:
                var classname = await connection.QueryAsync<ClassNameMasterData>("select name as classname,classcode, department as departmentcode, isactive from productclass");
                return classname.ToList();

            case 4:
                var department = await connection.QueryAsync<DepartmentMasterData>("select name as departmentname, departmentcode, groupcode, isactive from productdepartment where departmentcode <> 0");
                return department.ToList();

            case 5:
                var division = await connection.QueryAsync<DivisionMasterData>("select  name as divisionname, code as divisioncode, isactive from division");
                return division.ToList();

            case 6:
                var group = await connection.QueryAsync<GroupData>("select  name as groupname, groupcode, division as divisioncode, isactive from productgroup");
                return group.ToList();

            case 7:
                var subclass = await connection.QueryAsync<SubClassData>("SELECT name as subclassname,subclasscode, department as departmentcode, classid as classcode,isactive from productsubclass");
                return subclass.ToList();

            case 8:
                var brand = await connection.QueryAsync<BrandMasterData>("select name as brandname, brandcode, isactive from brand");
                return brand.ToList();

            case 9:
                var business = await connection.QueryAsync<BusinessMasterData>("select  name as businessname, businesscode, isactive from business");
                return business.ToList();

            case 10:
                var attributes = await connection.QueryAsync<AttributeMasterData>("SELECT pa.name AS attributename, pa.datatype, pa.isrequired, pa.isactive, d.name AS Division FROM productattribute pa JOIN division d ON CAST(pa.divisionid AS text) = d.code;\r\n");
                return attributes.ToList();
            case 11:
                var productcolour = await connection.QueryAsync<ProductColourData>("SELECT p.name AS ProductColourName, p.code AS Code, p.isactive, p.divisionid FROM mtrproductcolour p ");
                return productcolour.ToList();
            case 12:
                var setpcs = await connection.QueryAsync<SetpcsData>("select name as setpcsname,Code,isactive from mtrsetpcs");
                return setpcs.ToList();
            case 13:
                var leadtime = await connection.QueryAsync<LeadTimeData>("select name as leadtimename,isactive from mtrleadtime");
                return leadtime.ToList();
            case 14:
                var itemshape = await connection.QueryAsync<ItemShapeData>("select name as itemshapename,isactive from mtritemshape");
                return itemshape.ToList();
            case 15:
                var packagingType = await connection.QueryAsync<PackagingTypeData>("SELECT p.name AS PackagingTypeName, p.code AS PackagingTypeCode, p.isactive, p.divisionid FROM mtrpackagingtype p ");
                return packagingType.ToList();
            case 16:
                var finish = await connection.QueryAsync<FinishData>("select name as finishname,isactive from mtrfinish");
                return finish.ToList();
            case 17:
                var furnitureMaterial = await connection.QueryAsync<PrimaryFurnitureMaterialData>("select name as primaryfurniturematerialname,isactive from mtrprimaryfurniturematerial");
                return furnitureMaterial.ToList();
            case 18:
                var secondaryFurnitureMaterial = await connection.QueryAsync<SecondaryFurnitureMaterialData>("select name as secondaryfurniturematerialname,isactive from mtrsecondaryfurniturematerial");
                return secondaryFurnitureMaterial.ToList();
            case 19:
                var producttype1 = await connection.QueryAsync<Producttype1Data>("select name as producttype1name,isactive from mtrproducttype1");
                return producttype1.ToList();
            case 20:
                var productSubType1 = await connection.QueryAsync<ProductSubType1Data>("select name as productsubtype1name,isactive from mtrproductsubtype1");
                return productSubType1.ToList();
            case 21:
                var producttype2 = await connection.QueryAsync<Producttype2Data>("select name as producttype2name,isactive from mtrproducttype2");
                return producttype2.ToList();

            case 22:
                var productSubType2 = await connection.QueryAsync<ProductSubType2Data>("select name as productsubtype2name,isactive from mtrproductsubtype2");
                return productSubType2.ToList();
            case 23:
                var season = await connection.QueryAsync<SeasonData>("SELECT s.name AS SeasonName, s.code AS SeasonCode, s.isactive, s.divisionid FROM season s ");
                return season.ToList();
            case 24:
                var productpurpose = await connection.QueryAsync<ProductPurposeData>("SELECT p.name AS ProductPurposeName, p.code AS Code, p.isactive, p.divisionid FROM productpurpose p ");
                return productpurpose.ToList();
            case 25:
                var purchasetype = await connection.QueryAsync<PurchaseTypeData>("SELECT p.name AS PurchaseTypeName, p.code AS Code, p.isactive, p.divisionid FROM purchasetype p ");
                return purchasetype.ToList();
            case 26:
                var priceGroup = await connection.QueryAsync<PriceGroupingData>("SELECT p.name AS PriceGroupingName, p.code AS Code, p.isactive, p.divisionid FROM pricegrouping p ");
                return priceGroup.ToList();
            case 27:
                var itemType = await connection.QueryAsync<ItemTypeCodeData>("SELECT i.name AS ItemTypeCodeName, i.code AS Code, i.isactive, i.divisionid FROM mtritemtypecode i ");
                return itemType.ToList();
            case 28:
                var productclassificationcode = await connection.QueryAsync<ProductClassificationCodeData>("SELECT p.name AS ProductClassificationCodeName, p.code AS Code, p.isactive, p.divisionid FROM mtrproductclassificationcode p ");
                return productclassificationcode.ToList();
            case 29:
                var plcCode = await connection.QueryAsync<PLCCodeData>("SELECT p.name AS PLCCodeName, p.code AS Code, p.isactive, p.divisionid FROM mtrplccode p ");
                return plcCode.ToList();
            case 30:
                var uniqueidstatus = await connection.QueryAsync<UniqueIdStatusData>("select name as UniqueIdStatusName ,Code,IsActive from mtruniqueidstatus");
                return uniqueidstatus.ToList();
            case 31:
                var productgrouping = await connection.QueryAsync<ProductGroupingData>("SELECT p.name AS ProductGroupingName, p.code AS Code, p.isactive, p.divisionid FROM mtrproductgrouping p ");
                return productgrouping.ToList();
            case 32:
                var primarysecondarysku = await connection.QueryAsync<PrimarySecondarySKUData>("select name as PrimarySecondaryName ,code,isactive from mtrprimarysecondarysku");
                return primarysecondarysku.ToList();
            case 33:
                var countryoforigin = await connection.QueryAsync<CountryOfOriginData>("select name as Country_Of_Origin_Code ,description as Country_Of_Origin_Name ,isactive from mtrcountryoforigin");
                return countryoforigin.ToList();
            case 34:
                var currency = await connection.QueryAsync<CurrencyData>("select name as Currency_Code ,description as Currency_Name ,isactive from mtrcurrency");
                return currency.ToList();
            case 35:
                var taxcategory = await connection.QueryAsync<TaxCategoryData>("select name as TaxCategoryName,Code ,IsActive from mtrtaxcategory");
                return taxcategory.ToList();
            case 36:
                var innercarton = await connection.QueryAsync<InnerCartonCodeData>("SELECT i.name AS InnerCartonCodeName, i.code AS Code, i.isactive, i.divisionid FROM mtrinnercartoncode i ");
                return innercarton.ToList();
            case 37:
                var mastercarton = await connection.QueryAsync<MasterCartonCodeData>("SELECT m.name AS MasterCartonCodeName, m.code AS Code, m.isactive, m.divisionid FROM mtrmastercartoncode m ");
                return mastercarton.ToList();
            case 38:
                var diff_group = await connection.QueryAsync<DiffGroupData>("select name as DiffGroupName,Code ,IsActive from mtrdiffgroup2val");
                return diff_group.ToList();
            case 39:
                var madeofvalue = await connection.QueryAsync<MadeOfValueData>("select name as MadeOfValueName,Code ,IsActive from mtrmadeofvalue");
                return madeofvalue.ToList();
            case 40:
                var suppliercode = await connection.QueryAsync<SupplierCodeData>("SELECT s.name AS SupplierCodeName, s.code AS Code, s.isactive, s.divisionid FROM mtrsuppliercode s ");
                return suppliercode.ToList();
            case 41:
                var hsnCode = await connection.QueryAsync<HsnCodeData>("select Code, name as HsnName, IsActive from mtrhsncode");
                return hsnCode.ToList();
            case 42:
                var brandtype = await connection.QueryAsync<BrandTypeData>("select name as BrandTypeName,Code ,IsActive from mtrbrandtype");
                return brandtype.ToList();
            case 43:
                var producttype = await connection.QueryAsync<ProductTypeHierarchy>("select div_code,div_name,dept_code,dept_name,class_code,class_name,subclass_code,sub_name as Subclass_Name,producttype1 as Product_Type_1,productsubtype1 as Product_Subtype_1,producttype2 as Product_Type_2,productsubtype2 as Product_Subtype_2 from mtrproductype_subtype");
                return producttype.ToList();
            case 44:
                var rmsseason = await connection.QueryAsync<RMSSeason>("select name,code,isactive from mtrrmsseasoncode");
                return rmsseason.ToList();
            case 45:
                var warehouse = await connection.QueryAsync<WareHouse>("select name,code,isactive from warehouse");
                return warehouse.ToList();
            default:
                throw new ArgumentException("Master Data not Found");
        }
    }

    public async Task<byte[]> TemplatesExportExcel(bool? isActive)
    {
        using var connection = this.GetConnection();
        var result = new List<ExportTemplatesModel>();

        if (isActive is not null)
        {
            result = (await connection.QueryAsync<ExportTemplatesModel>($"select t.name as Templates,d.name as Division,TO_CHAR(t.datecreated, 'DD/MM/YYYY') AS datecreated,a.name as CreatedBy from template t " +
                                                                    $"JOIN division d ON d.id = t.divisionid " +
                                                                    $"JOIN appuser a ON a.id = t.createdby where t.isactive = '{isActive}' order by t.id asc ")).ToList();
        }
        else
        {
            result = (await connection.QueryAsync<ExportTemplatesModel>("select t.name as Templates,d.name as Division,TO_CHAR(t.datecreated, 'DD/MM/YYYY') AS datecreated,a.name as CreatedBy from template t " +
                                                                    "JOIN division d ON d.id = t.divisionid " +
                                                                    "JOIN appuser a ON a.id = t.createdby order by t.id asc ")).ToList();
        }

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Templates Records");

        if (result.Any())
        {
            var headers = new[] { "Templates", "Division", "DateCreated", "CreatedBy" };
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
                worksheet.Cells[row, 1].Value = record.Templates;
                worksheet.Cells[row, 2].Value = record.Division;
                worksheet.Cells[row, 3].Value = record.DateCreated;
                worksheet.Cells[row, 4].Value = record.CreatedBy;

                worksheet.Cells[row, 1, row, 15].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;

                for (int col = 1; col <= 4; col++)
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

    public async Task DeleteTemplate(IEnumerable<long> ids)
    {
        using var connection = this.GetConnection();
        foreach (var id in ids)
        {
            await connection.ExecuteAsync($"Delete from template where id = {id}");
        }
    }

    public async Task UpdateItemAndEanCode()
    {
        using var connection = this.GetConnection();
        var product = await connection.QueryAsync<ItemCodeStaging>("SELECT i.code, i.eancode, i.uidcode FROM product p JOIN itemcodestaging i ON p.uid = i.uidcode");

        foreach (var data in product)
        {
            var itemCodeValue = data.Code;
            var eanCodeValue = data.EanCode;

            var existingJson = await connection.ExecuteScalarAsync<string>($"SELECT columndata FROM product WHERE uid = '{data.UidCode}'");
            IDictionary<string, object> existingObject = JsonConvert.DeserializeObject<IDictionary<string, object>>(existingJson ?? "{}");
            existingObject["Item Code"] = itemCodeValue;
            existingObject["EAN Code"] = eanCodeValue;
            var updatedJson = JsonConvert.SerializeObject(existingObject);
            string updateQuery;

            if (updatedJson.Contains("Care Instruction 3 Caring Don't"))
            {
                string escapedJson = updatedJson.Replace("Care Instruction 3 Caring Don't", "Care Instruction 3 Caring Don''t");
                var update = $"UPDATE product SET columndata = '{escapedJson}' WHERE uid = '{data.UidCode}'";
                await connection.ExecuteAsync(update);
            }
            else
            {
                await connection.ExecuteAsync($"UPDATE product SET columndata = '{updatedJson}' WHERE uid = '{data.UidCode}'");
            }
        }
    }

    private string GetWorksheetName(long masterDataID)
    {
        switch (masterDataID)
        {
            case 1:
                return "HierarchicalMasterData";
            case 2:
                return "TemplateData";
            case 3:
                return "ClassNameMasterData";
            case 4:
                return "DepartmentMasterData";
            case 5:
                return "DivisionMasterData";
            case 6:
                return "GroupData";
            case 7:
                return "SubClassData";
            case 8:
                return "BrandMasterData";
            case 9:
                return "BusinessMasterData";
            case 10:
                return "AttributeMasterData";
            case 11:
                return "ProductColourData";
            case 12:
                return "SetpcsData";
            case 13:
                return "LeadTimeData";
            case 14:
                return "ItemShapeData";
            case 15:
                return "PackagingTypeData";
            case 16:
                return "FinishData";
            case 17:
                return "PrimaryFurnitureMaterialData";
            case 18:
                return "SecondaryFurnitureMaterialData";
            case 19:
                return "Producttype1Data";
            case 20:
                return "ProductSubType1Data";
            case 21:
                return "Producttype2Data";
            case 22:
                return "ProductSubType2Data";
            case 23:
                return "SeasonData";
            case 24:
                return "ProductPurposeData";
            case 25:
                return "PurchaseTypeData";
            case 26:
                return "PriceGroupingData";
            case 27:
                return "ItemTypeCodeData";
            case 28:
                return "ProductClassificationCodeData";
            case 29:
                return "PLCCodeData";
            case 30:
                return "UniqueIdStatusData";
            case 31:
                return "ProductGroupingData";
            case 32:
                return "PrimarySecondarySKUData";
            case 33:
                return "CountryOfOriginData";
            case 34:
                return "CurrencyData";
            case 35:
                return "TaxCategoryData";
            case 36:
                return "InnerCartonCodeData";
            case 37:
                return "MasterCartonCodeData";
            case 38:
                return "DiffGroupData";
            case 39:
                return "MadeOfValueData";
            case 40:
                return "SupplierCodeData";
            case 41:
                return "HSNCode";
            case 42:
                return "BrandType";
            case 43:
                return "producttype";
            case 44:
                return "RMSSeasonCode";
            case 45:
                return "Warehouse";
            default:
                throw new Exception("MasterData not found");
        }
    }

    private void LoadDataAndAddTable<T>(ExcelWorksheet worksheet, IEnumerable<T> data)
    {
        worksheet.Cells.LoadFromCollection(data, true);
        var table = worksheet.Tables.Add(worksheet.Cells[worksheet.Dimension.Address], worksheet.Name);
        table.TableStyle = TableStyles.Light9;
        table.ShowHeader = true;
        table.ShowFilter = false;
    }

    private string GetItemCodeName(long itemCodeId)
    {
        using var connection = this.GetConnection();

        var query = "SELECT code FROM itemcodestaging WHERE id = @ItemCodeId";
        var parameters = new { ItemCodeId = itemCodeId };
        var result = connection.QueryFirstOrDefault<string>(query, parameters);
        return result;
    }

    private bool IsValidDataType(string dataType)
    {
        var allowedDataTypes = new List<long> { 0, 10, 20, 30, 40 };

        return long.TryParse(dataType, out long dataTypeValue) && allowedDataTypes.Contains(dataTypeValue);
    }
}