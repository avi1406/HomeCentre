using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeCentre.PIM.DAL;
using Microsoft.AspNetCore.Mvc;

namespace HomeCentre.PIM.BAL.Contracts
{
    public interface ICamQcManager
    {
        Task<List<CamQc>> GetCamQcdetails(DateTime? fromDate, DateTime? toDate, string imageid);

        Task<List<CamQc>> GetCamQcL2details(DateTime? fromDate, DateTime? toDate, string imageid);

        Task<List<CamQc>> SearchCamQcdetails(DateTime? fromDate, DateTime? toDate);

        Task<List<UIDStatus>> GetItemStatus();

        Task<string> CAMQCL1ExportExcel(ImageTypeDropdown imageTypeDropdown);

        Task<string> CAMQCL2ExportExcel(ImageTypeDropdown imageTypeDropdown);

        Task UpdateCAMQCRecords(string? city, string? name, string? priority, long itemcode);

        Task<List<CamQc>> GetStudios();

        Task Updatetstatus();

        Task UpdateCAMQCQualityCheck(long itemcode, string? check, string? username);

        Task UpdateCAMQCL1QualityCheck(long itemcode, string? check, string? username);

        Task CAMQCActionState(long itemcode, string? action, string? rcomment, string? username);

        Task CAMQCL2Submit(long[] itemcode, string? submit, string? username);

        Task<List<HandOverPhotoshootStatus>> GetPhotoShootStatus();

        Task<List<HandOverPhotoshootStatus>> GetHandOverStatus();

        Task<List<ItemCodeNumber>> GetItemCodeNumbers();

        Task<List<ProcessStatus>> CAMQCProcessStatus();

        Task UpdateAssigned(string[] cities, long[] itemcode, DateTime[] dates, string? username);

        Task UpdateCAMQCAction(long itemcode, string? status, string? rejectioncomment, string? username);

        Task<List<CAMQcL2ExportExcel>> GetCAMQCL2Records(ProductImageModel productImageModel);

        Task<List<ReceivedAndHandoverStatus>> GetRejectedItemcode();

        Task RejectedItemcode(long itemcode, string rejectedby, string rejctedreason, string? username);

        Task RejectedItemcodeCAML2(long itemcode, string rejectedby, string rejctedreason, string? userrole);
    }
}