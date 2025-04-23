using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.Master;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace HomeCentre.PIM.BAL.Implementations
{
    public class PgPhotoShootManager : PgEntityAction<ProductImage>, IPhotoShootManager
    {
        private readonly IUserContext _userContext;
        private readonly IConfiguration _configuration;

        public PgPhotoShootManager(IUserContext userContext, IDbConnectionFactory connectionFactory, IConfiguration configuration)
            : base(userContext, connectionFactory, configuration)
        {
            _userContext = userContext;
            _configuration = configuration;
        }

        public async Task<List<MtrProductImageType>> Photoshootstatus()
        {
            try
            {
                using var connection = this.GetConnection();
                var mtrProductImageType = await connection.QueryAsync<MtrProductImageType>("SELECT * FROM mtrphotoshootstatus");
                return mtrProductImageType.ToList();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public async Task UpdateInward(long itemcode, DateTime? inwardDate)
        {
            try
            {
                using var connection = this.GetConnection();
                var query = "UPDATE productimage SET inwarddate = @InwardDate, allocationdate = @InwardDate WHERE itemcode = @ItemCode";
                await connection.ExecuteAsync(query, new { ItemCode = itemcode, InwardDate = inwardDate });
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public async Task UpdateStockReturnDate(long itemcode, DateTime? stockreturndate)
        {
            try
            {
                using var connection = this.GetConnection();
                var query = "UPDATE productimage SET stockreturndate = @StockReturnDate WHERE itemcode = @ItemCode";
                await connection.ExecuteAsync(query, new { ItemCode = itemcode, StockReturnDate = stockreturndate });
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public async Task<byte[]> PhotoShootExportExcel(ImageTypeDropdown imageTypeDropdown)
        {
            try
            {
                using var connection = this.GetConnection();
                var startDates = imageTypeDropdown.FromDate ?? DateTime.MinValue;
                var endDates = imageTypeDropdown.ToDate ?? DateTime.MaxValue;
                var formattedStartDate = startDates.ToString("yyyy/MM/dd 00:00:00");
                var formattedEndDate = endDates.ToString("yyyy/MM/dd 23:59:59");
                var imageType = imageTypeDropdown.ImageType;
                string[] cols = new string[] { "ItemCode", "EANCode", "IndividualShot", "CatalogueShot", "ImageURL", "Division", "SampleType", "ReceivedDate", "Priority", "StudioName", "StudioLocation", "InwardDate", "PhotoShootStatus", "StockReturnToQC", "StockReturnDate", "RangeShot" };
                var query = $" SELECT DISTINCT * FROM (SELECT pi.itemcode AS ItemCode,pi.id,pi.datemodified,TO_CHAR(pi.receiveddate, 'DD/MM/YY') AS receiveddate,pi.imageurl AS ImageURL, pi.imagetype AS SampleType, pi.productreceived,pi.pictureid,TO_CHAR(pi.inwarddate, 'DD/MM/YY') AS inwarddate,TO_CHAR(pi.stockreturndate, 'DD/MM/YY') AS stockreturndate,pi.imagetype AS Imagetype, pi.submitdate AS SubmitDate, pi.camqcl1submitdate AS CamQcL1SubmitDate,ics.eancode AS EanCode,pi.rangeshot,pi.individualstyle AS IndividualShot,pi.stockreturncomments AS comments,ps.priority AS Priority,s.name AS StudioName,pi.divisionid AS DivisionId,s.city AS Studiolocation,pi.photoshootstatus AS photoshootstatus,pi.stockreturn AS StockReturnToQC,d.name AS division, pi.catalogshot AS CatalogueShot,CASE WHEN pi.photoshootstatus IS NULL THEN 0 ELSE 1 END AS photoshootstatus_order FROM  photoshootproduct p JOIN  productimage pi ON p.id = pi.productid  LEFT JOIN productstudio ps ON p.id = ps.productid LEFT JOIN  studio s ON s.id = ps.studioid JOIN division d ON d.id = pi.divisionid " +
                $"LEFT JOIN itemcodestaging AS ics ON pi.itemcode = ics.code LEFT JOIN product AS pro ON pro.uid = ics.uidcode  LEFT JOIN mtrproductimagetype mit ON mit.name = pi.imagetype  WHERE pro.isactive = true and TO_CHAR(pi.receiveddate::date::timestamp WITH TIME ZONE, 'yyyy/MM/dd HH24:MI:SS'::text) BETWEEN '{formattedStartDate}' AND '{formattedEndDate}' AND pi.camqcstatus = 2 AND CAST(d.code AS INTEGER) IN ({imageTypeDropdown.UserDivision})";
                if (imageType != "All")
                {
                    query += $" AND pi.imagetype IN ('{imageType}') ";
                }

                query += " ) AS subquery ORDER BY subquery.datemodified desc";
                var data = (await connection.QueryAsync<PhotoShootExportModel>(query)).ToList();

                foreach (var item in data)
                {
                    switch (item.PhotoShootStatus)
                    {
                        case "1":
                            var photoshoot1 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 1");
                            item.PhotoShootStatus = photoshoot1.Name;
                            break;

                        case "2":
                            var photoshoot2 = await connection.QueryFirstOrDefaultAsync<MtrPhotoShootStatus>("SELECT name FROM mtrphotoshootstatus WHERE id = 2");
                            item.PhotoShootStatus = photoshoot2.Name;
                            break;
                        case null:
                        case "null":
                            item.PhotoShootStatus = string.Empty;
                            break;
                    }

                    switch (item.StockReturnToQC)
                    {
                        case "1":
                            item.StockReturnToQC = "Returned";
                            break;
                        case null:
                        case "null":
                            item.StockReturnToQC = string.Empty;
                            break;
                        default:
                            item.StockReturnToQC = "Yet To Return";
                            break;
                    }

                    switch (item.RangeShot)
                    {
                        case "1":
                            item.RangeShot = "Done";
                            break;
                        case null:
                        case "null":
                            item.RangeShot = string.Empty;
                            break;
                        default:
                            item.RangeShot = "Not Applicable";
                            break;
                    }
                }

                // var result = data.OrderByDescending(i =>
                // {
                //    var photoshootStatus = i.PhotoShootStatus;
                //    if (photoshootStatus.IsNullOrEmpty())
                //    {
                //        photoshootStatus = "Pending";
                //    }

                // return photoshootStatus;
                // }).ThenByDescending(i =>
                // {
                //    if (i.PhotoShootStatus == "Pending" || i.PhotoShootStatus.IsNullOrEmpty())
                //    {
                //        return i.CamQcL1SubmitDate;
                //    }
                //    else
                //    {
                //        return i.SubmitDate;
                //    }
                // }).ToList();
                var responseDataTable = this.ToDataTable(data);
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("PhotoShootExport");

                if (data.Any())
                {
                    var headers = new[] { "ItemCode", "EANCode", "IndividualShot", "CatalogueShot", "ImageURL", "Division", "SampleType", "ReceivedDate", "Priority", "StudioName", "StudioLocation", "InwardDate", "PhotoShootStatus", "StockReturnToQC", "StockReturnDate", "RangeShot" };

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
                    foreach (var record in data)
                    {
                        worksheet.Cells[row, 1].Value = record.Itemcode.ToString();
                        worksheet.Cells[row, 2].Value = record.EANCode.ToString();
                        worksheet.Cells[row, 3].Value = record.IndividualShot;
                        worksheet.Cells[row, 4].Value = record.CatalogueShot;
                        worksheet.Cells[row, 5].Value = record.ImageURL;
                        worksheet.Cells[row, 6].Value = record.Division;
                        worksheet.Cells[row, 7].Value = record.SampleType;
                        worksheet.Cells[row, 8].Value = record.ReceivedDate;
                        worksheet.Cells[row, 9].Value = record.Priority;
                        worksheet.Cells[row, 10].Value = record.StudioName;
                        worksheet.Cells[row, 11].Value = record.StudioLocation;
                        worksheet.Cells[row, 12].Value = record.InwardDate;
                        worksheet.Cells[row, 13].Value = record.PhotoShootStatus;
                        worksheet.Cells[row, 14].Value = record.StockReturnToQC;
                        worksheet.Cells[row, 15].Value = record.StockReturnDate;
                        worksheet.Cells[row, 16].Value = record.RangeShot;

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
                    worksheet.Column(i).Width = 20;
                }

                using var memoryStream = new MemoryStream();
                package.SaveAs(memoryStream);
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
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
