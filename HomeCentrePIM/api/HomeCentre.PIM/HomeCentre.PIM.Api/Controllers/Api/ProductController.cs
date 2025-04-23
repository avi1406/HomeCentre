using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using HomeCentre.PIM.Api.Models;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.BAL.Model.Uid;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.CommonWeb;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.Master;
using HomeCentre.PIM.WorkFlow.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using NPOI.HPSF;
using OfficeOpenXml;

namespace HomeCentre.PIM.Api.Controllers.Api;

[Route("/api/[controller]/[action]")]
public class ProductController : BaseApiController
{
    private readonly IProductManager _manager;
    private readonly IDbCache _cache;

    public ProductController(IProductManager manager, IDbCache cache)
    {
        _manager = manager;
        _cache = cache;
    }

    [HttpGet]
    public async Task<IEnumerable<ProductAttribute>> ListAttributes(long? divisionId, bool isActive = true)
    {
        return await _cache.GetAttributes(divisionId, isActive);
    }

    [HttpPost]
    public async Task AddOrUpdateAttribute([FromBody] ProductAttribute attribute)
    {
        await _manager.AddOrUpdateAttribute(attribute);
    }

    [HttpPut]
    public async Task ActivateAttributes([FromBody] long[] attributeIds)
    {
        await _manager.ActivateAttributes(attributeIds);
    }

    [HttpPut]
    public async Task DeactivateAttributes([FromBody] long[] attributeIds)
    {
        await _manager.DeactivateAttributes(attributeIds);
    }

    [HttpPost]
    public async Task MoveAttribute([FromBody] MoveAttributeModel model)
    {
        await _manager.MoveAttribute(model.AttributeId, model.DepartmentId);
    }

    [HttpGet]
    public async Task<SearchResult<Product>> SearchProducts(string? searchKey, int pageNumber, int pageCount, long? qcStatusId)
    {
        return await _manager.SearchProducts(searchKey, pageNumber, pageCount, qcStatusId);
    }

    [HttpGet]
    public async Task<List<ItemCodeProducts>> ListProductsItem(DateTime? fromdate, DateTime? todate, string status, string? deptcode, string? division)
    {
        return await _manager.ListProductsItem(fromdate, todate, status, deptcode, division);
    }

    [HttpGet]
    public async Task<FileContentResult> DownloadProducts()
    {
        var file = await _manager.DownloadProducts();
        return this.File(System.IO.File.ReadAllBytes(file),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Products.xlsx".ToUniqueFileNameUrl());
    }

    [HttpPost]
    public async Task<FileContentResult> ProductExportExcel([FromBody] AttributesExportModel attributesExportModel)
    {
        var filePath = await _manager.ProductExportExcel(attributesExportModel.FromDate, attributesExportModel.ToDate, attributesExportModel.Season, attributesExportModel.DeptCode, attributesExportModel.ClassCode, attributesExportModel.SubClassCode, attributesExportModel.RoleIds);
        byte[] fileContents = System.IO.File.ReadAllBytes(filePath);
        return this.File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "UID Attributes Insights.xlsx");
    }

    [HttpPost]
    public async Task<FileContentResult> AttributesExcel([FromBody] AttributesExportModel attributesExportModel)
    {
        var file = await _manager.AttributesExcel(attributesExportModel.FromDate, attributesExportModel.ToDate, attributesExportModel.Season,  attributesExportModel.DeptCode, attributesExportModel.ClassCode, attributesExportModel.SubClassCode, attributesExportModel.RoleIds);
        return this.File(System.IO.File.ReadAllBytes(file),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "All Attributes Insights.xlsx");
    }

