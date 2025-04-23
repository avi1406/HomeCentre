using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeCentre.PIM.BAL.Model.Uid;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.Master;
using Microsoft.AspNetCore.Mvc;

namespace HomeCentre.PIM.BAL.Contracts
{
    public interface IProductImageManager
    {
        Task AddOrUpdateProductImages(IEnumerable<ProductImage> productImages);

        Task<List<ReceivedAndHandoverStatus>> GetReceivedDate(DateTime? fromDate, DateTime? toDate);

        Task<List<ReceivedAndHandoverStatus>> GetRecords();

        Task<List<ReceivedAndHandoverStatus>> GetHandOverDateRange(DateTime? fromDate, DateTime? toDate);

        Task<List<ReceivedAndHandoverStatus>> GetProductImage(string? status);

        Task<List<ReceivedAndHandoverStatus>> GetrejectedFilters(string? status, DateTime? receivedfromDate, DateTime? receivedtoDate, DateTime? handOverfromDate, DateTime? handOvertoDate);

        Task<List<ReceivedAndHandoverStatus>> GetAllFilters(string? status, string? pSstatus, string? camPstatus, string? camRstatus, DateTime? receivedfromDate, DateTime? receivedtoDate, DateTime? handOverfromDate, DateTime? handOvertoDate, string? division);

        Task<List<ReceivedAndHandoverStatus>> GetAllQCFilters(string? status, DateTime? receivedfromDate, DateTime? receivedtoDate, DateTime? handOverfromDate, DateTime? handOvertoDate, string? division);

        Task<Stream> ProductImageExportExcel(ProductImageModel productImageModel);

        Task<byte[]> QCExportExcel(ReceivedProductsModel receivedProductsModel);

        Task<byte[]> RejectedItemcodeExportExcel(ProductImageModel productImageModel);

        Task<byte[]> GetAllFitersExportExcel(ReceivedProductsModel receivedProductsModel);

        Task<string> GetAllFitersPSExportExcel(ReceivedProductsModel receivedProductsModel);

        Task<List<MtrProductImageType>> GetProductImageType();

        Task<List<PhotoShoot>> GetPhotoShoot([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] string imageid);

        Task UpdateReceivedproducts(long id, long newid, string? imageType, string? imageUrl, DateTime? receivedDate, string? userdivision);

        Task AddProductPicture(string? imageUrl, string? imageName, long? itemcode, string? username);

        Task RemovePicture(long? itemcode, string? imageIds, string? username);

        Task<List<PhotoShootPicture>> GetPictureCamQcL1();

        Task UpdateProductImage(long itemcode, string? range, long id, string? comment, string? removeData, string? username);

        Task UpdatePhotoshotstate(long itemcode, string? productreceived, string? rcomment, string? username);

        Task UpdateQCAction(long itemcode, string? status, string? samplestatusrejection, string? username);

        Task RemoveReceivedProductRecords(long[] ids);

        Task UpdateQCRecords(long[] itemcode, string status, string? username);

        Task UpdateRejectedRecords(long[] itemcode, string? username);

        Task<List<QCModel>> GetQcRecords();

        Task UpdatePhotoshootProduct();
    }
}
