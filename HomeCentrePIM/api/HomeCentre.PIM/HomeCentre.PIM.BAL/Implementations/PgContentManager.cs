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
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace HomeCentre.PIM.BAL.Implementations
{
    public class PgContentManager : PgEntityAction<ProductImage>, IContentManager
    {
        private readonly IUserContext _userContext;
        private readonly IConfiguration _configuration;

        public PgContentManager(IUserContext userContext, IDbConnectionFactory connectionFactory, IConfiguration configuration)
            : base(userContext, connectionFactory, configuration)
        {
            _userContext = userContext;
            _configuration = configuration;
        }

        public async Task<List<ContentModel>> GetContentRecords([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] string imageid)
        {
            var startDates = fromDate ?? DateTime.MinValue;
            var endDates = toDate ?? DateTime.MaxValue;
            var formattedStartDate = startDates.ToString("yyyy/MM/dd 00:00:00");
            var formattedEndDate = endDates.ToString("yyyy/MM/dd 23:59:59");

            using var connection = this.GetConnection();

            var query = $"SELECT DISTINCT pi.itemcode, pi.imagetype as image, pi.divisionid as DivisionId, pi.pictureid as Pictureid, " +
                $"pi.stockreturncomments as PhotoshootComment,pi.stockreturn as Stockreturn, pi.camqcl2submitdate, ics.eancode, to_char(pi.receiveddate, 'DD/MM/YY') as receiveddate, " +
                $"to_char(pi.rangeshotdate, 'DD/MM/YY') as Rangeshotdate, to_char(pi.catalogshotdate, 'DD/MM/YY') as Catalogshotdate, " +
                $"to_char(pi.individualstyledate, 'DD/MM/YY') as Individualstyledate, pi.photoshootstatus, ps.priority, " +
                $"s.city as StudioLocation, s.name as StudioName, pi.contentaction as Contentaction " +
                $"FROM productimage pi " +
                $"JOIN productstudio ps ON pi.productid = ps.productid " +
                $"JOIN studio s ON ps.studioid = s.id " +
                $"LEFT JOIN itemcodestaging AS ics ON pi.itemcode = ics.code " +
                $"LEFT JOIN product AS pro ON pro.uid = ics.uidcode " +
                $"LEFT JOIN mtrproductimagetype mit ON mit.name = pi.imagetype " +
                $"WHERE pro.isactive = true and pi.camqcl2submit='1'";
            if (fromDate != null || toDate != null)
            {
                query += $" And to_char(pi.receiveddate::date::timestamp with time zone, 'yyyy/MM/dd HH24:MI:SS') BETWEEN '{formattedStartDate}' AND '{formattedEndDate}' ";
            }

            if (!string.IsNullOrEmpty(imageid))
            {
                query += $" AND pi.divisionid IN({imageid}) ";
            }

            var resultList = (await connection.QueryAsync<ContentModel>(query)).ToList();

            foreach (var result in resultList)
            {
                switch (result.PhotoShootStatus)
                {
                    case "1":
                        var mtrQuery1 = await connection.QueryFirstOrDefaultAsync<string>("SELECT name FROM mtrphotoshootstatus WHERE id = 1");
                        result.PhotoShootStatus = mtrQuery1;
                        break;

                    case "2":
                        var mtrQuery2 = await connection.QueryFirstOrDefaultAsync<string>("SELECT name FROM mtrphotoshootstatus WHERE id = 2");
                        result.PhotoShootStatus = mtrQuery2;
                        break;
                    case null:
                    case "null":
                        result.PhotoShootStatus = null;
                        break;
                }
            }

            return resultList.OrderByDescending(rl => rl.CamQcL2SubmitDate).ToList();
        }

        public async Task UpdateContentstate(long itemcode, string? action, string? rcomment)
        {
            try
            {
                using var connection = this.GetConnection();
                await connection.ExecuteAsync("UPDATE Productimage SET contentaction = @action,contentrejection=@rcomment WHERE itemcode= @itemcode", new { itemcode, action, rcomment });
            }
            catch
            {
                throw new Exception($"Record not Saved.");
            }
        }

        public async Task<Stream> ContentExportExcel([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, string? imageType)
        {
            try
            {
                using var connection = this.GetConnection();
                var startDates = fromDate ?? DateTime.MinValue;
                var endDates = toDate ?? DateTime.MaxValue;
                var formattedStartDate = startDates.ToString("yyyy/MM/dd 00:00:00");
                var formattedEndDate = endDates.ToString("yyyy/MM/dd 23:59:59");
                var results = new List<ContentExportModel>();
                string[] cols = new string[] { "ItemCode", "EANCode", "Image", "Division", "ReceivedDate", "Priority", "StudioName", "StudioLocation", "PhotoshootStatus", "WebsiteLiveDate", "RangeShotDate", "IndividualShotDate", "CatalogueShotDate" };
                string query = "SELECT DISTINCT pi.itemcode ,pi.imagetype as image,pi.pictureid as Pictureid, to_char(pi.camqcl2submitdate, 'DD/MM/YY') as CamQcL2SubmitDate, ics.eancode As EANcode,to_char(pi.receiveddate, 'DD/MM/YY') as receiveddate,to_char(pi.rangeshotdate, 'DD/MM/YY') as RangeShotDate," +
                    "to_char(pi.catalogshotdate, 'DD/MM/YY') as CatalogueShotDate,to_char(pi.individualstyledate, 'DD/MM/YY') as IndividualShotDate, pi.photoshootstatus, ps.priority, s.city as StudioLocation, pi.stockreturn as StockReturnToQC," +
                    " s.name as StudioName,pi.contentaction as Contentaction,d.name as Division " +
                "FROM productimage pi JOIN productstudio ps ON pi.productid = ps.productid " +
                "LEFT JOIN mtrproductimagetype mit ON mit.name = pi.imagetype " +
                "LEFT JOIN itemcodestaging AS ics ON pi.itemcode = ics.code " +
                $"LEFT JOIN product AS pro ON pro.uid = ics.uidcode " +
                "JOIN division d ON pi.divisionid = d.id JOIN studio s ON ps.studioid = s.id where pro.isactive = true and pi.camqcl2submit='1'";
                if (fromDate != null || toDate != null)
                {
                    query += $" And to_char(pi.receiveddate::date::timestamp with time zone, 'yyyy/MM/dd HH24:MI:SS') BETWEEN '{formattedStartDate}' AND '{formattedEndDate}'";
                }

                if (imageType != null)
                {
                    query += $" AND pi.divisionid IN ({imageType})";
                }

                var details = await connection.QueryAsync<ContentExportModel>(query);
                results = details.ToList();
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
                            var mtrQuery4 = await connection.QueryFirstOrDefaultAsync<string>("SELECT name FROM mtrphotoshootstatus WHERE id = 1");
                            data.PhotoShootStatus = mtrQuery4;
                            break;
                    }
                }

                foreach (var data in results)
                {
                    switch (data.StockReturnToQC)
                    {
                        case "0":
                            var stockreturn1 = await connection.QueryFirstOrDefaultAsync<string>("SELECT jsonvalue -> 0 ->> 'text' AS text_value FROM appsettings WHERE name = 'StockReturnToqc' ");
                            data.StockReturnToQC = stockreturn1;
                            break;

                        case "1":
                            var stockreturn2 = await connection.QueryFirstOrDefaultAsync<string>("SELECT jsonvalue -> 1 ->> 'text' AS text_value FROM appsettings WHERE name = 'StockReturnToqc' ");
                            data.StockReturnToQC = stockreturn2;
                            break;
                        case null:
                        case "null":
                            data.StockReturnToQC = " ";
                            break;
                    }
                }

                var result = results.OrderByDescending(r => r.CamQcL2SubmitDate).ToList();

                var responseDataTable = this.ToDataTable(result);

                return new FileExportHelper().ExportToExcel(responseDataTable.SetColumnsOrder(cols));
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