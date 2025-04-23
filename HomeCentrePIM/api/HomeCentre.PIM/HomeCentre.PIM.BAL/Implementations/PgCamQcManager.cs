using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage;
using Azure.Storage.Blobs;
using ClosedXML.Excel;
using Dapper;
using DocumentFormat.OpenXml.Office2021.Excel.RichDataWebImage;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentMigrator.Runner.Generators.Base;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Model.Geo;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.Master;
using HomeCentre.PIM.DAL.Staging;
using ImageMagick;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NPOI.Util;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Org.BouncyCastle.Crypto.Macs;

namespace HomeCentre.PIM.BAL.Implementations
{
    public class PgCamQcManager : PgEntityAction<Planning>, ICamQcManager
    {
        private readonly IBlobRepo _blobRepo;
        private readonly IConfiguration _configuration;

        public PgCamQcManager(IUserContext userContext, IDbConnectionFactory connectionFactory, IConfiguration configuration, IBlobRepo blobRepo)
            : base(userContext, connectionFactory, configuration)
        {
            _configuration = configuration;
            _blobRepo = blobRepo;
        }

        public async Task<List<CamQc>> GetCamQcdetails(DateTime? fromDate, DateTime? toDate, string imageid)
        {
            using var connection = this.GetConnection();
            var startDates = fromDate ?? DateTime.MinValue;
            var endDates = toDate ?? DateTime.MaxValue;
            var formattedStartDate = startDates.ToString("yyyy/MM/dd 00:00:00");
            var formattedEndDate = endDates.ToString("yyyy/MM/dd 23:59:59");
            var query = "SELECT DISTINCT ICS.EANCODE, PI.ID, PI.ITEMCODE, PI.PICTUREID AS PICTUREID, PI.IMAGEURL, TO_CHAR(PI.RECEIVEDDATE, 'DD/MM/YY') AS RECEIVEDDATE, PI.CAMQCL2SUBMIT AS CAMQCL2SUBMIT, S.REMAININGCAPACITY AS CAPACITY, PI.CAMQCL2ACTION AS L2ACTION, PI.CAMQCL1ACTION AS L1ACTION, PI.PRODUCTRECEIVED AS PRODUCTRECEIVED, PI.STOCKRETURNCOMMENTS AS COMMENTS, PI.CAMQCSTATUS, PI.STOCKRETURN AS STOCKRETURNTOQC, S.CITY, S.NAME, TO_CHAR(PS.ALLOCATIONDATE, 'DD/MM/YY') AS ALLOCATIONDATE, PI.REJECTIONCOMMENTS AS REJECTIONCOMMENTS, TO_CHAR(PI.HANDOVERDATE, 'DD/MM/YY') AS HANDOVERDATE, PI.IMAGETYPE, PS.PRIORITY, PI.QUALITYCHECK AS L2QUALITYCHECK, PI.DIVISIONID AS DIVISIONID, PI.HANDOVERSTATUS AS HANDOVERSTATUS, PI.PHOTOSHOOTSTATUS AS PHOTOSHOOTSTATUS, PI.CAMQCL1QUALITYCHECK AS CAMQCL1QUALITYCHECK, PI.STATUSDATE AS STATUSDATE FROM PRODUCTIMAGE PI LEFT JOIN PRODUCTSTUDIO PS ON PI.ITEMCODE = PS.ITEMCODE LEFT JOIN STUDIO S ON PS.STUDIOID = S.ID LEFT JOIN ITEMCODESTAGING AS ICS ON PI.ITEMCODE = ICS.CODE LEFT JOIN product AS pro ON pro.uid = ics.uidcode LEFT JOIN mtrproductimagetype mit ON mit.name = pi.imagetype  WHERE pro.isactive = true and ";
            if (fromDate != null || toDate != null)
            {
                query += $"  pi.receiveddate::date BETWEEN '{formattedStartDate}'::date AND '{formattedEndDate}'::date AND  ";
            }

            if (!string.IsNullOrEmpty(imageid))
            {
                query += $" mit.id = {imageid} AND ";
            }

            query += "pi.samplestatus = 'Approved' AND (pi.camqcstatus IS NULL OR pi.camqcstatus != 2) ORDER BY pi.id DESC, ics.eancode, pi.itemcode, Pictureid, pi.imageurl, receiveddate, pi.camqcl2submit, s.remainingcapacity,  pi.camqcL2action, pi.camqcl1action,  pi.productreceived, comments, pi.camqcstatus, stockReturnToQC, s.city, s.name, allocationDate, pi.rejectioncomments, HandOverDate, pi.imagetype, ps.priority, L2qualitycheck, DivisionId, handoverstatus, photoshootstatus, camqcl1qualitycheck desc";
            var products = await connection.QueryAsync<CamQc>(query);
            var resultlist = products.ToList();
            foreach (var result in resultlist)
            {
                switch (result.HandoverStatus)
                {
                    case "1":
                        var mtrQuery1 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 1");
                        result.HandoverStatus = mtrQuery1.Name;
                        break;

                    case "2":
                        var mtrQuery2 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 2");
                        result.HandoverStatus = mtrQuery2.Name;
                        break;
                    case null:
                    case "null":
                        var mtrQuery3 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 2");
                        result.HandoverStatus = mtrQuery3.Name;
                        break;
                }

                switch (result.Camqcstatus)
                {
                    case "1":
                        var mtrQuery1 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 2");
                        result.Camqcstatus = mtrQuery1.Name;
                        break;

                    case "2":
                        var mtrQuery2 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 1");
                        result.Camqcstatus = mtrQuery2.Name;
                        break;
                    case null:
                    case "null":
                        var mtrQuery3 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 2");
                        result.Camqcstatus = mtrQuery3.Name;
                        break;
                }

                switch (result.PhotoShootStatus)
                {
                    case "1":
                        var photshootquery1 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 1");
                        result.PhotoShootStatus = photshootquery1.Name;
                        break;

                    case "2":
                        var photshootquery2 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 2");
                        result.PhotoShootStatus = photshootquery2.Name;
                        break;
                    case null:
                    case "null":
                        result.PhotoShootStatus = null;
                        break;
                }
            }

            return resultlist.OrderByDescending(rl => rl.StatusDate).ToList();
        }

