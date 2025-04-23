using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeCentre.PIM.DAL;

namespace HomeCentre.PIM.BAL.Contracts
{
    public interface IWorkFlowManager
    {
        Task<WorkflowRun> CreateWorkflowRun(long workflowDesignId, long recordId, long? updatedById);

        Task<AppUser> GetUserInfoByWorkFlowRunId(long workFlowRunId);

        Task AddorUpdateTaskStepInstance(TaskStepInstance taskStepInstance);

        Task<string> GetllTaskStepInstance(int id);

        Task CreateTaskStepInstance(long taskStepId, long recordId, bool isFirstStep, long previousStepID, long? updatedBy);

        Task UpdateAction(string action, long id, string userComments);

        Task<List<TaskStepInstance>> GetpendingRequest(int id);

        Task<List<TaskStepInstance>> GetTaskInstancebyStatus(long id, string name, string action);

        Task<List<TaskStepInstance>> GetTaskStepInstance(string action);
    }
}