using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.BAL.Model.Account;
using HomeCentre.PIM.BAL.Model.Tenant;
using HomeCentre.PIM.BAL.Model.Uid;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.CommonWeb;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.Master;
using HomeCentre.PIM.WorkFlow.Repository;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Cmp;
using Renci.SshNet.Sftp;

namespace HomeCentre.PIM.Api.Controllers.api;

[Route("/api/[controller]/[action]")]
[MultiplePoliciesAuthorize(Constants.Policies.All)]
public class UserController : BaseApiController
{
    private readonly IUserManager _userManager;
    private readonly IDataProcess _dataProcess;
    private readonly IDbCache _dbCache;

    public UserController(IUserManager userManager, IDataProcess dataProcess, IDbCache dbCache)
    {
        _userManager = userManager;
        _dataProcess = dataProcess;
        _dbCache = dbCache;
    }

    [HttpPost]
    public async Task AddOrUpdateUser([FromBody] HyperlinkEdit model)
    {
        await _userManager.AddOrUpdateUser(model);
    }

    [HttpPost]
    public async Task<string> ProcessDataFromTemplate(string messageName)
    {
        var isHAngFire = await _dbCache.FindAppSettings("IsHangFire");
        if (isHAngFire.Value.ToLower() == "true")
        {
            var jobid = BackgroundJob.Enqueue(() => _dataProcess.ProcessDataFromTemplate(messageName, false));
            return $"JobId:{jobid}";
        }

        await _dataProcess.ProcessDataFromTemplate(messageName, false);
        return "Added Successfully";
    }

    [HttpGet]
    public async Task<List<AppUser>> ActiveUsers()
    {
        return await _userManager.GetAllActiveUsers();
    }

    [HttpPost]
    public async Task<IActionResult> UserExportExcel(bool? isActive)
    {
        string format = "xlsx";
        var fileBytes = await _userManager.UserExportExcel(isActive);
        return File(fileBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"User.{format}");
    }

