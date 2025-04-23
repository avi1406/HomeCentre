using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Hangfire;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Implementations.LandmarkProcess;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.BAL.Model.Uid;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.CommonWeb;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.Master;
using HomeCentre.PIM.WorkFlow.Repository;
using Microsoft.AspNetCore.Mvc;

namespace HomeCentre.PIM.Api.Controllers.Api
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class WorkFlowController : ControllerBase
    {
        private readonly IDataProcess _dataprocess;
        private readonly IWorkFlowManager _taskStepInstance;
        private readonly IWorkFlowManager1 _taskStepInstance1;
        private readonly IUIDManager _uidrepo;
        private readonly IDbCache _dbcache;
        private readonly IUserContext _userContext;

        public WorkFlowController(
            IWorkFlowManager taskStepInstance,
            IWorkFlowManager1 taskStepInstance1,
            IDataProcess dataprocess,
            IUIDManager uIDRepo,
            IUserContext userContext,
            IDbCache dbcache)
        {
            _taskStepInstance = taskStepInstance;
            _taskStepInstance1 = taskStepInstance1;
            _dataprocess = dataprocess;
            _uidrepo = uIDRepo;
            _userContext = userContext;
            _dbcache = dbcache;
        }

        [HttpPost]
        public async Task AddorUpdateTaskStepInstance([FromBody] TaskStepInstance taskStepInstance)
        {
            await _taskStepInstance.AddorUpdateTaskStepInstance(taskStepInstance);
        }

        [HttpGet]
        public async Task<AppUser> GetUserInfoByWorkFlowRunId(int workFlowRunId)
        {
            return await _uidrepo.GetPreviewInfo<AppUser>(workFlowRunId, "appuser");
        }

        [HttpGet]
        public async Task<UidInitiation> GetPlanogramOrFamilyNameByWorkFlowId(int workFlowRunId)
        {
            return await _uidrepo.GetPreviewInfo<UidInitiation>(workFlowRunId, "uidinitiation");
        }

        [HttpGet]
        public async Task<bool> GetIfAlreadyExistorNot(string planogramOrFamilyName)
        {
            var result = await _uidrepo.IsAlreadyExist(planogramOrFamilyName);
            return result;
        }

        [HttpPost]
        public async Task<UidInitiation> AddPlanogramOrFamilyName(UidInitiation uidDomainModel)
        {
            var valueFromConfig = await _dataprocess.FindAppSettings("UIDTaskStepId");
            var uidInitiation = await _uidrepo.AddPlanogramOrFamilyName(uidDomainModel);

            // var isHangFire = await _dbcache.FindAppSettings("IsHangFire");
            // if (isHangFire.Value.ToLower() == "true")
            // {
            //    var jobid = BackgroundJob.Enqueue(() => _taskStepInstance.CreateTaskStepInstance(int.Parse(valueFromConfig), uidInitiation.Id, true, 0, uidInitiation.UpdatedBy));
            //    return uidDomainModel;
            // }

            // await _taskStepInstance.CreateTaskStepInstance(int.Parse(valueFromConfig), uidInitiation.Id, true, 0, null);
            return uidDomainModel;
        }

        [HttpGet]
        public async Task<List<ExcelModel>> GetDataTableAsync(string messageName)
        {
            return await _dataprocess.ExcelModel(messageName);
        }

        [HttpPut]
        public async Task UpdateWorkflowstatusBatch(int[] ids, string action, string userComments)
        {
            foreach (var id in ids)
            {
                var table = await _taskStepInstance.GetllTaskStepInstance(id);
                var tablename = table.ToString();
                if (tablename.ToLower() == "appuser")
                {
                    if (action.ToLower() == "approved")
                    {
                        await _dataprocess.ApproveOrReject(id);
                    }
                }

                await _taskStepInstance.UpdateAction(action, id, userComments);
            }
        }

        [HttpGet]
        public async Task<List<TaskStepInstance>> GetTask(string action)
        {
            return await _taskStepInstance.GetTaskStepInstance(action);
        }

        [HttpGet]
        public async Task<List<PlanagromRecortList>> GetPlanogramRecordList()
        {
            return await _uidrepo.GetPlanogramListAsync();
        }

        [HttpGet]
        public async Task<List<PlanagromRecortList>> GetApprovedorRejectedList()
        {
            var roleid = await _userContext.GetRoleId();
            var divisionId = await _userContext.GetDepartmentIds();
            string role = string.Join(",", roleid);
            var roleLevel = await _userContext.GetRoleLevel();
            return await _uidrepo.GetPlanogramApprovalListAsync(role, divisionId, roleLevel);
        }

        [HttpPost]
        public async Task<WorkflowRun> AddorUpdateWorkflowRun([FromBody] WorkflowRun workflowRun)
        {
            await _taskStepInstance1.AddOrUpdateWorkflowRun(workflowRun, null);
            return workflowRun;
        }

        [HttpPost]
        public async Task<List<TasklevelSequence>> UpdateUserAction([FromBody] List<TasklevelSequence> tasklevelSeqInputLst)
        {
            await _taskStepInstance1.UpdateUserAction(tasklevelSeqInputLst);
            return tasklevelSeqInputLst;
        }

        [HttpGet]
        public async Task<List<UidInitiation>> SearchUID(string searchkey, bool isPlanogram)
        {
            return await _uidrepo.SearchUID(searchkey, isPlanogram);
        }

        [HttpPost]
        public async Task<List<PlanagromRecortList>> UidSearchByCriteria(UidSearchModel uidSearchModel)
        {
            return await _uidrepo.UidSearchByCriteria(uidSearchModel);
        }

        [HttpGet]
        public async Task<FileResult> ExportExcel(bool isGetAll, DateTime? startDate, DateTime? endDate, string fileName)
        {
            var fileStream = await _uidrepo.GetPlanogramApprovalListAsync(isGetAll, startDate, endDate);
            return File(fileStream, new FileExportHelper().GetMimeType("*.xlsx"), $"{fileName}.xlsx");
        }

        [HttpPost]
        public async Task<IActionResult> UIDExportExcel(UidSearchModel uidSearchModel)
        {
            string format = "xlsx";
            var fileBytes = await _uidrepo.UIDExcel(uidSearchModel);
            return File(fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"UidExcel.{format}");
        }

        [HttpGet]
        public async Task<EmailContentModel> EmailContent()
        {
            var userid = await _userContext.GetUserId();
            return await _uidrepo.EmailContent(userid);
        }

        [HttpGet]
        public async Task<string> Sentemail()
        {
            return await _uidrepo.SendEmail();
        }

        [HttpGet]
        public async Task<List<UidInitiation>> GetPlanoRecord()
        {
            return await _uidrepo.GetPlanoRecord();
        }

        [HttpPut]

        public async Task UpdateRejectedName(string[] name)
        {
            await _uidrepo.UpdateRejectedName(name);
        }
    }
}