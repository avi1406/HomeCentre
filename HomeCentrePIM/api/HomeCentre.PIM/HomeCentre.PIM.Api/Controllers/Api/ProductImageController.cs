using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.ExtendedProperties;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Implementations;
using HomeCentre.PIM.BAL.Model.Uid;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.CommonWeb;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.Master;
using HomeCentre.PIM.WorkFlow.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Org.BouncyCastle.Crypto;

namespace HomeCentre.PIM.Api.Controllers.Api
{
    [Route("/api/[controller]/[action]")]
    public class ProductImageController : BaseApiController
    {
        private readonly IProductImageManager _productImageManager;

        public ProductImageController(IProductImageManager productImageManager)
        {
            _productImageManager = productImageManager;
        }

        [HttpPost]
        public async Task AddOrUpdateProductImages([FromBody] IEnumerable<ProductImage> productImages)
        {
             await _productImageManager.AddOrUpdateProductImages(productImages);
        }

        [HttpGet]
        public async Task<List<ReceivedAndHandoverStatus>> GetReceivedDateRange(DateTime? fromDate, DateTime? toDate)
        {
            return await _productImageManager.GetReceivedDate(fromDate, toDate);
        }

        [HttpGet]

        public async Task<List<ReceivedAndHandoverStatus>> GetReceivedProductsRecords()
        {
            return await _productImageManager.GetRecords();
        }

        [HttpPost]
        public async Task<FileResult> QCExportExcel([FromBody] ReceivedProductsModel receivedProductsModel)
        {
            var fileStream = await _productImageManager.QCExportExcel(receivedProductsModel);
            return File(fileStream, new FileExportHelper().GetMimeType("*.xlsx"), $"QCExcel.xlsx");
        }

        [HttpGet]
        public async Task<List<ReceivedAndHandoverStatus>> GetHandOverDateRange(DateTime? fromDate, DateTime? toDate)
        {
            return await _productImageManager.GetHandOverDateRange(fromDate, toDate);
        }

        [HttpGet]
        public async Task<List<ReceivedAndHandoverStatus>> GetProductImage(string? status)
        {
            return await _productImageManager.GetProductImage(status);
        }

        [HttpGet]
        public async Task<List<ReceivedAndHandoverStatus>> GetAllReceivedProductFilters(string? status, string? pSstatus, string? camPstatus, string? camRstatus, DateTime? receivedfromDate, DateTime? receivedtoDate, DateTime? handOverfromDate, DateTime? handOvertoDate, string? division)
        {
            return await _productImageManager.GetAllFilters(status, pSstatus, camPstatus, camRstatus, receivedfromDate, receivedtoDate, handOverfromDate, handOvertoDate, division);
        }

        [HttpGet]

        public async Task<List<ReceivedAndHandoverStatus>> GetrejectedFilters(string? status, DateTime? receivedfromDate, DateTime? receivedtoDate, DateTime? handOverfromDate, DateTime? handOvertoDate)
        {
            return await _productImageManager.GetrejectedFilters(status, receivedfromDate, receivedtoDate, handOverfromDate, handOvertoDate);
        }

        [HttpGet]

        public async Task<List<ReceivedAndHandoverStatus>> GetAllReceivedProductQCFilters(string? status, DateTime? receivedfromDate, DateTime? receivedtoDate, DateTime? handOverfromDate, DateTime? handOvertoDate, string? division)
        {
            return await _productImageManager.GetAllQCFilters(status, receivedfromDate, receivedtoDate, handOverfromDate, handOvertoDate, division);
        }

        [HttpPost]
        public async Task<FileResult> RejectedItemcodeExportExcel([FromBody] ProductImageModel productImageModel)
        {
            var fileStream = await _productImageManager.RejectedItemcodeExportExcel(productImageModel);
            return File(fileStream, new FileExportHelper().GetMimeType("*.xlsx"), $"ProductImageExcel.xlsx");
        }