        public async Task<List<CamQc>> GetCamQcL2details(DateTime? fromDate, DateTime? toDate, string imageid)
        {
            using var connection = this.GetConnection();
            var startDates = fromDate ?? DateTime.MinValue;
            var endDates = toDate ?? DateTime.MaxValue;
            var formattedStartDate = startDates.ToString("yyyy/MM/dd 00:00:00");
            var formattedEndDate = endDates.ToString("yyyy/MM/dd 23:59:59");
            var query = "SELECT DISTINCT ICS.EANCODE, PI.ID, PI.ITEMCODE, PI.PICTUREID AS PICTUREID, PI.IMAGEURL, TO_CHAR(PI.RECEIVEDDATE, 'DD/MM/YY') AS RECEIVEDDATE, PI.CAMQCL2SUBMIT AS CAMQCL2SUBMIT, S.REMAININGCAPACITY AS CAPACITY, PI.CAMQCL2ACTION AS L2ACTION, PI.CAMQCL1ACTION AS L1ACTION,PI.SUBMITDATE AS SUBMITDATE, PI.PRODUCTRECEIVED AS PRODUCTRECEIVED, PI.STOCKRETURNCOMMENTS AS COMMENTS, PI.CAMQCSTATUS, PI.STOCKRETURN AS STOCKRETURNTOQC, S.CITY, S.NAME, TO_CHAR(PS.ALLOCATIONDATE, 'DD/MM/YY') AS ALLOCATIONDATE, PI.REJECTIONCOMMENTS AS REJECTIONCOMMENTS, TO_CHAR(PI.HANDOVERDATE, 'DD/MM/YY') AS HANDOVERDATE, PI.IMAGETYPE, PS.PRIORITY, PI.QUALITYCHECK AS L2QUALITYCHECK, PI.DIVISIONID AS DIVISIONID, PI.HANDOVERSTATUS AS HANDOVERSTATUS, PI.PHOTOSHOOTSTATUS AS PHOTOSHOOTSTATUS, PI.CAMQCL1QUALITYCHECK AS CAMQCL1QUALITYCHECK FROM PRODUCTIMAGE PI LEFT JOIN PRODUCTSTUDIO PS ON PI.ITEMCODE = PS.ITEMCODE LEFT JOIN STUDIO S ON PS.STUDIOID = S.ID LEFT JOIN ITEMCODESTAGING AS ICS ON PI.ITEMCODE = ICS.CODE LEFT JOIN product AS pro ON pro.uid = ics.uidcode LEFT JOIN mtrproductimagetype mit ON mit.name = pi.imagetype WHERE pro.isactive = true and ";
            if (fromDate != null || toDate != null)
            {
                query += $"  pi.receiveddate::date BETWEEN '{formattedStartDate}'::date AND '{formattedEndDate}'::date AND ";
            }

            if (!string.IsNullOrEmpty(imageid))
            {
                query += $" mit.id = {imageid} AND ";
            }

            query += "pi.samplestatus = 'Approved' And photoshootstatus = 2 AND (pi.camqcl2submit IS NULL OR TRIM(pi.camqcl2submit) != '1') ORDER BY pi.id DESC, ics.eancode, pi.itemcode, Pictureid, pi.imageurl, receiveddate, pi.camqcl2submit, s.remainingcapacity,  pi.camqcL2action, pi.camqcl1action,  pi.productreceived, comments, pi.camqcstatus, stockReturnToQC, s.city, s.name, allocationDate, pi.rejectioncomments, HandOverDate, pi.imagetype, ps.priority, L2qualitycheck, DivisionId, handoverstatus, photoshootstatus, camqcl1qualitycheck desc";
            var products = await connection.QueryAsync<CamQc>(query);
            var resultlist = products.ToList();
            foreach (var result in resultlist)
            {
                switch (result.HandoverStatus)
                {
                    case "1":
                        var mtrQuery1 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 1");
                        result.HandoverStatus = mtrQuery1.Name;
                        break;

                    case "2":
                        var mtrQuery2 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 2");
                        result.HandoverStatus = mtrQuery2.Name;
                        break;
                    case null:
                    case "null":
                        var mtrQuery3 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 2");
                        result.HandoverStatus = mtrQuery3.Name;
                        break;
                }

                switch (result.PhotoShootStatus)
                {
                    case "1":
                        var photshootquery1 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 1");
                        result.PhotoShootStatus = photshootquery1.Name;
                        break;

                    case "2":
                        var photshootquery2 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 2");
                        result.PhotoShootStatus = photshootquery2.Name;
                        break;
                    case null:
                    case "null":
                        result.PhotoShootStatus = null;
                        break;
                }
            }

            return resultlist.OrderByDescending(rl => rl.SubmitDate).ToList();
        }

