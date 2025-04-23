using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.BAL.Model.Template;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.Master;
using Microsoft.AspNetCore.Mvc;

namespace HomeCentre.PIM.BAL.Contracts;

public interface ITemplateManager
{
    Task AddOrUpdateTemplate(Template template);

    Task DeactivateTemplates(long[] ids);

    Task<SearchResult<Template>> SearchTemplates(string key, int pageNumber, int pageCount, long? departmentId,
        bool isActive);

    Task<IEnumerable<TemplateAttributeModel>> ListTemplateAttributes(long templateId);

    Task DeactivateAttribute(long[] ids);

    Task<byte[]> ItemcodeAllDownload(long marketplaceTemplateId, DateTime? fromDate, DateTime? toDate, string? division, string? deptcode, string? status, string? planogramname);

    Task<byte[]> DownloadTemplate(long templateId, string format);

    Task LinkAttributeToTemplate(TemplateAttribute templateAttribute);

    Task LinkAttributesToTemplate(List<TemplateAttribute> templateAttributes);

    Task RemoveAttributeFromTemplate(long templateAttributeId);

    Task<IEnumerable<Template>> GetTemplates();

    Task ActivateTemplate(long[] templateIds);

    Task PublishTemplate(long[] templateIds);

    Task<IEnumerable<Template>> GetUserTemplates();

    Task<IEnumerable<TemplateRole>> GetTemplateRoles(long templateId);

    Task RemoveAttributesFromTemplate(long[] templateAttributeIds);

    Task<Template> GetTemplate(long templateId);

    Task<byte[]> DownloadMasterDataAsExcel(long masterDataID);

    Task<IEnumerable> GetMasterData(long masterDataId);

    Task<byte[]> DownloadItemCodeTemplate(DateTime? fromdate, DateTime? toDate, string status, string? deptCode, string? division);

    Task<byte[]> DownloadAllItemCodeTemplate(DateTime? fromdate, DateTime? toDate, string status, string? deptCode, string? division);

    Task<FileContentResult> ItemCodeDownload(DateTime? fromDate, DateTime? toDate, long marketplaceTemplateId, string? division, string? deptcode, string? planogramname);

    Task<FileContentResult> ItemCodeSearchDownload(long[] itemcode, long marketplaceTemplateId);

    Task<byte[]> BulkDownload(long marketplacetemplateid);

    Task<List<List<string>>> MarketPlaceupload(MarketPlaceUpload marketPlaceUpload);

    Task<List<ItemCodeMultiSearch>> GetItemCodeMultiSearch(long[] itemcode);

    Task<List<ItemCodeSearch>> GetItemCode(ItemCodeData itemData);

    Task<List<GetCAMData>> GetItemCodeCAM(CAMSearch itemData);

    Task<List<ItemCodeSearch>> GetItemCodeSearch(long? marketplaceTemplateId);

    Task UpdateItemAndEanCode();

    Task<byte[]> TemplatesExportExcel(bool? isActive);

    Task DeleteTemplate(IEnumerable<long> ids);
}