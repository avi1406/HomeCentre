using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.BAL.Model.Uid;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.DAL;

namespace HomeCentre.PIM.BAL.Contracts
{
    public interface IUIDManager
    {
        Task<bool> IsAlreadyExist(string planogramOrFamilyName);

        Task<UidInitiation> AddPlanogramOrFamilyName(UidInitiation uidInitiation);

        Task<T> GetPreviewInfo<T>(long workFlowRunId, string tableName);

        // Task<List<PlanagromRecortList>> GetPlanogramApprovalListAsync(bool isPlanogramList, long buyerId, long designerId, long superAdminId);
        Task<List<PlanagromRecortList>> GetPlanogramApprovalListAsync(string roleId, string divisionid, string roleLevel);

        Task<List<PlanagromRecortList>> GetPlanogramListAsync();

        // Task<Stream> GetPlanogramApprovalListAsync(bool isGetAll, DateTime? startDate, DateTime? endDate);
        Task<List<UidInitiation>> SearchUID(string searchkey, bool isPlanogram);

        Task<Stream> GetPlanogramApprovalListAsync(bool isGetAll, DateTime? startDate, DateTime? endDate);

        Task<List<UidInitiation>> GetPlanoRecord();

        Task<byte[]> UIDExcel(UidSearchModel uidSearchModel);

        Task<List<PlanagromRecortList>> UidSearchByCriteria(UidSearchModel uidSearchModel);

        Task<EmailContentModel> EmailContent(long userid);

        Task<string> SendEmail();

        Task<IEnumerable<string>> GetApprovedPlanoOrFamilyNames(long divisionId);

        Task<IEnumerable<long>> GetDeptCode();

        Task<IEnumerable<string>> GetCode();

        Task<IEnumerable<string>> Purchasetype();

        Task<IEnumerable<string>> Pricegrouping();

        Task<IEnumerable<string>> Productpurpose();

        Task<IEnumerable<string>> Productpicture();

        Task<IEnumerable<long>> Planocode();

        Task UpdateRejectedName(string[] name);
    }
}
