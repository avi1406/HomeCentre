using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.BAL.Model.App;
using HomeCentre.PIM.BAL.Model.Template;
using HomeCentre.PIM.BAL.Model.Uid;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.Master;
using HomeCentre.PIM.DAL.Staging;
using HomeCentre.PIM.WorkFlow.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomeCentre.PIM.BAL.Implementations
{
    public class PgDbCache : IDbCache
    {
        private const string Tenants = "Tenants";
        private const string Users = "Users";
        private readonly IConfiguration _configuration;
        private readonly ICacheHandler _handler;
        private readonly Lazy<ILabelManager> _labelManager;
        private readonly Lazy<IMasterManager> _masterManager;
        private readonly Lazy<IUserManager> _userManager;
        private readonly Lazy<ITemplateManager> _templateManager;
        private readonly Lazy<IProductManager> _productManager;
        private readonly Lazy<IUIDManager> _uidRepo;

        public PgDbCache(ICacheHandler handler,
            IServiceProvider services,
            IConfiguration configuration)
        {
            _handler = handler;
            _configuration = configuration;
            _userManager = new Lazy<IUserManager>(services.GetRequiredService<IUserManager>);
            _masterManager = new Lazy<IMasterManager>(services.GetRequiredService<IMasterManager>);
            _labelManager = new Lazy<ILabelManager>(services.GetRequiredService<ILabelManager>);
            _templateManager = new Lazy<ITemplateManager>(services.GetRequiredService<ITemplateManager>);
            _productManager = new Lazy<IProductManager>(services.GetRequiredService<IProductManager>);
            _uidRepo = new Lazy<IUIDManager>(services.GetRequiredService<IUIDManager>);
        }

        public async Task Clear()
        {
            await _handler.RemoveItemFromCache(Constants.AllCacheKeys);
        }

        public async Task ClearUserCache(long userId)
        {
            await _handler.RemoveItemFromCache($"{Users}-{userId}");
            await _handler.RemoveItemFromCache($"{Users}-{userId}-me");
        }

        public async Task<AppSettings> FindAppSettings(string key)
        {
            var cacheValue = await this.GetAppSettings();
            return cacheValue.FirstOrDefault(x => x.Name == key);
        }

        public async Task<IEnumerable<LabelModel>> GetAllLabels(long? languageId)
        {
            var cacheValue = await _handler.GetFromCacheAsync(
                $"Labels-{languageId}",
                async args => await _labelManager.Value.GetLabels(languageId));
            return cacheValue;
        }

        public async Task<AppConfiguration> GetAppConfiguration()
        {
            var cacheValue = await _handler.GetFromCacheAsync(
                $"AppConfiguration",
                async args => await Task.FromResult(new AppConfiguration
                {
                    ApiUrl = _configuration["ApiUrl"],
                    AppUrl = _configuration["AppUrl"],
                    B2C = new B2CConfiguration
                    {
                        Tenant = _configuration["B2C-Tenant"],
                        ClientId = _configuration["B2C-ClientId"],
                        LoginDomain = _configuration["B2C-BaseAddress"]?.Split("//")[1],
                        LoginFlow = _configuration["B2C-LoginPolicy"],
                        ResetPasswordFlow = _configuration["B2C-ForgotPasswordFlow"]
                    }
                }));
            return cacheValue;
        }

        public async Task<IEnumerable<AppSettings>> GetAppSettings()
        {
            var cacheValue = await _handler.GetFromCacheAsync(
                $"AppSettings",
                async args => await _masterManager.Value.GetAppSettings());
            return cacheValue;
        }

        public async Task<IEnumerable<ProductAttribute>> GetAttributes(long? divisionId, bool isActive = true)
        {
            var cacheValue = await _handler.GetFromCacheAsync(
                $"GetAttributes-{isActive}-{divisionId}",
                async args => (await _productManager.Value.ListAttributes(divisionId, isActive)));
            return cacheValue;
        }

        public async Task<List<KeyValuePair<string, string>>> GetMaster(string attrLookupTable,
            string attrLookupTableColumn)
        {
            var cacheValue = await _handler.GetFromCacheAsync(
                $"GetMaster-{attrLookupTable}",
                async args => await _masterManager.Value.GetMaster(attrLookupTable, attrLookupTableColumn));
            return cacheValue;
        }

        public async Task<List<TemplateAttributeModel>> GetTemplateAttributes(long templateId)
        {
            var cacheValue = await _handler.GetFromCacheAsync(
                $"GetTemplateAttributes-{templateId}",
                async args => (await _templateManager.Value.ListTemplateAttributes(templateId)).ToList());
            return cacheValue;
        }

        public async Task<List<Template>> GetTemplates()
        {
            var cacheValue = await _handler.GetFromCacheAsync(
                $"GetTemplates",
                async args => (await _templateManager.Value.GetTemplates()).ToList());
            return cacheValue;
        }

        public async Task<List<Template>> GetUserTemplates()
        {
            var cacheValue = await _handler.GetFromCacheAsync(
                $"GetUserTemplates",
                async args => (await _templateManager.Value.GetUserTemplates()).ToList());
            return cacheValue;
        }

        public async Task<List<string>> GetUids()
        {
            var cacheValue = await _handler.GetFromCacheAsync(
                $"GetUids",
                async args => (await _productManager.Value.GetUids()).ToList());
            return cacheValue;
        }

        public async Task<IEnumerable<string>> GetApprovedPlanoOrFamilyNames(long divisionId)
        {
            var cacheValue = await _handler.GetFromCacheAsync(
                $"UidRepo-{divisionId}",
                async args => (await _uidRepo.Value.GetApprovedPlanoOrFamilyNames(divisionId)).ToList());
            return cacheValue;
        }

        public async Task<List<ProductMetaModel>> GetDepartments()
        {
            var cacheValue = await _handler.GetFromCacheAsync(
                $"GetDepartments",
                async args =>
                {
                    var divisions = (await this.GetMaster<Division>()).ToList();
                    var hM = await this.GetMaster<HierarchicalMaster>();
                    return hM.Select(x => new ProductMetaModel
                    {
                        Code = x.DepartmentCode,
                        Name = x.DepartmentName,
                        Id = long.Parse(x.DepartmentCode),
                        ParentCode = divisions.FirstOrDefault(y => y.Code == x.DivisionCode)?.Code,
                        UidKeys = divisions.FirstOrDefault(y => y.Code == x.DivisionCode)?.UidKeys
                    }).DistinctBy(x => x.Code).ToList();
                });
            return cacheValue;
        }

        public async Task<List<ProductMetaModel>> GetProductClasses()
        {
            var cacheValue = await _handler.GetFromCacheAsync(
                $"GetProductClasses",
                async args =>
                {
                    var departments = await this.GetDepartments();
                    var hM = await this.GetMaster<HierarchicalMaster>();
                    return hM.Select(x => new ProductMetaModel
                    {
                        Code = x.ClassCode,
                        Name = x.ClassName,
                        Id = long.Parse(x.ClassCode),
                        ParentCode = departments.FirstOrDefault(y => y.Code == x.DepartmentCode)?.Code
                    }).DistinctBy(x => x.Code).ToList();
                });
            return cacheValue;
        }

        public async Task<List<ProductMetaModel>> GetProductSubclasses()
        {
            var cacheValue = await _handler.GetFromCacheAsync(
                $"GetProductSubclasses",
                async args =>
                {
                    var departments = await this.GetProductClasses();
                    var hM = await this.GetMaster<HierarchicalMaster>();
                    return hM.Select(x => new ProductMetaModel
                    {
                        Code = x.SubclassCode,
                        Name = x.SubclassName,
                        Id = long.Parse(x.SubclassCode),
                        ParentCode = departments.FirstOrDefault(y => y.Code == x.ClassCode)?.Code
                    }).DistinctBy(x => x.Code).ToList();
                });
            return cacheValue;
        }

        public async Task<IEnumerable<T>> GetMaster<T>()
                                                            where T : Entity
        {
            var cacheValue = await _handler.GetFromCacheAsync(
                $"Master-{typeof(T).FullName}",
                async args => await _masterManager.Value.GetAll<T>());
            return cacheValue;
        }

        public async Task<int> GetMaxFileSize()
        {
            var cacheValue = await _handler.GetFromCacheAsync(
                "GetMaxFileSize",
                async args =>
                {
                    int maxFileSize = int.TryParse(_configuration["MaxFileSize"], out maxFileSize)
                        ? maxFileSize
                        : 2097152;
                    return await Task.FromResult(maxFileSize);
                });
            return cacheValue;
        }

        public async Task<AppUser> GetUser(long userId)
        {
            var cacheValue = await _handler.GetFromCacheAsync(
                $"{Users}-{userId}",
                async args =>
                {
                    var user = await _userManager.Value.Get(userId);
                    return user;
                });
            return cacheValue;
        }

        public async Task<AppUser> GetUserFromEmail(string userEmail)
        {
            var cacheValue = await _handler.GetFromCacheAsync(
                $"UserEmails-{userEmail}",
                async args =>
                {
                    var user = await _userManager.Value.GetUserByEmail(userEmail);
                    return user;
                });
            return cacheValue;
        }

        public async Task<IEnumerable<long>> GetDeptCode()
        {
            var cacheValue = await _handler.GetFromCacheAsync(
                $"UidRepo",
                async args => (await _uidRepo.Value.GetDeptCode()));
            return cacheValue;
        }

        public async Task<IEnumerable<string>> GetCode()
        {
            var cacheValue = await _handler.GetFromCacheAsync(
                $"subclass",
                async args => (await _uidRepo.Value.GetCode()));
            return cacheValue;
        }

        public async Task<IEnumerable<string>> Purchasetype()
        {
            var cacheValue = await _handler.GetFromCacheAsync(
            $"purchasetype",
            async args => (await _uidRepo.Value.Purchasetype()));
            return cacheValue;
        }

        public async Task<IEnumerable<string>> Pricegrouping()
        {
            var cacheValue = await _handler.GetFromCacheAsync(
            $"Pricegrouping",
            async args => (await _uidRepo.Value.Pricegrouping()));
            return cacheValue;
        }

        public async Task<IEnumerable<string>> Productpurpose()
        {
            var cacheValue = await _handler.GetFromCacheAsync(
            $"Productpurpose",
            async args => (await _uidRepo.Value.Productpurpose()));
            return cacheValue;
        }

        public async Task<IEnumerable<long>> Planocode()
        {
            var cacheValue = await _handler.GetFromCacheAsync(
            $"Planocode",
            async args => (await _uidRepo.Value.Planocode()));
            return cacheValue;
        }

        public async Task<IEnumerable<string>> Productpicture()
        {
            var cacheValue = await _handler.GetFromCacheAsync(
            $"Productpicture",
            async args => (await _uidRepo.Value.Productpicture()));
            return cacheValue;
        }

        public async Task<List<TemplateMapping>> GetTemplateMappings(long id)
        {
            var cacheValue = await _handler.GetFromCacheAsync(
                $"TemplateMappings",
                async args =>
                {
                    var user = await _userManager.Value.GetTemplateMappings(id);
                    return user;
                });
            return cacheValue;
        }

        public async Task<List<Merchandise_hierarchy>> GetMerchandiseHierarchy()
        {
            var cacheValue = await _handler.GetFromCacheAsync(
                $"merchandiseHierarchy",
                async args =>
                {
                    var masterlist = await _masterManager.Value.GetMerchandiseHierarchy();
                    return masterlist;
                });
            return cacheValue;
        }
    }
}
