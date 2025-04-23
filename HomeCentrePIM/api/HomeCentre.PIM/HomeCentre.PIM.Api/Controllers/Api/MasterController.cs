using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.BAL.Model.App;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.CommonWeb;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.Master;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Asn1.X509.Qualified;

namespace HomeCentre.PIM.Api.Controllers.Api
{
    [Route("/api/[controller]/[action]")]

    public class MasterController : BaseApiController
    {
        private readonly IDbCache _cache;
        private readonly ILabelManager _labelManager;
        private readonly IMasterManager _masterManager;
        private readonly IBlobRepo _blobRepo;

        // Constructor of the class.
        public MasterController(IDbCache cache, ILabelManager labelManager, IMasterManager masterManager, IBlobRepo blobRepo)
        {
            _cache = cache;
            _labelManager = labelManager;
            _masterManager = masterManager;
            _blobRepo = blobRepo;
        }

        // Get data from countries table.
        [HttpGet]
        public async Task<IEnumerable<Country>> GetCountries()
        {
            return (await _cache.GetMaster<Country>()).ToList().OrderBy(o => o.Name);
        }

        // Get data from currencies table.
        [HttpGet]
        public async Task<IEnumerable<Currency>> GetCurrencies()
        {
            return await _cache.GetMaster<Currency>();
        }

        // Get data from roles table.
        [HttpGet]
        public async Task<IEnumerable<Role>> GetRoles(bool isActive)
        {
            return await _masterManager.Search<Role>(isActive);
        }

        // Get data from genders table.
        [HttpGet]
        public async Task<IEnumerable<Gender>> GetGenders()
        {
            return await _cache.GetMaster<Gender>();
        }

        // Get data from table.
        [HttpGet]
        public async Task<AppConfiguration> GetAppConfiguration()
        {
            return await _cache.GetAppConfiguration();
        }

        // Get data from table.
        [HttpGet]
        public async Task<IEnumerable<LabelModel>> GetLabels(long? languageId)
        {
            return await _cache.GetAllLabels(languageId);
        }

        // Get data from table.
        [HttpGet]
        public async Task<SearchResult<LabelModel>> SearchLabels(
            long languageId)
        {
            return await _labelManager.Search(languageId);
        }

        // [HttpGet]
        // public async Task<SearchResult<LabelModel>> SearchLabels(string? searchKey, int pageNumber, int pageSize,
        //    long languageId)
        // {
        //    return await _labelManager.Search(searchKey, pageNumber, pageSize, languageId);
        // }

        // Get data from languages table.
        [HttpGet]
        public async Task<IEnumerable<Language>> GetLanguages()
        {
            return await _cache.GetMaster<Language>();
        }

        [HttpGet]
        public async Task<IEnumerable<AppSettings>> GetAppConfig()
        {
            return await _cache.GetAppSettings();
        }

        [HttpPut]
        public async Task ActivateRole(long id)
        {
            await _labelManager.ActivateRole(id);
        }

        [HttpPut]
        public async Task ActivateRoleBatch(long[] ids)
        {
            await _labelManager.ActivateRoleBatch(ids);
        }

        [HttpPut]
        public async Task DeactivateRole(long id)
        {
            await _labelManager.DeactivateRole(id);
        }

        [HttpPut]
        public async Task DeactivateRoleBatch(long[] ids)
        {
            await _labelManager.DeactivateRoleBatch(ids);
        }

        // [HttpPut]
        // public async Task<string> UpdateLabel(long id, string name, string
        // )
        // {
        //    return await _labelManager.UpdateLabel(id, name, description);
        // }
        [HttpPut]
        public async Task UpdateRoles(long id, string? name, string? description, bool? isActive)
        {
            await _labelManager.UpdateRoles(id, name, description, isActive);
        }

        [HttpPut]
        public async Task Ismail(string email)
        {
             await _labelManager.Ismail(email);
        }

        [HttpGet]
        public async Task<IEnumerable> GetUserRoles(long? appuserId)
        {
            return await _masterManager.GetUserRole(appuserId);
        }