        public async Task<List<CamQc>> SearchCamQcdetails(DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                using var connection = this.GetConnection();
                var startDates = fromDate ?? DateTime.MinValue;
                var endDates = toDate ?? DateTime.MaxValue;
                var formattedStartDate = startDates.ToString("yyyy/MM/dd");
                var formattedEndDate = endDates.ToString("yyyy/MM/dd");
                var query = "SELECT p.eancode, p.itemcode, pi.imageurl, to_char(pi.receiveddate, 'DD/MM/YY') as receiveddate, p.handoverstatus, p.photoshootstatus, s.city, s.name, to_char(ps.allocationDate, 'DD/MM/YY') as allocationDate," +
    " to_char(pi.handoverdate, 'DD/MM/YY') as handoverdate, pi.imagetype, ps.priority, pi.productreceived, d.name AS division " +
    "FROM studio s JOIN productstudio ps ON ps.studioid = s.id JOIN photoshootproduct p ON ps.productid = p.id " +
    "JOIN productimage pi ON pi.productid = p.id LEFT JOIN division d ON s.divisionid = d.id ";
                query += $"WHERE pi.receiveddate::date BETWEEN '{formattedStartDate}'::date AND '{formattedEndDate}' order by p.id desc";
                var products = await connection.QueryAsync<CamQc>(query);
                var resultlist = products.ToList();
                foreach (var result in resultlist)
                {
                    switch (result.HandoverStatus)
                    {
                        case "1":
                            var mtrQuery1 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 1");
                            result.HandoverStatus = mtrQuery1.Name;
                            break;

                        case "2":
                            var mtrQuery2 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 2");
                            result.HandoverStatus = mtrQuery2.Name;
                            break;
                        case null:
                        case "null":
                            var mtrQuery3 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 2");
                            result.HandoverStatus = mtrQuery3.Name;
                            break;
                    }

                    switch (result.PhotoShootStatus)
                    {
                        case "1":
                            var photshootquery1 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 1");
                            result.PhotoShootStatus = photshootquery1.Name;
                            break;

                        case "2":
                            var photshootquery2 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 2");
                            result.PhotoShootStatus = photshootquery2.Name;
                            break;
                        case null:
                        case "null":
                            var photshootquery4 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 1");
                            result.PhotoShootStatus = photshootquery4.Name;
                            break;
                    }
                }

                return resultlist;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public async Task<List<UIDStatus>> GetItemStatus()
        {
            using var connection = this.GetConnection();
            var query = "SELECT DISTINCT pi.itemcode, i.uidcode, p.columndata, pi.divisionid as Division \r\nFROM productimage pi\r\nJOIN itemcodestaging i ON pi.itemcode = i.code\r\nJOIN product p ON p.uid = i.uidcode ";
            var results = await connection.QueryAsync<UIDStatus>(query);
            foreach (var result in results)
            {
                var attributeQuery = $"SELECT label FROM productattribute WHERE isrequired = true AND divisionid = {result.Division}";
                var labels = await connection.QueryAsync<string>(attributeQuery);
                var columnDataKeys = result.ColumnData.Keys;
                int countOfKeysInLabels = columnDataKeys.Count(key => labels.Contains(key));
                result.PercentageOfKeysInLabels = (countOfKeysInLabels * 100) / labels.Count();
            }

            return results.ToList();
        }

        public async Task<List<ItemCodeNumber>> GetItemCodeNumbers()
        {
            using var connection = this.GetConnection();
            var plan = await connection.QueryAsync<ItemCodeNumber>("SELECT s.city, s.name, ps.priority FROM studio s JOIN productstudio ps ON s.id = ps.studioid;");
            return plan.ToList();
        }

        public async Task<string> CAMQCL1ExportExcel(ImageTypeDropdown imageTypeDropdown)
        {
            using var connection = this.GetConnection();
            var tempFile = Path.GetTempFileName();
            var startDates = imageTypeDropdown.FromDate ?? DateTime.MinValue;
            var endDates = imageTypeDropdown.ToDate ?? DateTime.MaxValue;
            var formattedStartDate = startDates.ToString("yyyy/MM/dd");
            var formattedEndDate = endDates.ToString("yyyy/MM/dd");
            var imageType = imageTypeDropdown.ImageType;

            var query = "SELECT DISTINCT  pi.id, ics.eancode As EANcode, pi.itemcode, pi.pictureid as Pictureid, pi.imageurl As ImageURL, to_char(pi.receiveddate, 'DD/MM/YY') as receiveddate, s.remainingcapacity as Capacity, pi.camqcL2action as L2Action, pi.statusdate, pi.camqcl1action as L1Action, pi.productreceived as productreceived, pi.stockreturncomments as comments, pi.stockreturn as stockReturnToQC, s.city, s.name, to_char(pi.allocationDate, 'DD/MM/YY') as allocationDate, pi.rejectioncomments as Rejectioncomments, to_char(pi.handoverdate, 'DD/MM/YY') as HandOverDate, pi.imagetype, ps.priority, pi.qualitycheck as L2qualitycheck, d.name as Division, pi.photoshootstatus AS photoshootstatus, pi.camqcl1qualitycheck as camqcl1qualitycheck " +
                "FROM ProductImage pi JOIN Division d ON pi.DivisionID = d.ID LEFT JOIN ProductStudio ps ON pi.ItemCode = ps.ItemCode LEFT JOIN Studio s ON ps.StudioID = s.ID  LEFT JOIN itemcodestaging AS ics ON pi.itemcode = ics.code LEFT JOIN product AS pro ON pro.uid = ics.uidcode " +
             $" WHERE pro.isactive = true and pi.receiveddate::date BETWEEN '{formattedStartDate}'::date AND '{formattedEndDate}'::date AND pi.samplestatus = 'Approved' AND (pi.camqcstatus IS NULL OR pi.camqcstatus != 2) and CAST(d.code AS INTEGER) IN ({imageTypeDropdown.UserDivision}) ";

            if (imageType != "All")
            {
                query += $"AND pi.imagetype IN ('{imageType}') ";
            }

            query += " ORDER BY pi.id DESC";

            var result = await connection.QueryAsync<CamQCExcel>(query);

            foreach (var item in result)
            {
                switch (item.PhotoShootStatus)
                {
                    case "1":
                        var photshootquery1 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 1");
                        item.PhotoShootStatus = photshootquery1.Name;
                        break;

                    case "2":
                        var photshootquery2 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 2");
                        item.PhotoShootStatus = photshootquery2.Name;
                        break;
                    case null:
                    case "null":
                        item.PhotoShootStatus = string.Empty;
                        break;
                }

                switch (item.StockReturnToQC)
                {
                    case "0":
                        item.StockReturnToQC = "Yet To Return";
                        break;

                    case "1":
                        item.StockReturnToQC = "Returned";
                        break;
                    case null:
                    case "null":
                        item.StockReturnToQC = string.Empty;
                        break;
                }
            }

            var products = result.OrderByDescending(r => r.StatusDate).ToList();

            if (products.Any())
            {
                var desiredColumnOrder = new List<string>
                {
                    "ItemCode", "EANCode", "ImageURL", "ReceivedDate", "StudioName", "StudioLocation", "StudioAvailableCapacity", "StudioAllocationDate", "SampleType", "Priority"
                };

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Sheet1");

                var headers = desiredColumnOrder;
                var headerBackgroundColor = ColorTranslator.FromHtml("#5787C5");
                var headerTextColor = System.Drawing.Color.White;
                var borderColor = ColorTranslator.FromHtml("#5787C5");

                for (int i = 0; i < headers.Count; i++)
                {
                    var cell = worksheet.Cells[1, i + 1];
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.Color.SetColor(headerTextColor);
                    cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(headerBackgroundColor);
                    cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Top.Color.SetColor(borderColor);
                    cell.Style.Border.Left.Color.SetColor(borderColor);
                    cell.Style.Border.Right.Color.SetColor(borderColor);
                    cell.Style.Border.Bottom.Color.SetColor(borderColor);
                }

                var row = 2;
                foreach (var record in products)
                {
                    for (int col = 1; col <= headers.Count; col++)
                    {
                        string columnName = headers[col - 1];
                        var cell = worksheet.Cells[row, col];

                        switch (columnName)
                        {
                            case "ItemCode":
                                cell.Value = record.ItemCode.ToString();
                                break;
                            case "EANCode":
                                cell.Value = record.EANCode.ToString();
                                break;
                            case "ImageURL":
                                cell.Value = record.ImageURL;
                                break;
                            case "ReceivedDate":
                                cell.Value = record.ReceivedDate;
                                break;
                            case "StudioName":
                                cell.Value = record.Name;
                                break;
                            case "StudioLocation":
                                cell.Value = record.City;
                                break;
                            case "StudioAvailableCapacity":
                                cell.Value = record.Capacity;
                                break;
                            case "StudioAllocationDate":
                                cell.Value = record.AllocationDate;
                                break;
                            case "SampleType":
                                cell.Value = record.ImageType;
                                break;
                            case "Priority":
                                cell.Value = string.IsNullOrEmpty(record.Priority) ? (record.ImageType == "GRN" ? "P0" : "P1") : record.Priority;
                                break;
                        }

                        cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        cell.Style.Border.Top.Color.SetColor(borderColor);
                        cell.Style.Border.Left.Color.SetColor(borderColor);
                        cell.Style.Border.Right.Color.SetColor(borderColor);
                        cell.Style.Border.Bottom.Color.SetColor(borderColor);
                    }

                    row++;
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                for (int i = 1; i <= 10; i++)
                {
                    worksheet.Column(i).Width = i == 7 || i == 8 ? 24 : 20;
                }

                using var memoryStream = new MemoryStream();
                package.SaveAs(memoryStream);
                await File.WriteAllBytesAsync(tempFile, memoryStream.ToArray());
            }

            return tempFile;
        }

        public async Task<string> CAMQCL2ExportExcel(ImageTypeDropdown imageTypeDropdown)
        {
            using var connection = this.GetConnection();
            var startDates = imageTypeDropdown.FromDate ?? DateTime.MinValue;
            var endDates = imageTypeDropdown.ToDate ?? DateTime.MaxValue;
            var formattedStartDate = startDates.ToString("yyyy/MM/dd");
            var formattedEndDate = endDates.ToString("yyyy/MM/dd");
            var imageType = imageTypeDropdown.ImageType;
            var tempFile = Path.GetTempFileName();
            using var wb = new XLWorkbook();
            var sheet = wb.AddWorksheet();
            using var stream = new MemoryStream();
            var query = "SELECT DISTINCT pi.id,ics.eancode As EANcode, pi.itemcode, pi.pictureid as Pictureid, pi.imageurl, to_char(pi.receiveddate, 'DD/MM/YYYY') as receiveddate, s.remainingcapacity as StudioAvailableCapacity, pi.camqcL2action as L2Action," +
            " pi.camqcl1action as L1Action, pi.submitdate as SubmitDate, pi.productreceived as productreceived, pi.stockreturncomments as comments, pi.stockreturn as StockReturnToQC, s.city As StudioLocation, s.name As StudioName," +
            " to_char(pi.allocationDate, 'DD/MM/YYYY') as allocationDate, pi.rejectioncomments as Rejectioncomments, to_char(pi.handoverdate, 'DD/MM/YY') as HandOverDate, pi.imagetype, ps.priority, pi.qualitycheck as L2qualitycheck, " +
            "d.name AS division, pi.handoverstatus AS handoverstatus, pi.photoshootstatus AS photoshootstatus, pi.camqcl1qualitycheck as camqcl1qualitycheck FROM studio s" +
            " JOIN productstudio ps ON ps.studioid = s.id JOIN productimage pi ON pi.itemcode = ps.itemcode JOIN division d on pi.divisionid = d.id  LEFT JOIN itemcodestaging AS ics ON pi.itemcode = ics.code LEFT JOIN product AS pro ON pro.uid = ics.uidcode " +
            $" WHERE pro.isactive = true and pi.receiveddate::date BETWEEN '{formattedStartDate}'::date AND '{formattedEndDate}'::date AND pi.samplestatus = 'Approved' and photoshootstatus = 2 AND (pi.camqcl2submit IS NULL OR TRIM(pi.camqcl2submit) != '1') and CAST(d.code AS INTEGER) IN ({imageTypeDropdown.UserDivision})";

            if (imageType != "All")
            {
                query += $"AND pi.imagetype IN ('{imageType}') ";
            }

            query += " ORDER BY pi.id DESC";

            var result = await connection.QueryAsync<CAMQcL2ExportExcel>(query);

            foreach (var item in result)
            {
                switch (item.HandoverStatus)
                {
                    case "1":
                        var mtrQuery1 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 1");
                        item.HandoverStatus = mtrQuery1.Name;
                        break;

                    case "2":
                        var mtrQuery2 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 2");
                        item.HandoverStatus = mtrQuery2.Name;
                        break;
                    case null:
                    case "null":
                        var mtrQuery3 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 2");
                        item.HandoverStatus = mtrQuery3.Name;
                        break;
                }

                switch (item.PhotoShootStatus)
                {
                    case "1":
                        var photshootquery1 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 1");
                        item.PhotoShootStatus = photshootquery1.Name;
                        break;

                    case "2":
                        var photshootquery2 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 2");
                        item.PhotoShootStatus = photshootquery2.Name;
                        break;
                    case "3":
                        var photshootquery3 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 3");
                        item.PhotoShootStatus = photshootquery3.Name;
                        break;
                    case null:
                    case "null":

                        item.PhotoShootStatus = string.Empty;
                        break;
                }

                switch (item.StockReturnToQC)
                {
                    case "0":
                        item.StockReturnToQC = "Yet To Return";
                        break;

                    case "1":
                        item.StockReturnToQC = "Returned";
                        break;
                    case null:
                    case "null":
                        item.StockReturnToQC = string.Empty;
                        break;
                }
            }

            var products = result.OrderByDescending(r => r.SubmitDate);

            if (products.Any())
            {
                var desiredColumnOrder = new List<string>
                {
                    "ItemCode", "EANCode", "PSImage", "Division", "ReceivedDate", "StudioName", "StudioLocation", "StudioAvailableCapacity", "StudioAllocationDate", "SampleType", "Priority", "PhotoshootStatus", "StockReturnToQC"
                };

                var r = 1;
                var cH = 1;

                foreach (var key in desiredColumnOrder)
                {
                    sheet.Cell(r, cH).Value = key;
                    cH++;
                }

                r++;
                foreach (var record in products)
                {
                    var c = 1;

                    foreach (var columnName in desiredColumnOrder)
                    {
                        string imageUrl1 = null;
                        var imgquery = @"SELECT pi.pictureid FROM productimage pi WHERE pi.itemcode = @Itemcode";
                        var pictureId = await connection.QueryFirstOrDefaultAsync<string>(imgquery, new { Itemcode = record.ItemCode });

                        if (pictureId is not null)
                        {
                            var lastPictureId = pictureId.Split(',').Last();
                            var imageUrlQuery = $@"
    SELECT up.imageurl 
    FROM photoshootuploadpicture up 
    WHERE up.id::text IN ('{lastPictureId}')
    ORDER BY up.id DESC 
    LIMIT 1";

                            imageUrl1 = await connection.QueryFirstOrDefaultAsync<string>(imageUrlQuery);
                        }

                        if (columnName == "ItemCode")
                        {
                            ////sheet.Cell(r, c).Value = record.ItemCode;
                            sheet.Cell(r, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                            double itemCodeValue = record.ItemCode;
                            sheet.Cell(r, c).Value = itemCodeValue;
                            sheet.Column(c).Width = 30;
                            sheet.Row(r).Height = 20;
                            sheet.Cell(r, c).Style.NumberFormat.Format = "0";
                        }
                        else if (columnName == "EANCode")
                        {
                            sheet.Cell(r, c).Style.NumberFormat.Format = "@";
                            sheet.Cell(r, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                            sheet.Cell(r, c).Value = record.EANCode;
                            sheet.Column(c).Width = 30;
                        }
                        else if (columnName == "Type")
                        {
                            sheet.Cell(r, c).Value = record.ImageType;
                            sheet.Column(c).Width = 30;
                        }
                        else if (columnName == "Division")
                        {
                            sheet.Cell(r, c).Value = record.Division;
                            sheet.Column(c).Width = 30;
                        }
                        else if (columnName == "ReceivedDate")
                        {
                            sheet.Cell(r, c).Style.NumberFormat.Format = "@";
                            sheet.Cell(r, c).Value = record.ReceivedDate ?? string.Empty;
                            sheet.Column(c).Width = 30;
                        }
                        else if (columnName == "StudioAllocationDate")
                        {
                            sheet.Cell(r, c).Style.NumberFormat.Format = "@";
                            sheet.Cell(r, c).Value = record.AllocationDate ?? string.Empty;
                            sheet.Column(c).Width = 30;
                        }
                        else if (columnName == "HandoverToPhotoshootStatus")
                        {
                            sheet.Cell(r, c).Value = record.HandoverStatus;
                            sheet.Column(c).Width = 30;
                        }
                        else if (columnName == "Priority")
                        {
                            sheet.Cell(r, c).Value = record.Priority;
                            sheet.Column(c).Width = 30;
                        }
                        else if (columnName == "PhotoshootStatus")
                        {
                            sheet.Cell(r, c).Value = record.PhotoShootStatus;
                            sheet.Column(c).Width = 30;
                        }
                        else if (columnName == "StockReturnToQC")
                        {
                            sheet.Cell(r, c).Value = record.StockReturnToQC;
                            sheet.Column(c).Width = 30;
                        }
                        else if (columnName == "StudioLocation")
                        {
                            sheet.Cell(r, c).Value = record.StudioLocation;
                            sheet.Column(c).Width = 30;
                        }
                        else if (columnName == "StudioName")
                        {
                            sheet.Cell(r, c).Value = record.StudioName;
                            sheet.Column(c).Width = 30;
                        }
                        else if (columnName == "StudioAvailableCapacity")
                        {
                            sheet.Cell(r, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                            sheet.Cell(r, c).Value = record.StudioAvailableCapacity;
                            sheet.Column(c).Width = 30;
                        }
                        else if (columnName == "PSImage")
                        {
                            if (!string.IsNullOrEmpty(imageUrl1))
                            {
                                string imageUrl = imageUrl1;
                                var imgFile = await _blobRepo.DownloadFile(imageUrl);
                                if (Path.GetExtension(imgFile).Equals(".avif", StringComparison.OrdinalIgnoreCase))
                                {
                                    using (MagickImage image = new MagickImage(imgFile))
                                    {
                                        image.Write("converted_image.png");
                                        imgFile = "converted_image.png";
                                    }
                                }

                                var picture = sheet.Pictures.Add(imgFile).MoveTo(sheet.Cell(r, c));
                                picture.Height = 63;
                                picture.Width = 80;
                                sheet.Column(c).Width = 14;
                                sheet.Row(r).Height = 50;
                            }
                            else
                            {
                                sheet.Cell(r, c).Value = null;
                            }
                        }

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

        public async Task UpdateCAMQCRecords(string city, string name, string priority, long itemcode)
        {
            try
            {
                using var connection = this.GetConnection();
                var studioid = await connection.QueryFirstOrDefaultAsync<int>("SELECT studioid FROM productstudio WHERE itemcode = @itemcode", new { itemcode });
                if (studioid == 0)
                {
                    var product = await connection.QueryFirstOrDefaultAsync<PhotoShootProduct>("SELECT id FROM photoshootproduct WHERE Itemcode = @itemcode", new { itemcode });
                    var photoshootProduct = await connection.QueryFirstOrDefaultAsync<Studio>($"SELECT remainingcapacity as TotalCapacity,id FROM studio WHERE name='{name}'");
                    await connection.ExecuteAsync("INSERT INTO productstudio (productid, studioid, priority,datecreated,itemcode) VALUES (@productid, @studioid, @priority,@datecreate,@itemcode)", new { productid = product.Id, studioid = photoshootProduct.Id, priority = priority, datecreate = DateTime.UtcNow, itemcode });
                    var remainingcapacity = photoshootProduct.TotalCapacity - 1;
                    await connection.ExecuteAsync($"Update studio set remainingcapacity = {remainingcapacity} where id = {photoshootProduct.Id}");
                }
                else
                {
                    var updateoldstudio = await connection.ExecuteAsync("UPDATE studio SET remainingcapacity = remainingcapacity + 1 WHERE id = @studioid", new { studioid });
                    var newstudio = await connection.QueryFirstOrDefaultAsync<int>("SELECT id FROM studio WHERE name = @name", new { name });
                    var updatenewstudio = await connection.ExecuteAsync("UPDATE studio SET remainingcapacity = remainingcapacity - 1 WHERE id = @newstudio", new { newstudio });
                    var query = await connection.ExecuteAsync("UPDATE productstudio SET priority = @Priority, studioid = @Studioid WHERE itemcode = @ItemCode", new { Priority = priority, Studioid = newstudio, ItemCode = itemcode });
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<CamQc>> GetStudios()
        {
            using var connection = this.GetConnection();
            var query = "SELECT DISTINCT ps.itemcode,s.name,ps.priority,s.remainingcapacity as Capacity,s.city, to_char(ps.allocationdate, 'DD/MM/YY') as AllocationDate  from studio s join productstudio ps on s.id=ps.studioid";
            var results = await connection.QueryAsync<CamQc>(query);
            return results.ToList();
        }

        public async Task Updatetstatus()
        {
            using var connection = this.GetConnection();
            var divisionCodes = "61,4,65";
            var divisions = divisionCodes.Split(',');
            foreach (var division in divisions)
            {
                var query = "SELECT * FROM product WHERE columndata->>'Division Code' = @Division";
                var products = await connection.QueryAsync<Product>(query, new { Division = division });
                foreach (var productdetails in products)
                {
                    var marketplacetempquery = "SELECT id FROM template WHERE divisionid = @DivisionId";
                    var marketplacetempIds = await connection.QueryAsync<int>(marketplacetempquery, new { DivisionId = int.Parse(division) });
                    var marketplaceAttributeData = $"SELECT pa.name, pa.datatype, t.id AS templateattributeid, t.templateid, t.attributeid, pa.isdatalist FROM productattribute pa JOIN templateattribute t ON pa.id = t.attributeid WHERE t.templateid = @Ids ";
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
                            else
                            {
                                return true;
                            }
                        });
                        if (allKeysFound)
                        {
                            var producttemplateid = await connection.QueryFirstOrDefaultAsync<string>(
         $"SELECT TemplateId FROM product WHERE uid = '{productdetails.Uid}'");

                            string ids = string.Empty;

                            if (producttemplateid == null)
                            {
                                ids = id.ToString();
                            }
                            else
                            {
                                if (!producttemplateid.Split(',').Contains(id.ToString()))
                                {
                                    ids = producttemplateid + "," + id.ToString();
                                }
                            }

                            if (!string.IsNullOrEmpty(ids))
                            {
                                await connection.ExecuteAsync($"UPDATE product SET templateid='{ids}' WHERE uid = '{productdetails.Uid}'");
                            }
                        }
                    }
                }
            }
        }

        public async Task UpdateCAMQCL1QualityCheck(long itemcode, string? check, string? username)
        {
            try
            {
                using var connection = this.GetConnection();
                var studioexist = await connection.QueryFirstOrDefaultAsync<int>($"SELECT id FROM productstudio where itemcode={itemcode}");
                if (studioexist != 0)
                {
                    var query = await connection.ExecuteAsync("UPDATE Productimage SET  camqcl1qualitycheck= @check, camqcl1qccheckby= @camqcl1qccheckby, camqcl1qccheckdate= @camqcl1qccheckdate    WHERE itemcode = @itemcode", new { itemcode, check, camqcl1qccheckby = username, camqcl1qccheckdate = DateTime.UtcNow });
                }
                else
                {
                    var studios = await connection.QueryFirstOrDefaultAsync<Studio>("SELECT priority FROM studio");
                    if (studios != null)
                    {
                        var division = await connection.QueryFirstOrDefaultAsync<string>("SELECT divisionid from productimage WHERE itemcode  = @itemcode", new { itemcode });
                        var studios1 = await connection.QueryAsync<Studio>(
    "SELECT priority\r\nFROM studio\r\nWHERE divisionid = CAST(@division AS INTEGER)\r\n  AND DATE(effectivefromdate) <= CURRENT_DATE\r\n  AND remainingcapacity!=0 AND isactive = true\r\nORDER BY\r\n  CASE\r\n    WHEN priority = 'P0' THEN 0\r\n    ELSE 1\r\n  END,\r\n  substring(priority from '^[^0-9]+')::text, \r\n  nullif(substring(priority from '[0-9]+')::int, 0) ASC;",
    new { division });

                        var product = await connection.QueryFirstOrDefaultAsync<PhotoShootProduct>("SELECT id FROM photoshootproduct WHERE Itemcode = @itemcode", new { itemcode });
                        bool checking = false;
                        foreach (var studio in studios1)
                        {
                            var photoshootProduct = await connection.QueryFirstOrDefaultAsync<Studio>($"SELECT remainingcapacity as TotalCapacity,id FROM studio WHERE divisionid = CAST(@division AS INTEGER) AND priority = '{studio.Priority}' LIMIT 1", new { division });
                            var imageType = await connection.QueryFirstOrDefaultAsync<string>("SELECT Imagetype from productimage WHERE itemcode  = @itemcode", new { itemcode });
                            if (photoshootProduct != null && photoshootProduct.TotalCapacity != 0)
                            {
                                string priority = string.Empty;
                                if (imageType == "Drape Image" || imageType == "Physical Sample")
                                {
                                    priority = "P1";
                                }
                                else
                                {
                                    priority = "P0";
                                }

                                await connection.ExecuteAsync("INSERT INTO productstudio (productid, studioid, priority,datecreated,itemcode) VALUES (@productid, @studioid, @priority,@datecreate,@itemcode)", new { productid = product.Id, studioid = photoshootProduct.Id, priority = priority, datecreate = DateTime.UtcNow, itemcode });
                                var remainingcapacity = photoshootProduct.TotalCapacity - 1;
                                await connection.ExecuteAsync($"Update studio set remainingcapacity = {remainingcapacity} where id = {photoshootProduct.Id}");
                                var query = await connection.ExecuteAsync("UPDATE Productimage SET  camqcl1qualitycheck= @check, camqcl1qccheckby= @camqcl1qccheckby, camqcl1qccheckdate= @camqcl1qccheckdate WHERE itemcode = @itemcode", new { itemcode, check, camqcl1qccheckby = username, camqcl1qccheckdate = DateTime.UtcNow });
                                checking = true;
                                break;
                            }
                        }

                        if (!checking)
                        {
                            throw new Exception("No capacity");
                        }
                    }
                    else
                    {
                        throw new Exception("No Studio is created.");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task UpdateCAMQCQualityCheck(long itemcode, string? check, string? username)
        {
            try
            {
                using var connection = this.GetConnection();
                var query = await connection.ExecuteAsync(
                   "UPDATE Productimage SET qualitycheck = @check, camqcl2qccheckby = @camqcl2qccheckby, camqcl2qccheckdate = @camqcl2qccheckdate, producttostatus = 'Approved'  WHERE itemcode = @itemcode", new { itemcode, check, camqcl2qccheckby = username, camqcl2qccheckdate = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task CAMQCActionState(long itemcode, string? action, string? rcomment, string? username)
        {
            try
            {
                using var connection = this.GetConnection();
                if (rcomment == null && action != "Fail")
                {
                    await connection.ExecuteAsync("UPDATE Productimage SET camqcL2action = @action, camqcl2qccheckby = @camqcl2qccheckby, camqcl2qccheckdate = @camqcl2qccheckdate WHERE itemcode = @itemcode", new { itemcode, action, camqcl2qccheckby = username, camqcl2qccheckdate = DateTime.UtcNow });
                }
                else if (rcomment != null)
                {
                    await connection.ExecuteAsync("UPDATE Productimage SET qualitycheck = @action,camqcl2rejection=@rcomment, camqcl2qccheckby = @camqcl2qccheckby, camqcl2qccheckdate = @camqcl2qccheckdate WHERE itemcode = @itemcode", new { itemcode, action, rcomment, camqcl2qccheckby = username, camqcl2qccheckdate = DateTime.UtcNow });
                }
            }
            catch
            {
                throw new Exception($"Record not Saved.");
            }
        }

        public async Task CAMQCL2Submit(long[] itemcode, string? submit, string? username)
        {
            try
            {
                using var connection = this.GetConnection();
                foreach (var item in itemcode)
                {
                    await connection.ExecuteAsync("UPDATE Productimage SET allaction = @allaction, camqcl2submit = @submit, camqcl2submitby = @camqcl2submitby, camqcl2submitdate = @camqcl2submitdate WHERE itemcode = @item", new { item, submit, camqcl2submitby = username, camqcl2submitdate = DateTime.UtcNow, allaction = "Approved" });
                }
            }
            catch
            {
                throw new Exception($"Record not Submited.");
            }
        }

        public async Task<List<HandOverPhotoshootStatus>> GetHandOverStatus()
        {
            using var connection = this.GetConnection();
            var plan = await connection.QueryAsync<HandOverPhotoshootStatus>("select * from mtrhandoverstatus");
            return plan.ToList();
        }

        public async Task<List<HandOverPhotoshootStatus>> GetPhotoShootStatus()
        {
            using var connection = this.GetConnection();
            var plan = await connection.QueryAsync<HandOverPhotoshootStatus>("select * from mtrphotoshootstatus");
            return plan.ToList();
        }

        public async Task UpdateAssigned(string[] cities, long[] itemcode, DateTime[] dates, string? username)
        {
            try
            {
                using var connection = this.GetConnection();
                ////var cityCounts = cities
                ////    .GroupBy(city => city)
                ////    .Select(group => new { City = group.Key, Count = group.Count() })
                ////    .ToList();
                ////foreach (var cityCount in cityCounts)
                ////{
                ////    await connection.ExecuteAsync(
                ////        "UPDATE studiocapacity SET Assigned = @Count WHERE city = @City",
                ////        new { Count = cityCount.Count, City = cityCount.City });
                ////    var totalCapacity = await connection.QueryFirstOrDefaultAsync<int>(
                ////    $"SELECT totalcapacity FROM studio WHERE city = @City",
                ////    new { City = cityCount.City });

                ////    var remainingCapacity = totalCapacity - cityCount.Count;
                ////    await connection.ExecuteAsync(
                ////        "UPDATE studio SET remainingcapacity = @RemainingCapacity WHERE city = @City",
                ////        new { RemainingCapacity = remainingCapacity, City = cityCount.City });
                ////}
                foreach (var itemcodes in itemcode)
                {
                    await connection.ExecuteAsync(
                        "UPDATE productimage SET camqcstatus = 2,allocationdate = @Allocationdate, camqcl1submitby = @camqcl1submitby, camqcl1submitdate = @camqcl1submitdate WHERE itemcode = @Itemcode",
                        new { Itemcode = itemcodes, Allocationdate = DateTime.UtcNow, camqcl1submitby = username, camqcl1submitdate = DateTime.UtcNow });
                    await connection.ExecuteAsync(
                        "UPDATE productstudio SET allocationdate = @Allocationdate WHERE itemcode = @Itemcode",
                        new { Itemcode = itemcodes, Allocationdate = DateTime.UtcNow, camqcl1submitby = username, camqcl1submitdate = DateTime.UtcNow });
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public async Task<List<ProcessStatus>> CAMQCProcessStatus()
        {
            try
            {
                using var connection = this.GetConnection();
                var query = "SELECT pi.itemcode as ItemCode,pi.rangeshot as RangeShot,pi.individualstyle as IndividualStyle,pi.catalogshot as CatologShot,pi.camqcl1action as L1Action,pi.qualitycheck as L2qualitycheck,pi.pictureid as PictureId,pi.Stockreturn as StockReturnToQC, pi.camqcl1qualitycheck as camqcl1qualitycheck FROM productimage pi";
                var products = await connection.QueryAsync<ProcessStatus>(query);
                return products.ToList();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public async Task UpdateCAMQCAction(long itemcode, string? status, string? rejectioncomment, string? username)
        {
            try
            {
                using var connection = this.GetConnection();
                if (status != "Fail" && rejectioncomment == null)
                {
                    await connection.ExecuteAsync($"UPDATE productimage SET camqcl1action = '{status}', camqcl1actionby = '{username}', camqcl1actiondate = '{DateTime.UtcNow}'  WHERE itemcode = {itemcode}");
                }
                else if (rejectioncomment != null)
                {
                    await connection.ExecuteAsync($"UPDATE productimage SET camqcl1qualitycheck = '{status}', camqcl1rejection = '{rejectioncomment}' WHERE itemcode = {itemcode}");
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public async Task<List<CAMQcL2ExportExcel>> GetCAMQCL2Records(ProductImageModel productImageModel)
        {
            try
            {
                using var connection = this.GetConnection();
                var startDates = productImageModel.FromDate ?? DateTime.MinValue;
                var endDates = productImageModel.ToDate ?? DateTime.MaxValue;
                var formattedStartDate = startDates.ToString("yyyy/MM/dd");
                var formattedEndDate = endDates.ToString("yyyy/MM/dd");
                var query = await connection.QueryAsync<CAMQcL2ExportExcel>(
                 "SELECT DISTINCT ics.eancode, pi.itemcode, to_char(pi.receiveddate::date::timestamp with time zone, 'yyyy/MM/dd'::text) as receiveddate, " +
                 "pi.imagetype, pi.handoverstatus, pi.photoshootstatus, " +
                 "to_char(ps.allocationdate::date::timestamp with time zone, 'yyyy/MM/dd'::text) as allocationdate, " +
                 "ps.priority, s.city AS StudioLocation, s.name AS StudioName, s.divisionid, " +
                 "s.remainingcapacity AS StudioAvailableCapacity " +
                 "FROM productimage pi " +
                 "JOIN productstudio ps ON pi.productid = ps.productid " +
                 "JOIN studio s ON ps.studioid = s.id " +
                 "LEFT JOIN itemcodestaging AS ics ON pi.itemcode = ics.code " +
                 $"WHERE to_char(pi.receiveddate::date::timestamp with time zone, 'yyyy/MM/dd'::text) BETWEEN '{formattedStartDate}' AND '{formattedEndDate}'");
                var products = query.ToList();
                foreach (var item in products)
                {
                    switch (item.HandoverStatus)
                    {
                        case "1":
                            var mtrQuery1 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 1");
                            item.HandoverStatus = mtrQuery1.Name;
                            break;

                        case "2":
                            var mtrQuery2 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 2");
                            item.HandoverStatus = mtrQuery2.Name;
                            break;
                        case null:
                        case "null":
                            var mtrQuery3 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 2");
                            item.HandoverStatus = mtrQuery3.Name;
                            break;
                    }

                    switch (item.PhotoShootStatus)
                    {
                        case "1":
                            var photshootquery1 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 1");
                            item.PhotoShootStatus = photshootquery1.Name;
                            break;

                        case "2":
                            var photshootquery2 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 2");
                            item.PhotoShootStatus = photshootquery2.Name;
                            break;
                        case null:
                        case "null":
                            var photshootquery4 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 1");
                            item.PhotoShootStatus = photshootquery4.Name;
                            break;
                    }
                }

                return products;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public async Task RejectedItemcode(long itemcode, string rejectedby, string rejctedreason, string? username)
        {
            try
            {
                using var connection = this.GetConnection();
                if (rejectedby != "QC")
                {
                    var itemcodestudio = await connection.QueryFirstOrDefaultAsync<ItemCode>($"select studioid as Code from productstudio where itemcode = {itemcode}");
                    if (itemcodestudio != null)
                    {
                        var checkstudio = await connection.QueryFirstOrDefaultAsync<Studio>("SELECT * FROM studio WHERE id = @StudioId", new { StudioId = itemcodestudio.Code });
                        var capacity = checkstudio.RemainingCapacity + 1;
                        if (checkstudio != null)
                        {
                            await connection.ExecuteAsync($"UPDATE studio SET remainingcapacity={capacity} WHERE id = {itemcodestudio.Code}");
                        }

                        await connection.ExecuteAsync($"DELETE FROM productstudio WHERE itemcode = {itemcode}");
                        var itemcodeimage = await connection.QueryFirstOrDefaultAsync<ProductImage>(
     $"SELECT pictureid AS ImageUrl FROM productimage WHERE itemcode = @ItemCode",
     new { ItemCode = itemcode });

                        if (itemcodeimage.ImageUrl != null)
                        {
                            var idsToDelete = itemcodeimage.ImageUrl.Split(',').Select(id => int.Parse(id.Trim())).ToArray();

                            await connection.ExecuteAsync(
                                "DELETE FROM uploadpicture WHERE id = ANY(@idsToDelete)",
                                new { idsToDelete });
                        }
                    }
                }

                var count = await connection.QueryFirstOrDefaultAsync<string>($"SELECT rejectedcount FROM productimage WHERE itemcode = {itemcode}");
                if (count == null)
                {
                    count = "1";
                }
                else
                {
                    int countValue = int.Parse(count);
                    count = (countValue + 1).ToString();
                }

                var currentValues = await connection.QueryFirstOrDefaultAsync<ReceivedAndHandoverStatus>($"SELECT rejectedby, rejectedreason, rejectedcount, rejecteddate FROM productimage WHERE itemcode = {itemcode}");

                string updatedRejectedBy;
                string updatedRejectedReason;
                string updatedRejectedDate;
                string todayDate = DateTime.Today.ToString("dd/MM/yyyy");

                if (currentValues.Rejectedreason == null)
                {
                    updatedRejectedBy = username + "(" + rejectedby + ")";
                    updatedRejectedReason = rejctedreason;
                    updatedRejectedDate = todayDate;
                }
                else
                {
                    string[] existingDates = currentValues.Rejecteddate.Split(',').Select(date => date.Trim()).ToArray();
                    string newDates = string.Join(", ", new[] { todayDate }.Concat(existingDates));
                    updatedRejectedBy = $"{username}({rejectedby}), {currentValues.Rejectedby}";
                    updatedRejectedReason = $"{rejctedreason}, {currentValues.Rejectedreason}";
                    updatedRejectedDate = newDates;
                }

                string query = $"UPDATE productimage SET  handoverdate=null,\r\n    handoverstatus=null,\r\n   " +
                     $" camqcstatus=null,\r\n    photoshootstatus=null,\r\n    blobimagefolderurl=null,\r\n    allocationdate=null,\r\n camqcl1submitdate=null,\r\n  camqcl1submitby=null,\r\n  " +
                     $" inwarddate=null,\r\n    stockreturn=null,\r\n    stockreturndate=null,\r\n  psreturnsubmitedby=null,\r\n  rangeshot=null,\r\n  psrangeshotby=null,\r\n individualstyle=null,\r\n psindividualby=null,\r\n  " +
                     $"catalogshot=null,\r\n   pscatalogby=null,\r\n  camqcl1actiondate=null,\r\n    rejectioncomments=null,\r\n    pictureid=null," +
                     $"\r\n    stockreturncomments=null,\r\n    rangeshotdate=null,\r\n  camqcl2qccheckdate=null,\r\n camqcl2qccheckby=null,\r\n camqcl2action=null," +
                     $"\r\n    camqcl2rejection=null,\r\n  camqcl1actionby=null,\r\n  camqcl1qccheckdate=null,\r\n  camqcl1qccheckby=null,\r\n    camqcl1rejection=null," +
                     $"\r\n    contentaction=null,\r\n    contentrejection=null,\r\n    camqcl1qualitycheck=null,\r\n   " +
                     $" catalogshotdate=null,\r\n    individualstyledate=null,\r\n    submitdate=null,\r\n  pssubmitedby=null,\r\n   camqcl2submit=null,\r\n  camqcl2submitdate=null,\r\n  camqcl2submitby=null,\r\n " +
                     $"  samplestatusrejection=null,rejecteditemsubmit=null," +
                     $"rejectedby='{updatedRejectedBy}',rejectedreason='{updatedRejectedReason}',rejectedcount='{count}',rejecteddate='{updatedRejectedDate}', ";
                if (rejectedby == "QC")
                {
                    query += $"   samplestatus='1'  WHERE itemcode = {itemcode} ";
                    await connection.ExecuteAsync(query);
                }
                else if (rejectedby == "CamQCL1")
                {
                    query += $" camqcl1action='1',samplestatus='2'  WHERE itemcode = {itemcode} ";
                    await connection.ExecuteAsync(query);
                }
                else if (rejectedby == "CamQCL2")
                {
                    query += $" qualitycheck='1'  WHERE itemcode = {itemcode} ";
                    await connection.ExecuteAsync(query);
                }
                else if (rejectedby == "Photoshoot")
                {
                    query += $" productreceived='1',camqcl1action='1',samplestatus='2'  WHERE itemcode = {itemcode} ";
                    await connection.ExecuteAsync(query);
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public async Task RejectedItemcodeCAML2(long itemcode, string rejectedby, string rejctedreason, string? userrole)
        {
            try
            {
                using var connection = this.GetConnection();

                var count = await connection.QueryFirstOrDefaultAsync<string>($"SELECT rejectedcount FROM productimage WHERE itemcode = {itemcode}");
                if (count == null)
                {
                    count = "1";
                }
                else
                {
                    int countValue = int.Parse(count);
                    count = (countValue + 1).ToString();
                }

                var currentValues = await connection.QueryFirstOrDefaultAsync<ReceivedAndHandoverStatus>($"SELECT rejectedby, rejectedreason, rejectedcount, rejecteddate FROM productimage WHERE itemcode = {itemcode}");
                string updatedRejectedBy;
                string updatedRejectedReason;
                string updatedRejectedDate;
                string todayDate = DateTime.Today.ToString("dd/MM/yyyy");
                if (currentValues == null)
                {
                    updatedRejectedBy = rejectedby + "(" + userrole + ")";
                    updatedRejectedReason = rejctedreason;
                    updatedRejectedDate = DateTime.Today.ToString("dd/MM/yyyy");
                }
                else
                {
                    string[] existingDates = currentValues.Rejecteddate.Split(',').Select(date => date.Trim()).ToArray();
                    string newDates = string.Join(", ", new[] { todayDate }.Concat(existingDates));
                    updatedRejectedBy = $"{userrole}({rejectedby}), {currentValues.Rejectedby}";
                    updatedRejectedReason = $"{rejctedreason}, {currentValues.Rejectedreason}";
                    updatedRejectedDate = newDates;
                }

                // var picture = await connection.QueryFirstOrDefaultAsync<string>($"SELECT pictureid FROM productimage WHERE itemcode = {itemcode}");
                // if (picture != null)
                // {
                //    var deleteQuery = $"DELETE FROM photoshootuploadpicture  WHERE id IN ({picture})";
                //    await connection.ExecuteAsync(deleteQuery);
                // }
                string query = $"UPDATE productimage SET  photoshootstatus= null,inwarddate= null,stockreturn= null,stockreturndate= null,rangeshot= null,rejecteditemsubmit='yes'," +
                    $"individualstyle= null,catalogshot= null,productreceived= null,rejectioncomments= null,stockreturncomments= null,rangeshotdate= null," +
                    $"camqcl2action= '1',contentaction= null,catalogshotdate= null,individualstyledate= null,submitdate= null,psactiondate= null,psactionby= null,psindividualby= null," +
                    $"pscatalogby= null,pssubmitedby= null,psreturnsubmitedby= null,psrangeshotby= null,camqcl2rejection= null, " +
                    $"rejectedby='{updatedRejectedBy}',rejectedreason='{updatedRejectedReason}',rejectedcount='{count}',rejecteddate='{updatedRejectedDate}', ";

                query += $" qualitycheck='1'  WHERE itemcode = {itemcode} ";
                await connection.ExecuteAsync(query);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public async Task<List<ReceivedAndHandoverStatus>> GetRejectedItemcode()
        {
            try
            {
                using var connection = this.GetConnection();
                var resultList = new List<ReceivedAndHandoverStatus>();
                var query = await connection.QueryAsync<ReceivedAndHandoverStatus>($"Select DISTINCT pi.id,pi.itemcode,ics.eancode,pi.imagetype,pi.receiveddate,pi.handoverdate,pi.handoverstatus,pi.imageurl,pi.camqcl1action as CamQCStatus,pi.stockreturncomments as Comments,pi.photoshootstatus,pi.samplestatus,pi.stockreturn,pi.statusdate,pi.camqcl1rejection,pi.divisionid as DivisionId,pi.qcaction,pi.rejectedreason,pi.rejectedby,pi.rejectedcount, pi.productsubmitdate, pi.rejecteditemsubmitdate, pi.rejecteditemsubmit,pi.rejecteddate from productimage as pi LEFT JOIN itemcodestaging AS ics ON pi.itemcode = ics.code LEFT JOIN product AS pro ON pro.uid = ics.uidcode where pro.isactive = true AND rejectedby is not null and rejectedreason is not null order by id desc "); // where  order by id desc ");
                var results = query.ToList();

                foreach (var result in results)
                {
                    switch (result.HandoverStatus)
                    {
                        case "1":
                            var mtrQuery1 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 1");
                            result.HandoverStatus = mtrQuery1.Name;
                            break;

                        case "2":
                            var mtrQuery2 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 2");
                            result.HandoverStatus = mtrQuery2.Name;
                            break;
                        case null:
                        case "null":
                            var mtrQuery3 = await connection.QueryFirstOrDefaultAsync<MtrHandoverStatus>("SELECT name FROM mtrhandoverstatus WHERE id = 2");
                            result.HandoverStatus = mtrQuery3.Name;
                            break;
                    }

                    switch (result.PhotoShootStatus)
                    {
                        case "1":
                            var photshootquery1 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 1");
                            result.PhotoShootStatus = photshootquery1.Name;
                            break;

                        case "2":
                            var photshootquery2 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 2");
                            result.PhotoShootStatus = photshootquery2.Name;
                            break;
                        case null:
                        case "null":
                            result.PhotoShootStatus = null;
                            break;
                    }
                }

                return results.OrderBy(r =>
                {
                    var samplestatus = string.Empty;
                    if (r.SampleStatus == "Rejected" || r.SampleStatus == "1")
                    {
                        samplestatus = "Rejected";
                    }
                    else
                    {
                        samplestatus = "Submitted";
                    }

                    return samplestatus;
                }).ThenByDescending(r =>
                {
                    var samplestatus = string.Empty;
                    var lastRejectedDate = DateTime.MinValue;
                    if (r.SampleStatus == "Rejected" || r.SampleStatus == "1")
                    {
                        samplestatus = "Rejected";
                        var rejectedDate = r.Rejecteddate;
                        string[] dates = rejectedDate.Split(',');
                        string lastDateString = dates[dates.Length - 1].Trim();
                        lastRejectedDate = DateTime.ParseExact(lastDateString, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        samplestatus = "Submitted";
                    }

                    return samplestatus == "Rejected" ? lastRejectedDate : r.ProductSubmitDate;
                }).ToList();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
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
    }
}