    [HttpPost]
    public async Task<FileContentResult> AllTemplateExcel([FromBody] AttributesExportModel attributesExportModel)
    {
        var filePath = await _manager.AllTemplateExcel(attributesExportModel.FromDate, attributesExportModel.ToDate, attributesExportModel.Season, attributesExportModel.DeptCode, attributesExportModel.ClassCode, attributesExportModel.SubClassCode, attributesExportModel.Templatetype, attributesExportModel.RoleIds);
        byte[] fileContents = System.IO.File.ReadAllBytes(filePath);

        string fileName = attributesExportModel.Templatetype switch
        {
            "10" => "Packaging Attributes Insights.xlsx",
            "8" => "QC Attributes Insights.xlsx",
            "6,12" => "T1 Attributes Insights.xlsx",
            "4" => "T2 Attributes Insights.xlsx",
            _ => "Template_Excel.xlsx"
        };

        return this.File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet]
    public async Task<List<dynamic>> SearchUidProducts(UidExportModel uidExport)
    {
        return await _manager.SearchUidProducts(uidExport);
    }

    [HttpPut]
    public async Task UpdateUidIsactive(string uid, bool isactive)
    {
        await _manager.UpdateUidIsactive(uid, isactive);
    }

    [HttpGet]
    public async Task<List<MerchandiseHierarchy>> GetHierarchicalMasters()
    {
        return await _manager.GetHierarchicalMasters();
    }

    [HttpGet]
    public async Task<List<ProductdeptMaster>> GetProductdeptMasters()
    {
        return await _manager.GetProductdeptMasters();
    }

    [HttpGet]
    public async Task<List<PriceGrouping>> GetPriceGrouping()
    {
        return await _manager.GetPriceGrouping();
    }

    [HttpGet]
    public async Task<List<ProductPurpose>> GetProductPurpose()
    {
        return await _manager.GetProductPurpose();
    }

    [HttpGet]
    public async Task<List<RmsStaging>> GetRms()
    {
        return await _manager.GetRms();
    }

    [HttpGet]

    public async Task<List<dynamic>> TotalCountViewColumnData()
    {
        return await _manager.TotalCountViewColumnData();
    }

    [HttpGet]

    public async Task<List<dynamic>> TotalCountJsonFormatData()
    {
        return await _manager.TotalCountJsonFormatData();
    }

    [HttpGet]
    public async Task<List<dynamic>> GetTotalCounts()
    {
        var viewColumnDataCount = await _manager.TotalCountViewColumnData();
        var jsonFormatDataCount = await _manager.TotalCountJsonFormatData();

        return new List<dynamic>
 {
     new
     {
         ViewColumnDataCount = viewColumnDataCount.FirstOrDefault(),
         JsonFormatDataCount = jsonFormatDataCount.FirstOrDefault()
     }
 };
    }

    [HttpGet]
    public async Task<IActionResult> DownloadImage(string imageUrl)
    {
        var fileContents = await _manager.DownloadImage(imageUrl);
        var fileName = await _manager.GetImageName(imageUrl);
        string contentType = ".jpg";
        if (fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
        {
            contentType = "image/jpg";
        }
        else if (fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            contentType = "image/png";
        }
        else if (fileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            contentType = "image/jpeg";
        }

        var fileBytes = System.IO.File.ReadAllBytes(fileContents);
        var fileContentResult = new FileContentResult(fileBytes, contentType)
        {
            FileDownloadName = fileName,
        };
        return fileContentResult;
    }

    [HttpPost]
    public async Task<IActionResult> AttributesExportExcel(bool? isActive, string? divisionId)
    {
        string format = "xlsx";
        var fileBytes = await _manager.AttributesExportExcel(isActive, divisionId);
        return File(fileBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Attributes.{format}");
    }

    [HttpDelete]
    public async Task DeleteAttribute(IEnumerable<long> ids)
    {
        await _manager.DeleteAttribute(ids);
    }

    [HttpGet]

    public async Task<List<dynamic>> UidColumndataCheck(string uid)
    {
        return await _manager.UidColumndataCheck(uid);
    }

    [HttpGet]

    public async Task<List<ProductDExcel>> UidComparison(string uid)
    {
        return await _manager.UidComparison(uid);
    }
}