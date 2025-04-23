using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using DocumentFormat.OpenXml.Wordprocessing;
using HomeCentre.PIM.Api.Models;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Implementations;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.BAL.Model.Template;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.Interfaces;
using HomeCentre.PIM.CommonWeb;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.Master;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;

namespace HomeCentre.PIM.Api.Controllers.Api;

[Route("/api/[controller]/[action]")]
public class TemplateController : BaseApiController
{
    private readonly ITemplateManager _manager;
    private readonly IUserContext _context;
    private readonly IDbCache _cache;
    private readonly IDataProcess _process;

    public TemplateController(ITemplateManager manager, IUserContext context, IDbCache cache, IDataProcess process)
    {
        _manager = manager;
        _context = context;
        _cache = cache;
        _process = process;
    }

    [HttpGet]
    public async Task<SearchResult<Template>> SearchTemplates(string? key, int pageNumber, int pageCount, bool isActive = true)
    {
        return await _manager.SearchTemplates(key, pageNumber, pageCount, await _context.GetDepartmentId(), isActive);
    }

    [HttpGet]
    public async Task<IEnumerable<TemplateAttributeModel>> ListTemplateAttributes(long templateId)
    {
        return await _manager.ListTemplateAttributes(templateId);
    }

    [HttpGet]
    public async Task<IEnumerable<TemplateRole>> GetTemplateRoles(long templateId)
    {
        return await _manager.GetTemplateRoles(templateId);
    }

    [HttpPost]
    public async Task AddOrUpdateTemplate([FromBody] Template template)
    {
        await _manager.AddOrUpdateTemplate(template);
    }

    [HttpPost]
    public async Task LinkAttributeToTemplate([FromBody] TemplateAttribute template)
    {
        await _manager.LinkAttributeToTemplate(template);
    }

    [HttpPost]
    public async Task LinkAttributesToTemplate([FromBody] List<TemplateAttribute> attributes)
    {
        await _manager.LinkAttributesToTemplate(attributes);
    }

    [HttpDelete]
    public async Task RemoveAttributeFromTemplate(long templateAttributeId)
    {
        await _manager.RemoveAttributeFromTemplate(templateAttributeId);
    }

    [HttpDelete]
    public async Task RemoveAttributesFromTemplate(long[] templateAttributeIds)
    {
        await _manager.RemoveAttributesFromTemplate(templateAttributeIds);
    }

    [HttpPut]
    public async Task DeactivateTemplates(long[] templateIds)
    {
        await _manager.DeactivateTemplates(templateIds);
    }

    [HttpPut]
    public async Task ActivateTemplates(long[] templateIds)
    {
        await _manager.ActivateTemplate(templateIds);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadTemplate(long templateId, string? format = "xlsx")
    {
        format ??= "xlsx";
        var template = await _manager.GetTemplate(templateId);
        var division = (await this._cache.GetMaster<Division>()).FirstOrDefault(x => x.Id == template.DivisionId);
        var file = await _manager.DownloadTemplate(templateId, format);
        return File(file,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{template.Name}.{format}");
    }

    [HttpGet]
    public async Task<Template> GetDownloadTemplate(long templateId)
    {
        return await _manager.GetTemplate(templateId);
    }

    [HttpPut]
    public async Task PublishTemplate(long[] templateIds)
    {
        await _manager.PublishTemplate(templateIds);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadMasterDataAsExcel(long masterDataID, string name)
    {
        var fileData = await _manager.DownloadMasterDataAsExcel(masterDataID);
        if (fileData == null || fileData.Length == 0)
        {
            return NotFound();
        }

        return File(fileData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{name}.xlsx");
    }

    [HttpGet]
    public async Task<IEnumerable> GetMasterData(long masterDataId)
    {
        return await _manager.GetMasterData(masterDataId);
    }

    [HttpPost]
    public async Task MasterDataUpload(string filePath, long masterId)
    {
        await _process.MasterDataUpload(filePath, masterId);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadItemCodeTemplate(DateTime? fromdate, DateTime? toDate, string status, string? deptCode, string? division)
    {
        string format = "xlsx";
        var fileContent = await _manager.DownloadItemCodeTemplate(fromdate, toDate, status, deptCode, division);

        string fileName = $"Item_Code_Template.{format}";
        string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        return File(fileContent, contentType, fileName);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadAllItemCodeTemplate(DateTime? fromdate, DateTime? toDate, string status, string? deptCode, string? division)
    {
        string format = "xlsx";
        var fileContent = await _manager.DownloadAllItemCodeTemplate(fromdate, toDate, status, deptCode, division);
        return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Item_Code_Template.{format}");
    }

    [HttpGet]
    public async Task<IActionResult> ItemCodeDownload([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, long marketplaceTemplateId, string? division, string? deptcode, string? planogramname)
    {
        var fileResult = await _manager.ItemCodeDownload(fromDate, toDate, marketplaceTemplateId, division, deptcode, planogramname);
        return fileResult;
    }

    [HttpGet]
    public async Task<IActionResult> ItemcodeAllDownload(long marketplaceTemplateId, DateTime? fromDate, DateTime? toDate, string? division, string? deptcode, string? status, string? planogramname)
    {
        string format = "xlsx";
        var fileResult = await _manager.ItemcodeAllDownload(marketplaceTemplateId, fromDate, toDate, division, deptcode, status, planogramname);
        return File(fileResult, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"ItemCode.{format}");
    }

    [HttpGet]
    public async Task<IActionResult> ItemCodeSearchDownload(long[] itemcode, long marketplaceTemplateId)
    {
        var fileResult = await _manager.ItemCodeSearchDownload(itemcode, marketplaceTemplateId);
        return fileResult;
    }

    [HttpGet]
    public async Task<List<ItemCodeSearch>> GetItemCode(ItemCodeData itemData)
    {
        return await _manager.GetItemCode(itemData);
    }

    [HttpGet]
    public async Task<List<GetCAMData>> GetItemCodeCAM(CAMSearch itemData)
    {
        return await _manager.GetItemCodeCAM(itemData);
    }

    [HttpGet]
    public async Task<List<ItemCodeMultiSearch>> GetItemCodeMultiSearch(long[] itemcode)
    {
        return await _manager.GetItemCodeMultiSearch(itemcode);
    }

    [HttpGet]
    public async Task<List<ItemCodeSearch>> GetItemCodeSearch(long? marketplaceTemplateId)
    {
        return await _manager.GetItemCodeSearch(marketplaceTemplateId);
    }

    [HttpGet]
    public async Task<IActionResult> MarketPlaceDownload(long marketplacetemplateid)
    {
        var fileData = await _manager.BulkDownload(marketplacetemplateid);
        if (fileData == null || fileData.Length == 0)
        {
            return NotFound();
        }

        return File(fileData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"MarketPlace.xlsx");
    }

    [HttpPost]
    public async Task MarketPlaceUpload([FromBody] MarketPlaceUpload marketPlaceUpload)
    {
        await _manager.MarketPlaceupload(marketPlaceUpload);
    }

    [HttpPost]
    public async Task<IActionResult> TemplatesExportExcel(bool? isActive)
    {
        string format = "xlsx";
        var fileBytes = await _manager.TemplatesExportExcel(isActive);
        return File(fileBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Templates.{format}");
    }

    [HttpDelete]
    public async Task DeleteTemplate(IEnumerable<long> ids)
    {
        await _manager.DeleteTemplate(ids);
    }

    [HttpGet]

    public async Task UpdateItemAndEanCode()
    {
        await _manager.UpdateItemAndEanCode();
    }
}