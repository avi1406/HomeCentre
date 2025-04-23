using System.Collections.Generic;
using System.Threading.Tasks;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.DAL;

namespace HomeCentre.PIM.BAL.Contracts;

public interface IMarketplaceManager
{
    Task<Marketplace> AddOrUpdate(Marketplace marketplace);

    Task<MarketplaceAttribute> AddOrUpdateAttribute(MarketplaceAttribute marketplaceAttribute);

    Task<MarketplaceTemplate> AddOrUpdateTemplate(MarketplaceTemplate marketplaceTemplate);

    Task<MarketplaceTemplateAttribute> AddOrUpdateTemplateAttribute(MarketplaceTemplateAttribute marketplaceTemplateAttribute);

    Task<SearchResult<Marketplace>> SearchMarketplaces(string searchKey, int pageNumber, int pageSize, bool isActive);

    Task<SearchResult<T>> SearchMarketplaceTemplates<T>(string searchKey, int pageNumber,
    int pageSize, long marketPlaceId, long[]? divisionId,
    long[]? productDepartmentId, bool isActive);

    Task<SearchResult<MarketplaceTemplate>> SearchMarketplaceTemplatescam(string searchKey, int pageNumber,
        int pageSize, long marketPlaceId, long? divisionId,
        long? productDepartmentId, bool isActive);

    Task<SearchResult<MarketplaceAttribute>> SearchMarketplaceAttributes(string searchKey, int pageNumber, int pageSize,
        long marketPlaceId, long divisionId);

    Task<SearchResult<MarketplaceTemplateAttribute>> SearchMarketplaceTemplateAttributes(string searchKey, int pageNumber, int pageSize, long marketPlaceTemplateId);

    Task<List<MarketplaceTemplateAttribute>> GetMarketplaceTemplateAttributes(string searchKey, int pageNumber, int pageSize, long marketPlaceTemplateId);

    Task<List<MarketplacetemplateDiv>> GetMarketplaceTemplateAttributesDiv(long marketPlaceTemplateId);

    Task RemoveMarketplaces(long[] ids);

    Task RemoveMarketplaceAttributes(long[] ids);

    Task RemoveMarketplaceTemplates(long[] ids);

    Task RemoveMarketplaceTemplateAttributes(long[] ids, string? templateid);

    Task ActivateMarketplaces(long[] ids);

    Task ActivateMarketplaceAttributes(long[] ids);

    Task ActivateMarketplaceTemplates(long[] ids);

    Task ActivateMarketplaceTemplateAttributes(long[] ids);

    Task DeactivateMarketplaces(long[] ids);

    Task DeactivateMarketplaceAttributes(long[] ids);

    Task DeactivateMarketplaceTemplates(long[] ids);

    Task DeactivateMarketplaceTemplateAttributes(long[] ids);

    Task ProcessUploadedMarketplaceAttributes(long marketplaceId, long divisionId, string blobUrl);

    Task<string> DownloadMarketplaceAttributeTemplate();

    IAsyncEnumerable<MarketplaceAttribute> AddOrUpdateAttributes(IEnumerable<MarketplaceAttribute> attributes);

    IAsyncEnumerable<MarketplaceTemplateAttribute> AddOrUpdateTemplateAttributes(IEnumerable<MarketplaceTemplateAttribute> attributes);

    Task<byte[]> MarketPlaceExportExcel(bool? isactive);

    Task DeleteMarketPlace(IEnumerable<long> ids);
}