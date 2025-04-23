using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.BAL.Model.App;
using HomeCentre.PIM.BAL.Model.Template;
using HomeCentre.PIM.BAL.Model.Uid;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.Master;
using HomeCentre.PIM.DAL.Staging;

namespace HomeCentre.PIM.BAL.Contracts
{
    public interface IDbCache
    {
        Task<IEnumerable<T>> GetMaster<T>()
            where T : Entity;

        Task Clear();

        Task ClearUserCache(long userId);

        Task<AppUser> GetUser(long userId);

        Task<AppUser> GetUserFromEmail(string userEmail);

        Task<AppConfiguration> GetAppConfiguration();

        Task<IEnumerable<LabelModel>> GetAllLabels(long? languageId);

        Task<int> GetMaxFileSize();

        Task<AppSettings> FindAppSettings(string key);

        Task<IEnumerable<AppSettings>> GetAppSettings();

        Task<IEnumerable<ProductAttribute>> GetAttributes(long? divisionId, bool isActive = true);

        Task<List<KeyValuePair<string, string>>> GetMaster(string attrLookupTable, string attrLookupTableColumn);

        Task<List<TemplateAttributeModel>> GetTemplateAttributes(long templateId);

        Task<List<Template>> GetTemplates();

        Task<List<ProductMetaModel>> GetDepartments();

        Task<List<ProductMetaModel>> GetProductClasses();

        Task<List<ProductMetaModel>> GetProductSubclasses();

        Task<List<Template>> GetUserTemplates();

        Task<List<string>> GetUids();

        Task<IEnumerable<string>> GetApprovedPlanoOrFamilyNames(long divisionId);

        Task<IEnumerable<long>> GetDeptCode();

        Task<IEnumerable<string>> GetCode();

        Task<List<TemplateMapping>> GetTemplateMappings(long id);

        Task<IEnumerable<string>> Purchasetype();

        Task<IEnumerable<string>> Pricegrouping();

        Task<IEnumerable<string>> Productpurpose();

        Task<IEnumerable<string>> Productpicture();

        Task<IEnumerable<long>> Planocode();

        Task<List<Merchandise_hierarchy>> GetMerchandiseHierarchy();
    }
}