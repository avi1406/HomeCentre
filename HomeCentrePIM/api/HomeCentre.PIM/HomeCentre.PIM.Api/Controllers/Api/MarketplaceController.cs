using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HomeCentre.PIM.Api.Models;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.CommonWeb;
using HomeCentre.PIM.DAL;
using Microsoft.AspNetCore.Mvc;

namespace HomeCentre.PIM.Api.Controllers.Api;

[Route("/api/[controller]/[action]")]
public class MarketplaceController : BaseApiController
{
    private readonly IMarketplaceManager _manager;

    public MarketplaceController(IMarketplaceManager manager)
    {
        _manager = manager;
    }

    [HttpPost]
    public async Task<Marketplace> AddOrUpdateMarketplace([FromBody] Marketplace marketplace)
    {
        return await _manager.AddOrUpdate(marketplace);
    }

    [HttpPost]
    public async Task<MarketplaceAttribute> AddOrUpdateMarketplaceAttribute([FromBody] MarketplaceAttribute attribute)
    {
        return await _manager.AddOrUpdateAttribute(attribute);
    }

    [HttpPost]
    public async Task<IEnumerable<MarketplaceAttribute>> AddOrUpdateMarketplaceAttributes([FromBody] IEnumerable<MarketplaceAttribute> attributes)
    {
        return await _manager.AddOrUpdateAttributes(attributes).ToListAsync();
    }

    [HttpPost]
    public async Task ProcessUploadedMarketplaceAttributes([FromBody] UploadMarketplaceAttributeModel model)
    {
        await _manager.ProcessUploadedMarketplaceAttributes(model.MarketplaceId, model.DivisionId, model.BlobUrl);
    }

