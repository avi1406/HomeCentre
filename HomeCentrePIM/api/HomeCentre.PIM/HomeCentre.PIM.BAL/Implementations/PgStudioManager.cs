using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Dapper;
using DocumentFormat.OpenXml.Spreadsheet;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.DomainModel;
using HomeCentre.PIM.DAL.Master;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace HomeCentre.PIM.BAL.Implementations
{
    public class PgStudioManager : PgEntityAction<Studio>, IStudioManager
    {
        private readonly IUserContext _userContext;
        private readonly IBlobRepo _blobRepo;
        private readonly IConfiguration _configuration;

        public PgStudioManager(IUserContext userContext, IDbConnectionFactory connectionFactory, IBlobRepo blobRepo, IConfiguration configuration)
            : base(userContext, connectionFactory, configuration)
        {
            _userContext = userContext;
            _blobRepo = blobRepo;
            _configuration = configuration;
        }

        public async Task<Studio> AddOrUpdateStudio(Studio studio)
        {
            using var connection = this.GetConnection();
            var remainingCapacity = await connection.QueryFirstOrDefaultAsync<Studio>($"select * from studio where name='{studio.Name}'");
            if (studio.Id == 0 || studio.Id == null)
            {
                if (remainingCapacity != null && remainingCapacity.DivisionId == studio.DivisionId)
                {
                    throw new Exception($"{studio.Name} Studio Already created");
                }
            }

            var studiocode = await connection.QueryAsync<dynamic>($"select 1 from studio where code = {studio.Code}");
            if (studiocode.Count() > 0)
            {
                throw new Exception($"Studio Code '{studio.Code}' already Exists");
            }

            var studios = await this.AddOrUpdate(studio);
            long totalCapacity;
            long remainingcapacitys = 0;
            if (studios.Week5Capacity == null)
            {
                totalCapacity = studios.Week1Capacity + studios.Week2Capacity + studios.Week3Capacity + studios.Week4Capacity;
            }
            else
            {
                totalCapacity = studios.Week1Capacity + studios.Week2Capacity + studios.Week3Capacity + studios.Week4Capacity + (studios.Week5Capacity ?? 0);
            }

            if (studio.Id != null)
            {
                if (remainingCapacity != null)
                {
                    if (remainingCapacity.TotalCapacity == totalCapacity)
                    {
                        remainingcapacitys = remainingCapacity.RemainingCapacity;
                    }
                    else
                    {
                        if (totalCapacity > remainingCapacity.TotalCapacity)
                        {
                            long add = totalCapacity - remainingCapacity.TotalCapacity;
                            remainingcapacitys = remainingCapacity.RemainingCapacity + add;
                        }
                        else
                        {
                            long add = remainingCapacity.TotalCapacity - totalCapacity;
                            remainingcapacitys = remainingCapacity.RemainingCapacity - add;
                        }
                    }
                }
                else
                {
                    remainingcapacitys = totalCapacity;
                }
            }

            if (studio.Id != null && studio.EffectivefromDate?.Date != null && studio.IsActive != false)
            {
                await connection.ExecuteAsync("UPDATE studio SET TotalCapacity = @TotalCapacity, RemainingCapacity = @RemainingCapacity,IsActive = @IsActive WHERE id = @StudioId",
       new
       {
           TotalCapacity = totalCapacity,
           RemainingCapacity = remainingcapacitys != 0 ? remainingcapacitys : totalCapacity,
           IsActive = studio.EffectivefromDate?.Date <= DateTime.Today,
           StudioId = studios.Id
       });
            }
            else
            {
                await connection.ExecuteAsync("UPDATE studio SET TotalCapacity = @TotalCapacity, RemainingCapacity = @RemainingCapacity, IsActive = @IsActive WHERE id = @StudioId",
      new
      {
          TotalCapacity = totalCapacity,
          RemainingCapacity = totalCapacity,
          IsActive = false,
          StudioId = studios.Id
      });
            }

            if (studio.EffectivefromDate?.Date == null)
            {
                await connection.ExecuteAsync("UPDATE studio SET IsActive = @IsActive WHERE id = @StudioId",
             new
             {
                 StudioId = studios.Id,
                 IsActive = false
             });
            }

            var query = @"
            INSERT INTO studiocapacity(studioid, city) 
            VALUES (@StudioId, @City)";
            await connection.ExecuteAsync(query, new { StudioId = studios.Id, City = studios.City });

            return studios;
        }

        public async Task Uploadstudio(string blobUrl)
        {
            using var connection = this.GetConnection();
            var tempFile = await _blobRepo.DownloadFile(blobUrl);

            using var wb = new XLWorkbook(tempFile, XLEventTracking.Disabled);
            var ws = wb.Worksheet(1);
            var dataTable = ws.Tables.Any() ? ws.Tables.First().AsNativeDataTable() : ws.RangeUsed().AsTable().AsNativeDataTable();

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
            if (!dataRows.Any())
            {
                throw new Exception("Uploaded Excel file has no data.");
            }

            foreach (DataRow row in dataTable.Rows)
            {
                var name = row["Studio Name"]?.ToString();
                var division = row["Division"]?.ToString();
                var divisionID = await connection.QueryFirstOrDefaultAsync<Division>($"select * from division where name='{division}'");
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(divisionID?.Code))
                {
                    throw new Exception("Studio name or division is missing.");
                }

                var checkstudio = await connection.QueryFirstOrDefaultAsync<Studio>(
                    "SELECT * FROM studio WHERE name = @Name AND divisionId::text = @Division",
                    new { Name = name, Division = divisionID.Code });

                if (checkstudio == null)
                {
                    throw new Exception($"Studio {name} does not exist.");
                }
                else
                {
                    if (row["Week1 Capacity"] == null || !IsNumeric(row["Week1 Capacity"]) || Convert.ToInt64(row["Week1 Capacity"]) < 0)
                    {
                        throw new Exception("Week1 Capacity must be a non-null, non-negative integer.");
                    }

                    if (row["Week2 Capacity"] == null || !IsNumeric(row["Week2 Capacity"]) || Convert.ToInt64(row["Week2 Capacity"]) < 0)
                    {
                        throw new Exception("Week2 Capacity must be a non-null, non-negative integer.");
                    }

                    if (row["Week3 Capacity"] == null || !IsNumeric(row["Week3 Capacity"]) || Convert.ToInt64(row["Week3 Capacity"]) < 0)
                    {
                        throw new Exception("Week3 Capacity must be a non-null, non-negative integer.");
                    }

                    if (row["Week4 Capacity"] == null || !IsNumeric(row["Week4 Capacity"]) || Convert.ToInt64(row["Week4 Capacity"]) < 0)
                    {
                        throw new Exception("Week4 Capacity must be a non-null, non-negative integer.");
                    }

                    bool hasWeek5Capacity = row.Table.Columns.Contains("Week5 Capacity") && row["Week5 Capacity"] != DBNull.Value;
                    if (hasWeek5Capacity)
                    {
                        var week5CapacityValue = row["Week5 Capacity"]?.ToString().Trim();
                        if (string.IsNullOrEmpty(week5CapacityValue))
                        {
                            return;
                        }

                        if (!IsNumeric(week5CapacityValue) || Convert.ToInt64(week5CapacityValue) < 0)
                        {
                            throw new Exception("Week5 Capacity must be a valid, non-negative integer if provided.");
                        }
                    }

                    var week1Capacity = Convert.ToInt64(row["Week1 Capacity"]);
                    var week2Capacity = Convert.ToInt64(row["Week2 Capacity"]);
                    var week3Capacity = Convert.ToInt64(row["Week3 Capacity"]);
                    var week4Capacity = Convert.ToInt64(row["Week4 Capacity"]);
                    var week5Capacity = hasWeek5Capacity ? Convert.ToInt64(row["Week5 Capacity"]) : 0;

                    long totalCapacity = week1Capacity + week2Capacity + week3Capacity + week4Capacity;
                    if (hasWeek5Capacity)
                    {
                        totalCapacity += week5Capacity;
                    }

                    long remainingCap = checkstudio.TotalCapacity - checkstudio.RemainingCapacity;
                    if (checkstudio.RemainingCapacity == 0)
                    {
                        if (checkstudio.TotalCapacity > totalCapacity)
                        {
                            throw new Exception($"Give more Capacity to studio {name}");
                        }
                    }
                    else if (remainingCap > totalCapacity)
                    {
                        throw new Exception($"Give more Capacity to studio {name}");
                    }
                }
            }

            foreach (DataRow row in dataTable.Rows)
            {
                var name = row["Studio Name"]?.ToString();
                var division = row["Division"]?.ToString();
                var divisionID = await connection.QueryFirstOrDefaultAsync<Division>($"select * from division where name='{division}'");
                var checkstudio = await connection.QueryFirstOrDefaultAsync<Studio>(
                    "SELECT * FROM studio WHERE name = @Name AND divisionId::text = @Division",
                    new { Name = name, Division = divisionID.Code });
                var week1Capacity = Convert.ToInt64(row["Week1 Capacity"]);
                var week2Capacity = Convert.ToInt64(row["Week2 Capacity"]);
                var week3Capacity = Convert.ToInt64(row["Week3 Capacity"]);
                var week4Capacity = Convert.ToInt64(row["Week4 Capacity"]);
                bool hasWeek5Capacity = row.Table.Columns.Contains("Week5 Capacity") && row["Week5 Capacity"] != DBNull.Value;
                var week5Capacity = hasWeek5Capacity ? Convert.ToInt64(row["Week5 Capacity"]) : 0;
                long totalCapacity = week1Capacity + week2Capacity + week3Capacity + week4Capacity;
                long remainingCapacity = 0;
                if (hasWeek5Capacity)
                {
                    totalCapacity += week5Capacity;
                }

                if (checkstudio.TotalCapacity == totalCapacity)
                {
                    remainingCapacity = checkstudio.RemainingCapacity;
                }
                else if (checkstudio.TotalCapacity < totalCapacity)
                {
                    remainingCapacity = checkstudio.RemainingCapacity + (totalCapacity - checkstudio.TotalCapacity);
                }
                else if (checkstudio.TotalCapacity > totalCapacity)
                {
                    long remains = checkstudio.TotalCapacity - checkstudio.RemainingCapacity;
                    remainingCapacity = checkstudio.RemainingCapacity - totalCapacity;
                }

                var query = new StringBuilder("UPDATE studio SET ");
                query.Append("Week1Capacity = @Week1Capacity, ");
                query.Append("Week2Capacity = @Week2Capacity, ");
                query.Append("Week3Capacity = @Week3Capacity, ");
                query.Append("Week4Capacity = @Week4Capacity, ");
                if (hasWeek5Capacity)
                {
                    query.Append("Week5Capacity = @Week5Capacity, ");
                }

                query.Append("RemainingCapacity = @RemainingCapacity, ");
                query.Append("TotalCapacity = @TotalCapacity ");
                query.Append("WHERE name = @Name AND divisionId::text = @Division");
                await connection.ExecuteAsync(query.ToString(),
                    new
                    {
                        Week1Capacity = week1Capacity,
                        Week2Capacity = week2Capacity,
                        Week3Capacity = week3Capacity,
                        Week4Capacity = week4Capacity,
                        Week5Capacity = hasWeek5Capacity ? week5Capacity : (long?)null,
                        TotalCapacity = totalCapacity,
                        RemainingCapacity = remainingCapacity,
                        Name = name,
                        Division = divisionID.Code
                    });
            }
        }

        public async Task DeleteStudio(IEnumerable<long> ids)
        {
            using var connection = this.GetConnection();
            foreach (var id in ids)
            {
                await connection.ExecuteAsync($"Delete from studio where id = {id}");
            }
        }

        public async Task CapacityCalculation()
        {
            using var connection = this.GetConnection();
            var studios = await connection.QueryAsync<(string City, int TotalCapacity, int RemainingCapacity)>(
    "SELECT city, totalcapacity, remainingcapacity FROM studio");

            foreach (var studio in studios)
            {
                int assignedValue = studio.TotalCapacity - studio.RemainingCapacity;
                await connection.ExecuteAsync($"Update studiocapacity set assigned = {assignedValue} where city = '{studio.City}' and city IS NOT NULL and studioid IS NOT NULL");
            }
        }

        public async Task<byte[]> DownloadStudio(bool? isActive)
        {
            using var connection = this.GetConnection();
            var studiodata = new List<StudioExcel>();
            await this.CapacityCalculation();

            if (isActive is not null)
            {
                studiodata = (await connection.QueryAsync<StudioExcel>($"SELECT Studio.name AS Name, Studio.code AS Code, division.name AS Division, Studio.city AS City, Studio.priority AS Priority, MAX(sc.assigned) AS Assigned, " +
                                                                        $"MAX(Studio.totalcapacity) AS TotalCapacity, MAX(Studio.remainingcapacity) AS RemainingCapacity FROM Studio " +
                                                                        $"JOIN division ON division.code = Studio.divisionId::text " +
                                                                        $"JOIN studiocapacity sc ON Studio.city = sc.city  where Studio.isactive = '{isActive}' " +
                                                                        $"GROUP BY Studio.name, Studio.code, division.name, Studio.city, Studio.priority " +
                                                                        $"ORDER BY Studio.name ")).ToList();
            }
            else
            {
                studiodata = (await connection.QueryAsync<StudioExcel>("SELECT Studio.name AS Name, Studio.code AS Code, division.name AS Division, Studio.city AS City, Studio.priority AS Priority, MAX(sc.assigned) AS Assigned, " +
                                                                        "MAX(Studio.totalcapacity) AS TotalCapacity, MAX(Studio.remainingcapacity) AS RemainingCapacity FROM Studio " +
                                                                        "JOIN division ON division.code = Studio.divisionId::text " +
                                                                        "JOIN studiocapacity sc ON Studio.city = sc.city " +
                                                                        "GROUP BY Studio.name, Studio.code, division.name, Studio.city, Studio.priority " +
                                                                        "ORDER BY Studio.name ")).ToList();
            }

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Studio");

            if (studiodata.Any())
            {
                var headers = new[] { "Studio Name", "Studio Code", "Division", "City", "Priority", "TotalCapacity", "Assigned", "RemainingCapacity" };
                var headerBackgroundColor = System.Drawing.ColorTranslator.FromHtml("#5787C5");
                var headerTextColor = System.Drawing.Color.White;
                var borderColor = System.Drawing.ColorTranslator.FromHtml("#5787C5");
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = worksheet.Cells[1, i + 1];
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.Color.SetColor(headerTextColor);
                    cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
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
                foreach (var record in studiodata)
                {
                    worksheet.Cells[row, 1].Value = record.Name;
                    worksheet.Cells[row, 2].Value = record.Code;
                    worksheet.Cells[row, 3].Value = record.Division;
                    worksheet.Cells[row, 4].Value = record.City;
                    worksheet.Cells[row, 5].Value = record.Priority;
                    worksheet.Cells[row, 6].Value = record.TotalCapacity;
                    worksheet.Cells[row, 7].Value = record.Assigned;
                    worksheet.Cells[row, 8].Value = record.RemainingCapacity;
                    worksheet.Cells[row, 1, row, 8].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
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

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            }

            using var memoryStream = new MemoryStream();
            package.SaveAs(memoryStream);
            return memoryStream.ToArray();
        }

        public async Task<SearchResult<Studio>> SearchStudios(string searchKey, int pageNumber, int pageSize, bool isActive)
        {
            return await this.Search(searchKey, pageNumber, pageSize, isActive: isActive);
        }

        public async Task<List<StudioModel>> GetStudiosDetails(string? isActive)
        {
            using var connection = this.GetConnection();

            string query = @"   SELECT 
                                s.name AS StudioName, 
                                s.code,
                                s.id, 
                                s.city, 
                                MAX(s.totalcapacity) AS TotalCapacity,
                                MAX(sc.assigned) AS Assigned,
                                MAX(s.remainingcapacity) AS RemainingCapacity,
                                s.week1capacity,
                                s.week2capacity,
                                s.week3capacity,
                                s.week4capacity,
                                s.week5capacity,
                                s.totalcapacity,
                                s.priority,
                                s.isActive,
                                s.effectivefromdate,
                                d.name AS DivisionName,
                                s.divisionid 
                            FROM 
                                studio s 
                            JOIN 
                                studiocapacity sc ON s.city = sc.city 
                            JOIN 
                                division d ON s.divisionid = d.id";

            if (isActive == "1")
            {
                query += " WHERE s.isActive = false";
            }
            else if (isActive == "0")
            {
                query += " WHERE s.isActive = true";
            }

            query += " GROUP BY s.name, s.code, s.id, s.city, s.week1capacity, s.week2capacity, s.week3capacity, s.week4capacity, s.week5capacity, s.totalcapacity, s.priority, s.isActive, s.effectivefromdate, d.name, s.divisionid";
            query += " ORDER BY s.name ASC";

            var studios = await connection.QueryAsync<StudioModel>(query, new { IsActive = isActive });

            return studios.ToList();
        }

        public async Task<List<Studio>> GetStudio()
        {
            using var connection = this.GetConnection();
            var studios = await connection.QueryAsync<Studio>("SELECT * FROM studio");
            return studios.ToList();
        }

        public async Task DeactivateStudios(long[] ids)
        {
            await this.Deactivate(ids);
        }

        public async Task ActivateStudios(long[] ids)
        {
            using var connection = this.GetConnection();
            await this.Activate(ids);

            var studioUpdateQuery = @"update studio set effectivefromdate = 
            case
                when effectivefromdate is null or effectivefromdate > @effectivedate then @effectivedate
                else effectivefromdate
            end
            where id = @studioId";

            foreach (var id in ids)
            {
                var existingDate = await connection.ExecuteScalarAsync<DateTime?>("select effectivefromdate from studio where id = @studioId", new { studioId = id });
                if (existingDate != null && existingDate <= DateTime.Now.Date)
                {
                    continue;
                }

                await connection.ExecuteAsync(studioUpdateQuery, new { effectivedate = DateTime.Now, studioId = id });
            }
        }

        public async Task RemoveStudios(long[] ids)
        {
            var action = this.GetAction<Studio>();
            await action.Remove(ids);
        }

        public async Task UpdateIsactive()
        {
            try
            {
                using var connection = this.GetConnection();
                var currentDate = DateTime.Now.ToString("yyyy/MM/dd");
                var query = await connection.ExecuteAsync($"UPDATE studio SET isactive = true WHERE to_char(effectivefromdate::date::timestamp with time zone, 'yyyy/MM/dd') = '{currentDate}'");
            }
            catch (Exception ex)
            {
                throw new Exception("Error Updating Isactive", ex);
            }
        }

        private bool IsNumeric(object value)
        {
            return long.TryParse(value?.ToString(), out _);
        }
    }
}