        [HttpPost]
        public async Task<FileResult> GetAllFitersExportExcel([FromBody] ReceivedProductsModel receivedProductsModel)
        {
            var fileStream = await _productImageManager.GetAllFitersExportExcel(receivedProductsModel);
            return File(fileStream, new FileExportHelper().GetMimeType("*.xlsx"), $"ReceivedProductsExcel.xlsx");
        }

        [HttpPost]
        public async Task<FileContentResult> GetAllFitersPSExportExcel([FromBody] ReceivedProductsModel receivedProductsModel)
        {
            var filePath = await _productImageManager.GetAllFitersPSExportExcel(receivedProductsModel);
            return this.File(System.IO.File.ReadAllBytes(filePath),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SKU Records.xlsx");
        }

        [HttpPost]
        public async Task<FileResult> ProductImageExportExcel([FromBody] ProductImageModel productImageModel)
        {
            var fileStream = await _productImageManager.ProductImageExportExcel(productImageModel);
            return File(fileStream, new FileExportHelper().GetMimeType("*.xlsx"), $"ProductImageExcel.xlsx");
        }

        [HttpGet]

        public async Task<List<MtrProductImageType>> GetProductImageType()
        {
            return await _productImageManager.GetProductImageType();
        }

        [HttpGet]

        public async Task<List<PhotoShoot>> GetPhotoShoot([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] string imageid)
        {
            return await _productImageManager.GetPhotoShoot(fromDate, toDate, imageid);
        }

        [HttpPut]
        public async Task UpdateReceivedproducts(long id, long newid, string? imageType, string? imageUrl, DateTime? receivedDate, string? userdivision)
        {
            await _productImageManager.UpdateReceivedproducts(id, newid, imageType, imageUrl, receivedDate, userdivision);
        }

        [HttpPut]
        public async Task AddProductPicture(string? imageUrl, string? imageName, long? itemcode, string? username)
        {
            await _productImageManager.AddProductPicture(imageUrl, imageName, itemcode, username);
        }

        [HttpPut]
        public async Task RemovePicture(long? itemcode, string? imageIds, string? username)
        {
            await _productImageManager.RemovePicture(itemcode, imageIds, username);
        }

        [HttpGet]
        public async Task<List<PhotoShootPicture>> GetPictureCamQcL1()
        {
            return await _productImageManager.GetPictureCamQcL1();
        }

        [HttpPut]
        public async Task UpdateProductImage(long itemcode, string? range, long id, string? comment, string? removeData, string? username)
        {
            await _productImageManager.UpdateProductImage(itemcode, range, id, comment, removeData, username);
        }

        [HttpPut]
        public async Task UpdatePhotoshotstate(long itemcode, string? productreceived, string? rcomment, string? username)
        {
            await _productImageManager.UpdatePhotoshotstate(itemcode, productreceived, rcomment, username);
        }

        [HttpPut]
        public async Task UpdateQCAction(long itemcode, string? status, string? samplestatusrejection, string? username)
        {
            await _productImageManager.UpdateQCAction(itemcode, status, samplestatusrejection, username);
        }

        [HttpDelete]

        public async Task RemoveReceivedProductRecords([FromBody] long[] ids)
        {
            await _productImageManager.RemoveReceivedProductRecords(ids);
        }

        [HttpPut]

        public async Task UpdateQCRecords(long[] itemcode, string status, string? username)
        {
              await _productImageManager.UpdateQCRecords(itemcode, status, username);
        }

        [HttpPut]

        public async Task UpdateRejectedRecords(long[] itemcode, string? username)
        {
            await _productImageManager.UpdateRejectedRecords(itemcode, username);
        }

        [HttpGet]

        public async Task<List<QCModel>> GetQcRecords()
        {
            return await _productImageManager.GetQcRecords();
        }

        [HttpPut]

        public async Task UpdatePhotoshootProduct()
        {
            await _productImageManager.UpdatePhotoshootProduct();
        }
    }
}