    [HttpGet]
    public async Task<FileContentResult> DownloadMarketplaceAttributeTemplate()
    {
        var file = await _manager.DownloadMarketplaceAttributeTemplate();
        return this.File(await System.IO.File.ReadAllBytesAsync(file),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Marketplace-Attribute.xlsx".ToUniqueFileNameUrl());
    }

    [HttpPost]
    public async Task<MarketplaceTemplate> AddOrUpdateMarketplaceTemplate([FromBody] MarketplaceTemplate template)
    {
        return await _manager.AddOrUpdateTemplate(template);
    }

    [HttpPost]
    public async Task<MarketplaceTemplateAttribute> AddOrUpdateMarketplaceTemplateAttribute([FromBody] MarketplaceTemplateAttribute attribute)
    {
        return await _manager.AddOrUpdateTemplateAttribute(attribute);
    }

    [HttpPost]
    public async Task<IEnumerable<MarketplaceTemplateAttribute>> AddOrUpdateMarketplaceTemplateAttributes([FromBody] IEnumerable<MarketplaceTemplateAttribute> attributes)
    {
        return await _manager.AddOrUpdateTemplateAttributes(attributes).ToListAsync();
    }

    [HttpGet]
    public async Task<SearchResult<Marketplace>> SearchMarketplaces(string searchKey, int pageNumber, int pageSize,
 bool isActive)
    {
        return await _manager.SearchMarketplaces(searchKey, pageNumber, pageSize, isActive);
    }

    [HttpGet]
    public async Task<SearchResult<MarketplaceAttribute>> SearchMarketplaceAttributes(string searchKey, int pageNumber,
        int pageSize, long marketPlaceId, long divisionId)
    {
        return await _manager.SearchMarketplaceAttributes(searchKey, pageNumber, pageSize, marketPlaceId, divisionId);
    }

    [HttpGet]
    public async Task<SearchResult<MarketplaceTemplate>> SearchMarketplaceTemplates(string searchKey, int pageNumber,
    int pageSize, long marketPlaceId, long[]? divisionId,
    long[]? productDepartmentId, bool isActive)
    {
        return await _manager.SearchMarketplaceTemplates<MarketplaceTemplate>(searchKey, pageNumber, pageSize, marketPlaceId, divisionId, productDepartmentId, isActive);
    }

    [HttpGet]
    public async Task<SearchResult<MarketplaceTemplate>> SearchMarketplaceTemplatesCam(string searchKey, int pageNumber,
        int pageSize, long marketPlaceId, long? divisionId,
        long? productDepartmentId, bool isActive)
    {
        return await _manager.SearchMarketplaceTemplatescam(searchKey, pageNumber, pageSize, marketPlaceId, divisionId, productDepartmentId, isActive);
    }

    [HttpGet]
    public async Task<SearchResult<MarketplaceTemplateAttribute>> SearchMarketplaceTemplateAttributes(string searchKey, int pageNumber,
        int pageSize, long marketPlaceTemplateId)
    {
        return await _manager.SearchMarketplaceTemplateAttributes(searchKey, pageNumber, pageSize, marketPlaceTemplateId);
    }

    [HttpGet]
    public async Task<List<MarketplaceTemplateAttribute>> GetMarketplaceTemplateAttributes(string searchKey, int pageNumber,
        int pageSize, long marketPlaceTemplateId)
    {
        return await _manager.GetMarketplaceTemplateAttributes(searchKey, pageNumber, pageSize, marketPlaceTemplateId);
    }

    [HttpGet]
    public async Task<List<MarketplacetemplateDiv>> GetMarketplaceTemplateAttributesDiv(long marketPlaceTemplateId)
    {
        return await _manager.GetMarketplaceTemplateAttributesDiv(marketPlaceTemplateId);
    }

    [HttpPut]
    public async Task ActivateMarketplaces([FromBody] long[] ids)
    {
        await _manager.ActivateMarketplaces(ids);
    }

    [HttpPut]
    public async Task ActivateMarketplaceAttributes([FromBody] long[] ids)
    {
        await _manager.ActivateMarketplaceAttributes(ids);
    }

    [HttpPut]
    public async Task ActivateMarketplaceTemplates([FromBody] long[] ids)
    {
        await _manager.ActivateMarketplaceTemplates(ids);
    }

    [HttpPut]
    public async Task ActivateMarketplaceTemplateAttributes([FromBody] long[] ids)
    {
        await _manager.ActivateMarketplaceTemplateAttributes(ids);
    }

    [HttpPut]
    public async Task DeactivateMarketplaceTemplateAttributes([FromBody] long[] ids)
    {
        await _manager.DeactivateMarketplaceTemplateAttributes(ids);
    }

    [HttpPut]
    public async Task DeactivateMarketplaces([FromBody] long[] ids)
    {
        await _manager.DeactivateMarketplaces(ids);
    }

    [HttpPut]
    public async Task DeactivateMarketplaceAttributes([FromBody] long[] ids)
    {
        await _manager.DeactivateMarketplaceAttributes(ids);
    }

    [HttpPut]
    public async Task DeactivateMarketplaceTemplates([FromBody] long[] ids)
    {
        await _manager.DeactivateMarketplaceTemplates(ids);
    }

    [HttpDelete]
    public async Task RemoveMarketplaceTemplateAttributes([FromBody] long[] ids, string? templateid)
    {
        await _manager.RemoveMarketplaceTemplateAttributes(ids, templateid);
    }

    [HttpDelete]
    public async Task RemoveMarketplaces([FromBody] long[] ids)
    {
        await _manager.RemoveMarketplaces(ids);
    }

    [HttpDelete]
    public async Task RemoveMarketplaceAttributes([FromBody] long[] ids)
    {
        await _manager.RemoveMarketplaceAttributes(ids);
    }

    [HttpDelete]
    public async Task RemoveMarketplaceTemplates([FromBody] long[] ids)
    {
        await _manager.RemoveMarketplaceTemplates(ids);
    }

    [HttpPost]
    public async Task<IActionResult> MarketPlaceExportExcel(bool? isactive)
    {
        string format = "xlsx";
        var fileBytes = await _manager.MarketPlaceExportExcel(isactive);
        return File(fileBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"MarketPlace.{format}");
    }

    [HttpDelete]
    public async Task DeleteMarketPlace(IEnumerable<long> ids)
    {
        await _manager.DeleteMarketPlace(ids);
    }
}