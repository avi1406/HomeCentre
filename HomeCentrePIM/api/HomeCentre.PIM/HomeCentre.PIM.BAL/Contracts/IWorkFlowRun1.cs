using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using HomeCentre.PIM.DAL;

namespace HomeCentre.PIM.BAL.Contracts
{
    public interface IWorkFlowManager1
    {
        Task<WorkflowRun> AddOrUpdateWorkflowRun(WorkflowRun workflowRun, IDbConnection idbConn);

        Task<List<TasklevelSequence>> UpdateUserAction(List<TasklevelSequence> tasklevelSeqInputLst);
    }
}