    [HttpPost]
    public async Task<IActionResult> RoleExportExcel(bool? isActive)
    {
        string format = "xlsx";
        var fileBytes = await _userManager.RoleExportExcel(isActive);
        return File(fileBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Role.{format}");
    }

    [HttpGet]
    public async Task<List<AppUser>> InactiveUsers()
    {
        return await _userManager.GetAllInactiveUsers();
    }

    [HttpGet]
    public async Task<SearchResult<UserListItemModel>> SearchUsers(bool isActive)
    {
        return await _userManager.SearchUsers(isActive);
    }

    [HttpGet]
    public async Task<SearchResult<UserListItemModel>> SearchAllUsers(IsActiveFilter isActiveFilter)
    {
        return await _userManager.SearchAllUsers(isActiveFilter);
    }

    [HttpGet]
    public async Task<SearchResult<Role>> SearchAllRoles(IsActiveFilter isActiveFilter)
    {
        return await _userManager.SearchAllRoles(isActiveFilter);
    }

    [HttpGet]
    public async Task<SearchResult<ProductAttribute>> SearchAllAttributes(IsActiveFilter isActiveFilter)
    {
        return await _userManager.SearchAllAttributes(isActiveFilter);
    }

    [HttpGet]
    public async Task<IEnumerable<ProductAttribute>> SearchAttributes(IsActiveFilter isActiveFilter, string? divisionId)
    {
        return await _userManager.SearchAttributes(isActiveFilter, divisionId);
    }

    [HttpGet]
    public async Task<IEnumerable<Template>> SearchTemplatesIsactive(IsActiveFilter isActiveFilter)
    {
        return await _userManager.SearchTemplates(isActiveFilter);
    }

    [HttpGet]
    public async Task<SearchResult<Template>> SearchAllTemplates(IsActiveFilter isActiveFilter)
    {
        return await _userManager.SearchAllTemplates(isActiveFilter);
    }

    [HttpGet]
    public async Task<SearchResult<Marketplace>> SearchAllMarketPlace(IsActiveFilter isActiveFilter)
    {
        return await _userManager.SearchAllMarketPlace(isActiveFilter);
    }

    [HttpGet]
    public async Task<List<GetDetails>> GetRoleName()
    {
        return await _userManager.GetRoleName();
    }

    [HttpGet]
    public async Task<List<GetUserStudioModal>> GetAllUserStudioDetails()
    {
        return await _userManager.GetAllUserStudioDetails();
    }

    [HttpGet]
    public async Task<AppUser> GetUserByEmail(string userEmail)
    {
        return await _userManager.GetUserByEmail(userEmail);
    }

    [HttpPut]
    public async Task UpdateUser(UserListItemModel appUser)
    {
        await _userManager.UpdateUser(appUser);
    }

    [HttpPut]
    public async Task ActivateUser(long id)
    {
        await _userManager.ActivateUser(id);
    }

    [HttpPut]
    public async Task ActivateUserBatch(long[] ids)
    {
        await _userManager.ActivateUserBatch(ids);
    }

    [HttpPut]
    public async Task DeactivateUser(long id)
    {
        await _userManager.DeactivateUser(id);
    }

    [HttpPut]
    public async Task DeactivateUserBatch(long[] ids)
    {
        await _userManager.DeactivateUserBatch(ids);
    }

    [HttpGet]
    public async Task<int> GetUserCount()
    {
        return await _userManager.GetUserCount();
    }

    [HttpPost]
    public async Task AddRole([FromBody] Role model)
    {
        model.Name = model.Name?.ToUpper();
        await _userManager.AddRole(model);
    }

    [HttpPost]
    public async Task AddUserRole([FromBody] AppUserRoleModel model)
    {
        await _userManager.AddUserRole(model);
    }

    [HttpPost]
    public async Task AddDepartmentToUser([FromBody] AppUserRoleModel appUserRoleModel)
    {
        await _userManager.AddUserDepartment(appUserRoleModel);
    }

    [HttpGet]
    public async Task<List<UserDepartment>> GetDepartmentDetails(string userId)
    {
        return await _userManager.GetDepartmentDetails(userId);
    }

    [HttpDelete]
    public async Task DeleteDepartmentToUser([FromBody] AppUserRole appUserRole)
    {
        await _userManager.DeleteDepartment(appUserRole);
    }

    [HttpPost]
    public async Task LinkUsersToMarketPlaces([FromBody] IEnumerable<AppuserMarketplace> appuserMarketplaces)
    {
        await _userManager.LinkUsersToMarketPlaces(appuserMarketplaces);
    }
    
    [HttpDelete]
    public async Task UnlinkUserFromMarketPlaces(long[] ids)
    {
        await _userManager.UnlinkUserFromMarketPlaces(ids);
    }

    [HttpGet]
    public async Task<IEnumerable<Marketplace>> GetMarketplacesOfUser(long userId)
    {
        return await _userManager.GetMarketplacesOfUser(userId);
    }

    [HttpPost]
    public async Task AddStudioToUser([FromBody] AppUserStudioModal appUserStudioModal)
    {
        await _userManager.AddStudioToUser(appUserStudioModal);
    }

    [HttpGet]
    public async Task<List<GetUserStudioModal>> GetUserStudioDetails(long userId)
    {
        return await _userManager.GetUserStudioDetails(userId);
    }

    [HttpDelete]
    public async Task DeleteStudioFromUser([FromBody] AppUserStudioDeleteModal appUserStudioDeleteModal)
    {
        await _userManager.DeleteStudioFromUser(appUserStudioDeleteModal);
    }

    [HttpDelete]
    public async Task DeleteUser(IEnumerable<long> ids)
    {
        await _userManager.DeleteUser(ids);
    }

    [HttpDelete]
    public async Task DeleteRole(IEnumerable<long> ids)
    {
        await _userManager.DeleteRole(ids);
    }

    [HttpGet]
    public async Task<IEnumerable<string>> ListFilesFromSftp()
    {
        return await _userManager.ListFilesFromSftp();
    }

    [HttpGet]
    public async Task<string> DesignerDigestemail()
    {
        return await _userManager.DesignerL1Digestemail();
    }

    [HttpGet]
    public async Task<string> BuyerDigestemail()
    {
        return await _userManager.BuyerL1Digestemail();
    }

    [HttpGet]
    public async Task<string> ItemcodeDigestemail()
    {
        return await _userManager.ItemcodeDigestemail();
    }

    [HttpGet]
    public async Task<string> CamDigestemail()
    {
        return await _userManager.CamDigestemail();
    }
}