        [HttpDelete]
        public async Task DeleteRoles(long appuserId, long roleId)
        {
            await _labelManager.DeleteRoles(appuserId, roleId);
        }

        [HttpGet]
        public async Task<IEnumerable> DownloadBlobFile(string url)
        {
            return await _blobRepo.DownloadFile(url);
        }

        [HttpPost]
        public async Task UploadBlobFile(string fileName, string containerName, string filePath)
        {
            await _blobRepo.UploadFile(fileName, containerName, filePath);
        }

        [HttpDelete]
        public async Task DeleteBlobFile(string containerName, string fileName)
        {
            await _blobRepo.DeleteFile(containerName, fileName);
        }

        [HttpGet]
        public async Task<IEnumerable> CopyBlobFile(string sourceUrl, string destinationContainer, string destinationFileName)
        {
            return await _blobRepo.CopyFile(sourceUrl, destinationContainer, destinationFileName);
        }

        [HttpGet]
        public async Task<IEnumerable<ProductMetaModel>> GetDepartments()
        {
            return await _cache.GetDepartments();
        }
        
        [HttpGet]
        public async Task<IEnumerable<ProductMetaModel>> GetProductClasses()
        {
            return await _cache.GetProductClasses();
        }
        
        [HttpGet]
        public async Task<IEnumerable<ProductMetaModel>> GetProductSubclasses()
        {
            return await _cache.GetProductSubclasses();
        }

        [HttpGet]
        public async Task<IEnumerable> GetErrorLogUser(DateTime fromDate, DateTime toDate)
        {
            return await _labelManager.GetErrorLogUser(fromDate, toDate);
        }

        [HttpGet]
        public async Task<IEnumerable> GetUserDepartment(long? appuserId)
        {
            return await _masterManager.GetUserDepartment(appuserId);
        }

        [HttpGet]
        public async Task<IEnumerable<RejectionReason>> GetRejectReason()
        {
            return await _cache.GetMaster<RejectionReason>();
        }

        // public async Task<IEnumerable<Division>> GetDivision()
        // {
        //    return await _cache.GetMaster<Division>();
        // }
        [HttpGet]
        public async Task<IEnumerable<Division>> GetDivision()
        {
            var value = await _cache.GetMaster<Division>();
            var orderedValue = value.OrderBy(x => x.Code == "61" ? 0 : x.Code == "4" ? 1 : 2);

            return orderedValue;
        }

        [HttpGet]
        public async Task<IEnumerable<UploadedFileStatus>> GetUploadedFileStatus()
        {
            return await _cache.GetMaster<UploadedFileStatus>();
        }
        
        [HttpGet]
        public async Task<IEnumerable<TemplateStatus>> GetTemplateStatus()
        {
            return await _cache.GetMaster<TemplateStatus>();
        }
        
        [HttpGet]
        public async Task<IEnumerable<QueryType>> GetQueryTypes()
        {
            return await _cache.GetMaster<QueryType>();
        }
        
        [HttpGet]
        public async Task<IEnumerable<QueryPriority>> GetQueryPriorities()
        {
            return await _cache.GetMaster<QueryPriority>();
        }
        
        [HttpGet]
        public async Task<IEnumerable<ProductQcStatus>> GetQcStatus()
        {
            return await _cache.GetMaster<ProductQcStatus>();
        }
        
        [HttpGet]
        public async Task<IEnumerable<TableMetadata>> GetTables()
        {
            return await _cache.GetMaster<TableMetadata>();
        }

        [HttpGet]
        public async Task<IEnumerable<TableMetaDataColumn>> GetTableColumns()
        {
            return await _cache.GetMaster<TableMetaDataColumn>();
        }

        [HttpGet]
        public async Task<IEnumerable<Season>> GetSeasons()
        {
            return await _cache.GetMaster<Season>();
        }

        [HttpGet]
        public async Task<IEnumerable<HelpEmail>> GetHelpEmail()
        {
            return await _cache.GetMaster<HelpEmail>();
        }
    }
}