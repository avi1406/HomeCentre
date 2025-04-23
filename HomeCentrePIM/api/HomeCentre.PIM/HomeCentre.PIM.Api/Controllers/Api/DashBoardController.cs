using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Model.Dashboard;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.CommonWeb;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.Master;
using Microsoft.AspNetCore.Mvc;

namespace HomeCentre.PIM.Api.Controllers.Api
{
    [Route("/api/[controller]/[action]")]
    [MultiplePoliciesAuthorize(Constants.Policies.All)]
    public class DashBoardController : BaseApiController
    {
        private readonly IUserManager _userManager;

        public DashBoardController(IUserManager userManager)
        {
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<int> GetUserCount()
        {
            return await _userManager.GetUserCount();
        }

        [HttpGet]
        public async Task<IEnumerable<ReportCountsModel>> GetRecentLoginHistoryCountDateWise()
        {
            return await _userManager.GetRecentLoginHistoryCountDateWise();
        }

        [HttpGet]

        public async Task<List<PlanoCountModel>> GetPlanoCount(DateTime? fromDate, DateTime? todate, long[]? divisionId)
        {
            return await _userManager.GetPlanoCount(fromDate, todate, divisionId);
        }

        [HttpGet]

        public async Task<List<PlanoCountModel>> GetDivisionPlanoCount()
        {
            return await _userManager.GetDivisionPlanoCount();
        }

        [HttpPost]
        public async Task<FileResult> GetPlanoCountExcel([FromBody] GetPlanoModalDto inputData)
        {
            var fileStream = await _userManager.GetPlanoCountExcel(inputData);
            return File(fileStream, new FileExportHelper().GetMimeType("*.xlsx"), $"PlanoCountExcel.xlsx");
        }

        [HttpGet]
        public async Task<List<UidCountModel>> GetUidCount(DateTime? fromdate, DateTime? toDate, long[]? divisionId)
        {
            return await _userManager.GetUidCount(fromdate, toDate, divisionId);
        }

        [HttpGet]
        public async Task<List<PlanoUserCountModel>> GetPlanoUserCount(long divisionId, DateTime? fromDate, DateTime? toDate)
        {
            return await _userManager.GetPlanoUserCount(divisionId, fromDate, toDate);
        }

        [HttpGet]
        public async Task<List<PlanoUidUserCount>> GetPlanoUidUserCount(long divisionId, DateTime? fromDate, DateTime? toDate)
        {
            return await _userManager.GetPlanoUidUserCount(divisionId, fromDate, toDate);
        }

        [HttpGet]

        public async Task<List<TemplateUidCountModel>> GetTemplatesUidCount(string? division_code, string? dept_code, string? class_code, string? subclass_code, DateTime? fromDate, DateTime? toDate)
        {
            return await _userManager.GetTemplatesUidCount(division_code, dept_code, class_code, subclass_code, fromDate, toDate);
        }

        [HttpGet]

        public async Task<List<TemplateUidCountModel>> GetTemplatesUidDivisionCount()
        {
            return await _userManager.GetTemplatesUidDivisionCount();
        }

        [HttpPost]
        public async Task<FileResult> GetTemplatesUidCountExcel([FromBody] GetTemplatesUidCountExcel getTemplatesUidCountExcel)
        {
            var fileStream = await _userManager.GetTemplatesUidCountExcel(getTemplatesUidCountExcel);
            return File(fileStream, new FileExportHelper().GetMimeType("*.xlsx"), $"UidCountExcel.xlsx");
        }

        [HttpGet]

        public async Task<List<TemplateDeptCount>> GetTemplateDeptCount(long divisionId)
        {
            return await _userManager.GetDeptUidCount(divisionId);
        }

        [HttpGet]

        public async Task<List<TemplateDeptCount>> GetDeptAllCount(long divisionId, string? dept, string? deptclass, string subclass, DateTime? fromDate, DateTime? toDate)
        {
            return await _userManager.GetDeptAllCount(divisionId, dept, deptclass, subclass, fromDate, toDate);
        }

        [HttpGet]
        public async Task<List<TemplateDeptCount>> GetDeptBuyer1Count(long divisionId)
        {
            return await _userManager.GetDeptBuyer1Count(divisionId);
        }
    }
}