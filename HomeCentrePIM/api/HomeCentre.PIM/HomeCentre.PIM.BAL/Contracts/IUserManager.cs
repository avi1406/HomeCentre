using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.BAL.Model.Account;
using HomeCentre.PIM.BAL.Model.Dashboard;
using HomeCentre.PIM.BAL.Model.Tenant;
using HomeCentre.PIM.BAL.Model.Uid;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.Master;
using Org.BouncyCastle.Asn1.Cmp;
using Renci.SshNet.Sftp;

namespace HomeCentre.PIM.BAL.Contracts
{
    public interface IUserManager
    {
        Task AddOrUpdateUser(HyperlinkEdit modelAdminUserModel, long? overrideUserId = null);

        Task<AppUser> Get(long userId);

        Task<AppUser> GetUserByEmail(string userEmail);

        Task<string> Authenticate(LoginViewModel model);

        Task<MeModel> GetCurrentUser();

        Task UpdateUser(UserListItemModel appuser);

        Task ActivateUser(long id);

        Task ActivateUserBatch(long[] ids);

        Task DeactivateUser(long id);

        Task DeactivateUserBatch(long[] ids);

        Task<SearchResult<UserListItemModel>> SearchUsers(bool isActive);

        Task<SearchResult<UserListItemModel>> SearchAllUsers(IsActiveFilter isActiveFilter);

        Task<SearchResult<Role>> SearchAllRoles(IsActiveFilter isActiveFilter);

        Task<SearchResult<ProductAttribute>> SearchAllAttributes(IsActiveFilter isActiveFilter);

        Task<IEnumerable<ProductAttribute>> SearchAttributes(IsActiveFilter isActiveFilter, string? divisionId);

        Task<IEnumerable<Template>> SearchTemplates(IsActiveFilter isActiveFilter);

        Task<SearchResult<Template>> SearchAllTemplates(IsActiveFilter isActiveFilter);

        Task<SearchResult<Marketplace>> SearchAllMarketPlace(IsActiveFilter isActiveFilter);

        Task<MeModel> Me(long userId);

        Task<int> GetUserCount();

        Task<IEnumerable<ReportCountsModel>> GetRecentLoginHistoryCountDateWise();

        Task<byte[]> UserExportExcel(bool? isActive);

        Task<byte[]> RoleExportExcel(bool? isActive);

        Task<List<AppUser>> GetAllActiveUsers();

        Task<List<AppUser>> GetAllInactiveUsers();

        // Task<string> AddLabel(LabelModel model);
        Task AddRole(Role model);

        Task AddUserRole(AppUserRoleModel model);

        Task AddUserDepartment(AppUserRoleModel appUserRoleModel);

        Task<List<UserDepartment>> GetDepartmentDetails(string userId);

        Task DeleteDepartment(AppUserRole appUserRole);

        Task LinkUsersToMarketPlaces(IEnumerable<AppuserMarketplace> appuserMarketplaces);

        Task UnlinkUserFromMarketPlaces(long[] ids);

        Task<IEnumerable<Marketplace>> GetMarketplacesOfUser(long userId);

        Task<List<TemplateMapping>> GetTemplateMappings(long id);

        Task AddStudioToUser(AppUserStudioModal appUserStudioModel);

        Task<List<GetUserStudioModal>> GetUserStudioDetails(long userId);

        Task DeleteStudioFromUser(AppUserStudioDeleteModal appUserStudioDeleteModal);

        Task<IEnumerable<string>> ListFilesFromSftp();

        Task<List<PlanoCountModel>> GetPlanoCount(DateTime? fromDate, DateTime? todate, long[]? divisionId);

        Task<List<PlanoCountModel>> GetDivisionPlanoCount();

        Task<byte[]> GetPlanoCountExcel(GetPlanoModalDto inputData);

        Task<List<UidCountModel>> GetUidCount(DateTime? fromDate, DateTime? toDate, long[]? divisionId);

        Task<List<PlanoUserCountModel>> GetPlanoUserCount(long divisionId, DateTime? fromDate, DateTime? toDate);

        Task<List<PlanoUidUserCount>> GetPlanoUidUserCount(long divisionId, DateTime? fromDate, DateTime? toDate);

        Task<List<TemplateUidCountModel>> GetTemplatesUidCount(string? division_code, string? dept_code, string? class_code, string? subclass_code, DateTime? fromDate, DateTime? toDate);

        Task<byte[]> GetTemplatesUidCountExcel(GetTemplatesUidCountExcel getTemplatesUidCountExcel);

        Task<List<TemplateUidCountModel>> GetTemplatesUidDivisionCount();

        Task<List<TemplateDeptCount>> GetDeptAllCount(long divisionId, string? dept, string? deptclass, string subclass, DateTime? fromDate, DateTime? toDate);

        Task<List<TemplateDeptCount>> GetDeptUidCount(long divisionId);

        Task<List<TemplateDeptCount>> GetDeptBuyer1Count(long divisionId);

        Task<string> GetTiming();

        Task<List<GetDetails>> GetRoleName();

        Task<List<GetUserStudioModal>> GetAllUserStudioDetails();

        Task DeleteUser(IEnumerable<long> ids);

        Task DeleteRole(IEnumerable<long> ids);

        Task<string> DesignerL1Digestemail();

        Task<string> BuyerL1Digestemail();

        Task<string> ItemcodeDigestemail();

        Task<string> CamDigestemail();
    }
}