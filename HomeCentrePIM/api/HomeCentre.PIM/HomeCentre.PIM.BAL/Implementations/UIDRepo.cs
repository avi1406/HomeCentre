using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AgileObjects.AgileMapper;
using Dapper;
using FluentEmail.Core;
using Hangfire;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Implementations;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.BAL.Model.Email;
using HomeCentre.PIM.BAL.Model.Uid;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.Master;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph.SecurityNamespace;
using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using RazorEngine;

namespace HomeCentre.PIM.WorkFlow.Repository
{
    public static class DataTableExtensions
    {
        public static DataTable SetColumnsOrder(this DataTable table, params string[] columnNames)
        {
            int columnIndex = 0;
            foreach (var columnName in columnNames)
            {
                table.Columns[columnName].SetOrdinal(columnIndex);
                columnIndex++;
            }

            return table;
        }
    }

    public class UIDRepo : PgEntityAction<UidInitiation>, IUIDManager
    {
        private readonly IWorkFlowManager _taskStepInstance;
        private readonly IUserContext _userContext;
        private readonly IDbCache _dbCache;
        private readonly IMailEngine _mailEngine;
        private Dictionary<int, string> _userAction = new Dictionary<int, string>();

        public UIDRepo(IDbConnectionFactory connectionFactory, IUserContext userContext, IDbCache dbCache, IWorkFlowManager taskStepInstance, IDataProcess dataProcess, IConfiguration configuration, IMailEngine mailEngine)
            : base(userContext, connectionFactory, configuration)
        {
            _taskStepInstance = taskStepInstance;
            _userContext = userContext;
            _dbCache = dbCache;
            _mailEngine = mailEngine;
        }

