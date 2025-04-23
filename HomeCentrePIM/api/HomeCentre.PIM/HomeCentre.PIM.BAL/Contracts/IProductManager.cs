using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.ExtendedProperties;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.BAL.Model.Uid;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.Master;
using Microsoft.AspNetCore.Mvc;

namespace HomeCentre.PIM.BAL.Contracts;

public interface IProductManager
{
    Task AddOrUpdateAttribute(ProductAttribute attribute);

    Task<IEnumerable<ProductAttribute>> ListAttributes(long? divisionId, bool isActive);

    Task MoveAttribute(long attributeId, long departmentId);

    Task ActivateAttributes(long[] attributeIds);

    Task DeactivateAttributes(long[] attributeIds);

    Task<SearchResult<Product>> SearchProducts(string searchKey, int pageNumber, int pageCount, long? qcStatusId);

    Task<List<ItemCodeProducts>> ListProducts(DateTime? fromdate, DateTime? todate, string status, long deptcode);

    Task<List<ItemCodeProducts>> ListProductsItem(DateTime? fromdate, DateTime? todate, string status, string? deptcode, string? division);

    Task<List<ItemCodeProducts>> ListAllProducts(DateTime? fromdate, DateTime? todate, string status, long deptcode);

    Task<string> DownloadProducts();

    Task<string> AttributesExcel(DateTime? fromDate, DateTime? toDate, string? season, string? dept_code, string? class_code, string? subclass_code, long[]? roleIds);

    Task<string> AllTemplateExcel(DateTime? fromDate, DateTime? toDate, string? season, string? dept_code, string? class_code, string? subclass_code, string templateType, long[]? roleIds);

    Task<string> ProductExportExcel(DateTime? fromDate, DateTime? toDate, string? season, string? dept_code, string? class_code, string? subclass_code, long[]? roleIds);

    Task<List<dynamic>> SearchUidProducts(UidExportModel uidExport);

    Task UpdateUidIsactive(string uid, bool isactive);

    Task<List<string>> GetUids();

    Task<List<MerchandiseHierarchy>> GetHierarchicalMasters();

    Task<List<ProductdeptMaster>> GetProductdeptMasters();

    Task<List<RmsStaging>> GetRms();

    Task<List<ProductPurpose>> GetProductPurpose();

    Task<List<PriceGrouping>> GetPriceGrouping();

    Task<string> DownloadImage(string imageUrl);

    Task<string> GetImageName(string imageUrl);

    Task<List<ProductDExcel>> UidComparison(string uid);

    Task<List<dynamic>> UidColumndataCheck(string uid);

    Task<List<dynamic>> TotalCountJsonFormatData();

    Task<List<dynamic>> TotalCountViewColumnData();

    Task<byte[]> AttributesExportExcel(bool? isActive, string? divisionId);

    Task DeleteAttribute(IEnumerable<long> ids);
}