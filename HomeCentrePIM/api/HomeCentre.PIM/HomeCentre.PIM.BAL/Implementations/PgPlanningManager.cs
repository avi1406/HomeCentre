using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Dapper;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.Master;
using HomeCentre.PIM.DAL.Staging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace HomeCentre.PIM.BAL.Implementations
{
    public class PgPlanningManager : PgEntityAction<Planning>, IPlanningManager
    {
        public PgPlanningManager(IUserContext userContext, IDbConnectionFactory connectionFactory, IConfiguration configuration)
            : base(userContext, connectionFactory, configuration)
        {
        }

        public async Task<List<Planning>> GetPlanningDetails()
        {
            using var connection = this.GetConnection();
            var plan = await connection.QueryAsync<Planning>("SELECT DISTINCT s.divisionid, pi.divisionid as DivisionId, pi.itemcode, ics.eancode, pi.photoshootstatus, pi.submitdate AS SubmitDate, pi.camqcl1submitdate AS CamQcL1SubmitDate, pi.qualitycheck as CamQcStatus, " +
                                      "pi.stockreturncomments AS PhotoshootComment, pi.stockreturn as StockReturnToQc, d.name AS division, TO_CHAR(g.grn_date, 'DD/MM/YYYY') AS GrnDate, w_grn.name as GrnLocation, " +
                                      "w_from.name as From_Loc, w_to.name as To_Loc, TO_CHAR(t.to_date, 'DD/MM/YYYY') as TO_Date, s.name as Studio, pi.producttostatus as ProductToStatus, pi.id " +
                                      "FROM studio s " +
                                      "INNER JOIN productstudio ps ON ps.studioid = s.id " +
                                      "INNER JOIN photoshootproduct p ON ps.productid = p.id " +
                                      "RIGHT JOIN productimage pi ON pi.productid = p.id " +
                                      "LEFT JOIN division d ON pi.divisionid = d.id " +
                                      "LEFT JOIN itemcodestaging AS ics ON pi.itemcode = ics.code " +
                                      "LEFT JOIN product AS pro ON pro.uid = ics.uidcode " +
                                      "LEFT JOIN grn g ON g.itemcode = pi.itemcode " +
                                      "LEFT JOIN transferorder t ON t.itemcode = pi.itemcode " +
                                      "LEFT JOIN warehouse w_to ON t.to_loc = w_to.code " +
                                      "LEFT JOIN warehouse w_from ON t.from_loc = w_from.code " +
                                      "LEFT JOIN grn t_grn ON t.itemcode = t_grn.itemcode " +
                                      "LEFT JOIN warehouse w_grn ON t_grn.grn_location = w_grn.code " +
                                      "WHERE pro.isactive = true AND pi.imagetype = 'GRN' " +
                                      "AND (pi.samplestatus = 'Approved' OR pi.grnstatus = '1' OR pi.grnstatus = '2') " +
                                      "ORDER BY pi.id DESC");

            var resultList = plan.ToList();
            foreach (var result in resultList)
            {
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

            // Modified return statement to order by pi.id DESC first
            return resultList.OrderByDescending(i => i.Id) // Ordering by pi.id in descending order
                             .ThenByDescending(i =>
                             {
                                 var photoshootStatus = i.PhotoShootStatus;
                                 if (i.PhotoShootStatus.IsNullOrEmpty())
                                 {
                                     photoshootStatus = "Pending";
                                 }

                                 return photoshootStatus;
                             }).ThenByDescending(i =>
                             {
                                 if (i.PhotoShootStatus == "1" || i.PhotoShootStatus == "Pending" || i.PhotoShootStatus == null)
                                 {
                                     return i.CamQcL1SubmitDate;
                                 }
                                 else
                                 {
                                     return i.SubmitDate;
                                 }
                             }).ToList();
        }

        public async Task<byte[]> PlanningExportExcel([FromBody] ProductImageModel productImageModel)
        {
            try
            {
                using var connection = this.GetConnection();
                var results = new List<Planning>();
                string[] cols = new string[] { "ItemCode", "EANCode", "GRNDate", "Division", "GRNLocation", "FromLOC", "TOLoc", "TODate", "AllocationStudio", "Status", "PhotoShootStatus", "StockReturn", "CAMQCL2Status" };
                results = (await connection.QueryAsync<Planning>("SELECT DISTINCT s.divisionid, pi.divisionid as DivisionId, pi.itemcode, ics.eancode, pi.photoshootstatus, pi.submitdate AS SubmitDate, pi.camqcl1submitdate AS CamQcL1SubmitDate, pi.qualitycheck as CamQcStatus, " +
                                      "pi.stockreturncomments AS PhotoshootComment, pi.stockreturn as StockReturnToQc, d.name AS division, TO_CHAR(g.grn_date, 'DD/MM/YYYY') AS GrnDate, w_grn.name as GrnLocation, " +
                                      "w_from.name as From_Loc, w_to.name as To_Loc, TO_CHAR(t.to_date, 'DD/MM/YYYY') as TO_Date, s.name as Studio, pi.producttostatus as ProductToStatus, pi.id " +
                                      "FROM studio s " +
                                      "INNER JOIN productstudio ps ON ps.studioid = s.id " +
                                      "INNER JOIN photoshootproduct p ON ps.productid = p.id " +
                                      "RIGHT JOIN productimage pi ON pi.productid = p.id " +
                                      "LEFT JOIN division d ON pi.divisionid = d.id " +
                                      "LEFT JOIN itemcodestaging AS ics ON pi.itemcode = ics.code " +
                                      "LEFT JOIN product AS pro ON pro.uid = ics.uidcode " +
                                      "LEFT JOIN grn g ON g.itemcode = pi.itemcode " +
                                      "LEFT JOIN transferorder t ON t.itemcode = pi.itemcode " +
                                       "LEFT JOIN warehouse w_to ON t.to_loc = w_to.code " +
                                      "LEFT JOIN warehouse w_from ON t.from_loc = w_from.code " +
                                      "LEFT JOIN grn t_grn ON t.itemcode = t_grn.itemcode " +
                                      "LEFT JOIN warehouse w_grn ON t_grn.grn_location = w_grn.code " +
                                      "WHERE pro.isactive = true AND pi.imagetype = 'GRN' " +
                                      "AND (pi.samplestatus = 'Approved' OR pi.grnstatus = '1' OR pi.grnstatus = '2') " +
                                      "ORDER BY pi.id DESC")).ToList();

                foreach (var data in results)
                {
                    switch (data.PhotoShootStatus)
                    {
                        case "1":
                            var mtrQuery1 = await connection.QueryFirstOrDefaultAsync<string>("SELECT name FROM mtrphotoshootstatus WHERE id = 1");
                            data.PhotoShootStatus = mtrQuery1;
                            break;

                        case "2":
                            var mtrQuery2 = await connection.QueryFirstOrDefaultAsync<string>("SELECT name FROM mtrphotoshootstatus WHERE id = 2");
                            data.PhotoShootStatus = mtrQuery2;
                            break;
                        case null:
                        case "null":
                            data.PhotoShootStatus = string.Empty;
                            break;
                    }

                    switch (data.StockReturnToQc)
                    {
                        case "0":
                            data.StockReturnToQc = "Yet To Return";
                            break;

                        case "1":
                            data.StockReturnToQc = "Returned";
                            break;
                        case null:
                        case "null":
                            data.StockReturnToQc = string.Empty;
                            break;
                    }
                }

                // var result = results.OrderByDescending(i =>
                // {
                //    var photoshootStatus = i.PhotoShootStatus;
                //    if (i.PhotoShootStatus.IsNullOrEmpty())
                //    {
                //        photoshootStatus = "Pending";
                //    }

                // return photoshootStatus;
                // }).ThenByDescending(i =>
                // {
                //    if (i.PhotoShootStatus == "1" || i.PhotoShootStatus == "Pending" || i.PhotoShootStatus == null)
                //    {
                //        return i.CamQcL1SubmitDate;
                //    }
                //    else
                //    {
                //        return i.SubmitDate;
                //    }
                // }).ToList();
                var result = results.OrderByDescending(i => i.Id) // Sort by pi.id DESC first
                .ThenByDescending(i =>
                {
                    var photoshootStatus = i.PhotoShootStatus;
                    if (i.PhotoShootStatus.IsNullOrEmpty())
                    {
                        photoshootStatus = "Pending";
                    }

                    return photoshootStatus;
                })
                .ThenByDescending(i =>
                {
                    if (i.PhotoShootStatus == "1" || i.PhotoShootStatus == "Pending" || i.PhotoShootStatus == null)
                    {
                        return i.CamQcL1SubmitDate;
                    }
                    else
                    {
                        return i.SubmitDate;
                    }
                })
                .ToList();

                var responseDataTable = this.ToDataTable(result);
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("GRNRecords");

                if (result.Any())
                {
                    var headers = cols;

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
                    foreach (var record in result)
                    {
                        worksheet.Cells[row, 1].Value = record.ItemCode.ToString();
                        worksheet.Cells[row, 2].Value = record.EanCode.ToString();
                        worksheet.Cells[row, 3].Value = record.GrnDate;
                        worksheet.Cells[row, 4].Value = record.Division;
                        worksheet.Cells[row, 5].Value = record.GrnLocation;
                        worksheet.Cells[row, 6].Value = record.From_Loc;
                        worksheet.Cells[row, 7].Value = record.To_Loc;
                        worksheet.Cells[row, 8].Value = record.TO_Date;
                        worksheet.Cells[row, 9].Value = record.Studio;
                        worksheet.Cells[row, 10].Value = record.ProductToStatus;
                        worksheet.Cells[row, 11].Value = record.PhotoShootStatus;
                        worksheet.Cells[row, 12].Value = record.StockReturnToQc;
                        worksheet.Cells[row, 13].Value = record.CamQcStatus;

                        for (int col = 1; col <= headers.Length; col++)
                        {
                            var dataCell = worksheet.Cells[row, col];
                            dataCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
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

                for (int i = 1; i <= 16; i++)
                {
                    worksheet.Column(i).Width = 16;
                }

                using var memoryStream = new MemoryStream();
                package.SaveAs(memoryStream);
                return memoryStream.ToArray();
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