        public async Task<bool> IsAlreadyExist(string planogramOrFamilyName)
        {
            using var connection = this.GetConnection();
            var result = await connection.QueryAsync("select * from uidinitiation where lower(planogram) = @name or lower(familyname) = @name", new { name = planogramOrFamilyName.ToLower().Trim() });
            var resultCount = result.Count();
            if (resultCount < 1)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public async Task<UidInitiation> AddPlanogramOrFamilyName(UidInitiation uidInitiation)
        {
            uidInitiation.Status = "Pending";
            uidInitiation.FirstLevelAction = "Pending";
            return await AddOrUpdate(uidInitiation);
        }

        public async Task<T> GetPreviewInfo<T>(long workFlowRunId, string tableName)
        {
            using (var connection = this.GetConnection())
            {
                return (await connection.QueryAsync<T>($"select * from {tableName} where id = (select recordid from workflowrun where id = @id)", new { id = workFlowRunId })).FirstOrDefault();
            }
        }

        public async Task<List<PlanagromRecortList>> GetPlanogramListAsync()
        {
            try
            {
                using var connection = this.GetConnection();

                // var uidList = (await connection.QueryAsync<PlanagromRecortList>("select u.id as Planocode, u.firstlevelapproveddate as firstLevelActionDate, u.description,u.firstlevelcomments, COALESCE(u.firstlevelaction,'Pending') AS firstlevelaction, COALESCE(u.secondlevelaction,'Pending') AS secondlevelaction, u.secondlevelapproveddate as secondLevelActionDate, u.secondlevelcomments, u.id, u.dateCreated, t.action as test, t.id as TaskStepInstanceId, w.recordid as RecordId, l1.name as FirstLevelApprover, l2.name as SecondLevelApprover,di.name as Division, c1.name as CreatedBy,  concat(u.planogram, ' ', u.familyname) as planogramOrFamilyName,  CASE WHEN u.familyname = '' THEN 'Planogram' ELSE 'FamilyName' END as Type from uidinitiation u JOIN workflowrun w ON u.id = w.recordid  JOIN taskstepinstance t ON t.workflowrunid = w.id LEFT JOIN appuser l1 ON l1.id = u.firstlevelapprovedby LEFT JOIN appuser c1 ON c1.id = u.createdby    LEFT JOIN appuser l2 ON l2.id = u.secondlevelapprovedby  LEFT JOIN division di  ON di.id = u.divisionid  order by Id desc"))?.ToList();
                var uidList = connection.Query<PlanagromRecortList>("select * from vw_planogramrecordlist");
                return uidList.DistinctBy(a => a.Id).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error on GetPlanogramListAsync : {ex.Message}");
            }
        }

        public async Task<List<PlanagromRecortList>> GetPlanogramApprovalListAsync(string roleId, string divisionId, string roleLevel)
        {
            divisionId = RemoveTrailingZero(divisionId);
            try
            {
                string[] roleIds = roleId.Split(',');
                using var connection = this.GetConnection();
                if (roleLevel == "Level 1" && roleIds.Contains("2"))
                {
                    var uid = (await connection.QueryAsync<PlanagromRecortList>($"select u.*,u.planocode as PlanoOrFamilyCode,concat(u.planogram, ' ', u.familyname) AS PlanoOrFamilyName,to_char(u.datecreated::date, 'dd/MM/yyyy') AS PlanoOrFamilyCreatedDate,u.firstlevelcomments as FirstLevelComments,u.secondlevelcomments as SecondLevelComments, to_char(u.firstlevelapproveddate::date, 'dd/MM/yyyy') AS L1ActionDate, t.id as TaskStepInstanceId, t.name as TaskStepInstanceName, w.recordid as RecordId, l1.name as ActionedbyL1,l2.name as ActionedbyL2, di.name as Division, c1.name as PlanoOrFamilyCreatedBy , concat(u.planogram, ' ', u.familyname) as PlanoOrFamilyName, CASE WHEN u.familyname = '' THEN 'Planogram' ELSE 'FamilyName' END as RequestType from uidinitiation u JOIN workflowrun w ON u.id = w.recordid JOIN taskstepinstance t ON t.workflowrunid = w.id LEFT JOIN appuser l1 ON l1.id = u.firstlevelapprovedby  LEFT JOIN appuser c1 ON c1.id = u.createdby  LEFT JOIN appuser l2 ON l2.id = u.secondlevelapprovedby  LEFT JOIN division di  ON di.id = u.divisionid where u.divisionid in ({divisionId}) and u.firstlevelaction = 'Approved' and u.status = 'Pending' order by u.Id desc")).ToList();
                    return uid;
                }
                else
                {
                    var uidList =
                    (await connection.QueryAsync<PlanagromRecortList>($"select u.*,u.planocode as PlanoOrFamilyCode,concat(u.planogram, ' ', u.familyname) AS PlanoOrFamilyName,to_char(u.datecreated::date, 'dd/MM/yyyy') AS PlanoOrFamilyCreatedDate,u.firstlevelcomments as FirstLevelComments,u.secondlevelcomments as SecondLevelComments, to_char(u.firstlevelapproveddate::date, 'dd/MM/yyyy') AS L1ActionDate, t.id as TaskStepInstanceId, t.name as TaskStepInstanceName, w.recordid as RecordId, l1.name as ActionedbyL1,l2.name as ActionedbyL2, di.name as Division, c1.name as PlanoOrFamilyCreatedBy , concat(u.planogram, ' ', u.familyname) as PlanoOrFamilyName, CASE WHEN u.familyname = '' THEN 'Planogram' ELSE 'FamilyName' END as RequestType from uidinitiation u JOIN workflowrun w ON u.id = w.recordid JOIN taskstepinstance t ON t.workflowrunid = w.id LEFT JOIN appuser l1 ON l1.id = u.firstlevelapprovedby  LEFT JOIN appuser c1 ON c1.id = u.createdby  LEFT JOIN appuser l2 ON l2.id = u.secondlevelapprovedby  LEFT JOIN division di  ON di.id = u.divisionid where u.divisionid in ({divisionId}) and u.firstlevelaction = 'Pending' and u.status = 'Pending' order by Id desc")).ToList();
                    return uidList;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error on GetPlanogramApprovalListAsync : {ex.Message}");
            }
        }

        public async Task<List<UidInitiation>> SearchUID(string searchkey, bool isPlanogram)
        {
            using var connection = this.GetConnection();
            return (List<UidInitiation>)await connection.QueryAsync<UidInitiation>($"select planogram, familyname from uidinitiation where LOWER(planogram) LIKE LOWER('{searchkey}%') or LOWER(familyname) Like Lower('{searchkey}%') order by planogram,familyname ASC");
        }

        public async Task<Stream> GetPlanogramApprovalListAsync(bool isGetAll, DateTime? startDate, DateTime? endDate)
        {
            using var connection = this.GetConnection();
            var query = string.Empty;
            if (!isGetAll)
            {
                var startDates = startDate ?? DateTime.MinValue;
                var endDates = endDate ?? DateTime.UtcNow;
                var formatedStartDate = startDates.ToString("yyyy/MM/dd hh:mm:ss");
                var formatedEndDate = endDates.ToString("yyyy/MM/dd hh:mm:ss");
                query = $"select * from vw_planogramrecordlist where(datecreated between '{formatedStartDate}' and '{formatedEndDate}') order by Id desc";
            }
            else
            {
                query = "select * from vw_planogramrecordlist ORDER by Id desc";
            }

            string[] cols = new string[] { "id", "planogramOrFamilyName", "Planocode", "Division", "Type", "description", "CreatedBy", "dateCreated", "FirstLevelApprover", "firstlevelaction", "firstLevelActionDate", "firstlevelcomments", "SecondLevelApprover", "secondlevelaction", "secondLevelActionDate", "secondlevelcomments" };
            var result = (await connection.QueryAsync<PlanagromRecortList>(query)).DistinctBy(a => a.Id).ToList();
            var responseDataTable = this.ToDataTable(result);

            if (responseDataTable.Columns.Contains("TaskStepInstanceId"))
            {
                responseDataTable.Columns.Remove("TaskStepInstanceId");
            }

            if (responseDataTable.Columns.Contains("RecordId"))
            {
                responseDataTable.Columns.Remove("RecordId");
            }

            return new FileExportHelper().ExportToExcel(responseDataTable.SetColumnsOrder(cols));
        }

        public async Task<List<UidInitiation>> GetPlanoRecord()
        {
            using (var connection = this.GetConnection())
            {
                var record = await connection.QueryAsync<UidInitiation>("select planocode as PlanoCode,planogram as Planogram,familyname as FamilyName from uidinitiation where planocode is not null");
                return record.ToList();
            }
        }

        public async Task<List<PlanagromRecortList>> UidSearchByCriteria(UidSearchModel uidSearchModel)
        {
            using var connection = this.GetConnection();

            // var plano = await connection.QueryAsync<RmsStaging>("select r.uda_value, r.datemodified, r.uda_value_desc from uidinitiation u JOIN rmsstaging r ON LOWER(CONCAT(u.planogram, u.familyname)) = LOWER(r.uda_value_desc)");
            // foreach (var item in plano)
            // {
            //    var updateplano = $"Update uidinitiation u set planocode = '{item.UDA_VALUE}',rmsintegrationdate = '{item.DateModified}' where LOWER(CONCAT(u.planogram, u.familyname)) = LOWER('{item.UDA_VALUE_DESC}') and u.status = 'Approved'";
            //    await connection.ExecuteAsync(updateplano);
            // }
            var query = string.Empty;
            if (uidSearchModel.DepartmentId != null || uidSearchModel.FromDate != null || uidSearchModel.ToDate != null)
            {
                var startDates = uidSearchModel.FromDate ?? DateTime.MinValue;
                var endDates = uidSearchModel.ToDate ?? DateTime.MaxValue;
                var formatedStartDate = startDates.ToString("dd/MM/yyyy");
                var formatedEndDate = endDates.ToString("dd/MM/yyyy");

                if (uidSearchModel.DepartmentId == null || uidSearchModel.DepartmentId == string.Empty)
                {
                    // query = $"select * from vw_planogramrecordlist where(datecreated between '{formatedStartDate}' and '{formatedEndDate}') order by Id desc";
                    query = $@"SELECT * FROM vw_planogramrecordlist WHERE to_date(datecreated, 'dd/MM/yyyy') BETWEEN to_date('{formatedStartDate}', 'dd/MM/yyyy') AND to_date('{formatedEndDate}', 'dd/MM/yyyy') ORDER BY Id DESC";
                }
                else
                {
                    // query = $"select * from vw_planogramrecordlist where(datecreated between '{formatedStartDate}' and '{formatedEndDate}') and departmentid = {uidSearchModel.DepartmentId} order by Id desc";
                    string[] departmentIds = uidSearchModel.DepartmentId.Split(',');
                    int[] departmentIdArray = Array.ConvertAll(departmentIds, int.Parse);
                    query = $"SELECT * FROM vw_planogramrecordlist " +
                                   $"WHERE to_date(datecreated, 'dd/MM/yyyy') BETWEEN to_date('{formatedStartDate}', 'dd/MM/yyyy') AND to_date('{formatedEndDate}', 'dd/MM/yyyy') " +
                                   $"AND departmentid IN ({string.Join(",", departmentIdArray)}) " +
                                   $"ORDER BY Id DESC";
                }
            }
            else
            {
                var valueFromConfig = await _dbCache.FindAppSettings("PlanogramRecordList");
                query = $"select * from vw_planogramrecordlist ORDER by Id desc";
                //// limit {int.Parse(valueFromConfig.Value)}
            }

            var uidList = connection.Query<PlanagromRecortList>(query);
            return uidList.ToList();
        }

        public async Task<byte[]> UIDExcel(UidSearchModel uidSearchModel)
        {
            using var connection = this.GetConnection();
            var result = new List<PlanagromRecortList>();

            if (uidSearchModel.DepartmentId == null && uidSearchModel.FromDate == null && uidSearchModel.ToDate == null)
            {
                result = (await connection.QueryAsync<PlanagromRecortList>("select * from vw_planogramrecordlist ORDER by Id desc")).ToList();
            }
            else
            {
                result = await UidSearchByCriteria(uidSearchModel);
            }

            result.ForEach(item => item.PlanoOrFamilyName = item.PlanoOrFamilyName?.Trim());

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Planogram Records");

            if (result.Any())
            {
                var headers = new[] { "PlanoOrFamilyName", "PlanoOrFamilyCode", "Division", "RequestType", "Description", "PlanoOrFamilyNameCreationStatusL1", "PlanoOrFamilyNameCreationStatusL2", "PlanoOrFamilyCodeStatus", "ActionedbyL1", "L1ActionDate", "ActionedbyL2", "L2ActionDate", "PlanoOrFamilyCreatedBy", "PlanoOrFamilyCreatedDate", "PlanoOrFamilyCodeUpdatedDate" };
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
                    worksheet.Cells[row, 1].Value = record.PlanoOrFamilyName;
                    worksheet.Cells[row, 2].Value = record.PlanoOrFamilyCode;
                    worksheet.Cells[row, 3].Value = record.Division;
                    worksheet.Cells[row, 4].Value = record.RequestType;
                    worksheet.Cells[row, 5].Value = record.Description;
                    worksheet.Cells[row, 6].Value = record.PlanoOrFamilyNameCreationStatusL1;
                    worksheet.Cells[row, 7].Value = record.PlanoOrFamilyNameCreationStatusL2;
                    worksheet.Cells[row, 8].Value = record.PlanoOrFamilyCodeStatus;
                    worksheet.Cells[row, 9].Value = record.ActionedbyL1;
                    worksheet.Cells[row, 10].Value = record.L1ActionDate;
                    worksheet.Cells[row, 11].Value = record.ActionedbyL2;
                    worksheet.Cells[row, 12].Value = record.L2ActionDate;
                    worksheet.Cells[row, 13].Value = record.PlanoOrFamilyCreatedBy;
                    worksheet.Cells[row, 14].Value = record.PlanoOrFamilyCreatedDate;
                    worksheet.Cells[row, 15].Value = record.PlanoOrFamilyCodeUpdatedDate;

                    worksheet.Cells[row, 1, row, 15].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;

                    for (int col = 1; col <= 15; col++)
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

        public async Task<EmailContentModel> EmailContent(long userid)
        {
            try
            {
                using var connection = this.GetConnection();
                string str = string.Empty;
                var emailContentModel = new EmailContentModel();
                var content = (await connection.QueryAsync<EmailContentModel>($"select emailcontent from sentemail as e join actor as a on a.taskstepinstanceid = e.taskstepinstanceid where a.appuserid = {userid} and status = 1 order by e.datecreated desc limit 25")).ToList();
                foreach (var email in content?.Where(a => !string.IsNullOrWhiteSpace(a.Emailcontent)))
                {
                    str = str + email.Emailcontent + "<br/><br/>";
                }

                emailContentModel.CreatedDate = DateTime.Now.Date;
                emailContentModel.Emailcontent = str;
                emailContentModel.UserId = userid;
                return emailContentModel;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<string> SendEmail()
        {
            using var connection = this.GetConnection();
            var users = await connection.QueryAsync<EmailContents>("select * from vw_emailcontents");
            foreach (var user in users)
            {
                string plainText = Regex.Replace(user.EmailContent, "<.*?>", string.Empty);
                plainText = Regex.Replace(plainText, "&[a-z]+;", string.Empty);
                plainText = Regex.Replace(plainText, "&nbsp", " ");
                await _mailEngine.SendEmail("DigestEmailContent", new EmailContents
                {
                    EmailContent = plainText
                }, "Welcome to HC.PIM", null, new[] { user.Email });
                await connection.ExecuteAsync($"Update sentemail set status = 2 where taskstepinstanceid in (select taskstepinstanceid from actor where appuserid = {user.AppuserId})");
            }

            return "Mails Has Sent";
        }

        public async Task<IEnumerable<string>> GetApprovedPlanoOrFamilyNames(long divisionId)
        {
            using var connection = this.GetConnection();
            var names = new List<string>();
            var planoOrFamilyNames = await connection.QueryAsync<(string, string)>(
                "select planogram, familyname from uidinitiation where status = 'Approved' and divisionid = @divisionId;",
                new
                {
                    divisionId
                });
            foreach (var planoOrFamilyName in planoOrFamilyNames)
            {
                names.Add(planoOrFamilyName.Item1);
                names.Add(planoOrFamilyName.Item2);
            }

            return names.Distinct();
        }

        public async Task<IEnumerable<long>> GetDeptCode()
        {
            using var connection = this.GetConnection();
            var codes = new List<long>();
            var deptCode = await connection.QueryAsync<ProductDepartment>(
                "select * from productdepartment");
            foreach (var dept in deptCode)
            {
                codes.Add(dept.Departmentcode);
            }

            return codes;
        }

        public async Task<IEnumerable<string>> GetCode()
        {
            using var connection = this.GetConnection();
            var subcodes = new List<string>();
            var subCode = await connection.QueryAsync<HierarchicalMaster>(
                "select * from hierarchicalmaster");
            foreach (var sub in subCode)
            {
                subcodes.Add(sub.ClassCode);
                subcodes.Add(sub.SubclassCode);
                subcodes.Add(sub.GroupCode);
            }

            return subcodes;
        }

        public async Task<IEnumerable<string>> Purchasetype()
        {
            using var connection = this.GetConnection();
            var purchase = new List<string>();
            var purchasetype = await connection.QueryAsync<Purchasetype>(
            "select * from Purchasetype");
            foreach (var sub in purchasetype)
            {
                purchase.Add(sub.Name.ToLower());
            }

            return purchase;
        }

        public async Task<IEnumerable<string>> Pricegrouping()
        {
            using var connection = this.GetConnection();
            var product = new List<string>();
            var purchasetype = await connection.QueryAsync<PriceGrouping>(
            "select * from PriceGrouping");
            foreach (var sub in purchasetype)
            {
                product.Add(sub.Name.ToLower());
            }

            return product;
        }

        public async Task<IEnumerable<string>> Productpurpose()
        {
            using var connection = this.GetConnection();
            var productpurpose = new List<string>();
            var purpose = await connection.QueryAsync<ProductPurpose>(
            "select * from ProductPurpose");
            foreach (var sub in purpose)
            {
                productpurpose.Add(sub.Name.ToLower());
            }

            return productpurpose;
        }

        public async Task<IEnumerable<long>> Planocode()
        {
            using var connection = this.GetConnection();
            var planocode = new List<long>();
            var plano = await connection.QueryAsync<RmsStaging>(
            "select * from rmsstaging");
            foreach (var code in plano)
            {
                planocode.Add(code.UDA_VALUE);
            }

            return planocode;
        }

        public async Task<IEnumerable<string>> Productpicture()
        {
            using var connection = this.GetConnection();
            var productpicture = new List<string>();
            var picture = await connection.QueryAsync<UploadPicture>(
            "select * from UploadPicture ");
            foreach (var pic in picture)
            {
                productpicture.Add(pic.Name.ToLower());
                productpicture.Add(pic.Id.ToString());
            }

            return productpicture;
        }

        public async Task UpdateRejectedName(string[] name)
        {
            using var connection = this.GetConnection();

            foreach (var names in name)
            {
                var queryResult = await connection.QueryFirstOrDefaultAsync<UidInitiation>($"SELECT status,firstlevelaction,secondlevelaction FROM uidinitiation WHERE planogram = '{names}' OR familyname = '{names}'");

                if (queryResult.Status == "Rejected" || queryResult.FirstLevelAction == "Rejected" || queryResult.SecondLevelAction == "Rejected")
                {
                    await connection.ExecuteAsync($"UPDATE uidinitiation SET status = 'Pending',firstlevelaction = 'Pending',secondlevelaction = 'Pending',firstlevelapprovedby = null,firstlevelapproveddate = null,secondlevelapprovedby = null,secondlevelapproveddate = null  WHERE planogram = '{names}' OR familyname = '{names}'");
                    await connection.ExecuteAsync($@"
                    UPDATE taskstepinstance
                    SET action = 
                        CASE 
                            WHEN name = 'UID First Level Approvel' THEN 'InProgress'
                            WHEN name = 'ApprovalL2Mail' THEN 'YetToStart'
                            WHEN name = 'ApprovalL1Mail' THEN 'YetToStart'
                            WHEN name = 'Initiation Mail' THEN 'YetToStart'
                            WHEN name = 'UID second level Approval' THEN 'YetToStart'
                            ELSE action
                        END
                    WHERE title = '{names}'
                ");
                }
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

        private string RemoveTrailingZero(string divisionId)
        {
            if (divisionId.EndsWith(",0"))
            {
                return divisionId.Substring(0, divisionId.Length - 2);
            }

            return divisionId;
        }

        // public async Task<TasklevelSequence> AddUserAction(TasklevelSequence seq)
        // {
        //    return await AddOrUpdate(TasklevelSequence);
        // }
    }
}