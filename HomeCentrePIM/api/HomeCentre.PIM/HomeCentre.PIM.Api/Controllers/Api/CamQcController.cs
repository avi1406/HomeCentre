using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.ExtendedProperties;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Implementations;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.CommonWeb;
using HomeCentre.PIM.DAL;
using Microsoft.AspNetCore.Mvc;
using NPOI.OpenXmlFormats.Wordprocessing;

namespace HomeCentre.PIM.Api.Controllers.Api
{
    [Route("/api/[controller]/[action]")]
    public class CamQcController : BaseApiController
    {
        private readonly ICamQcManager _iCamQcManager;

        public CamQcController(ICamQcManager iCamQcManager)
        {
            _iCamQcManager = iCamQcManager;
        }

        [HttpGet]
        public async Task<List<CamQc>> GetCamQcdetails([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] string imageid)
        {
            return await _iCamQcManager.GetCamQcdetails(fromDate, toDate, imageid);
        }

        [HttpGet]
        public async Task<List<CamQc>> GetCamQcL2details([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] string imageid)
        {
            return await _iCamQcManager.GetCamQcL2details(fromDate, toDate, imageid);
        }

        [HttpGet]
        public async Task<List<CamQc>> SearchCamQcdetails([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            return await _iCamQcManager.SearchCamQcdetails(fromDate, toDate);
        }

        [HttpGet]
        public async Task<List<UIDStatus>> GetItemStatus()
        {
            return await _iCamQcManager.GetItemStatus();
        }

        [HttpPost]
        public async Task<FileContentResult> CAMQCL1ExportExcel([FromBody] ImageTypeDropdown imageTypeDropdown)
        {
            var filePath = await _iCamQcManager.CAMQCL1ExportExcel(imageTypeDropdown);
            byte[] fileContents = System.IO.File.ReadAllBytes(filePath);
            return this.File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"CAMQCL1Excel.xlsx".ToUniqueFileNameUrl());
        }

        [HttpPost]
        public async Task<FileContentResult> CAMQCL2ExportExcel([FromBody] ImageTypeDropdown imageTypeDropdown)
        {
            var filePath = await _iCamQcManager.CAMQCL2ExportExcel(imageTypeDropdown);
            byte[] fileContents = System.IO.File.ReadAllBytes(filePath);
            return this.File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"CAMQCL2Excel.xlsx".ToUniqueFileNameUrl());
        }

        [HttpPut]
        public async Task UpdateCAMQCRecords(string? city, string? name, string? priority, long itemcode)
        {
            await _iCamQcManager.UpdateCAMQCRecords(city, name, priority, itemcode);
        }

        [HttpGet]
        public async Task<List<CamQc>> GetStudios()
        {
            return await _iCamQcManager.GetStudios();
        }

        [HttpGet]
        public async Task Updatetstaus()
        {
            await _iCamQcManager.Updatetstatus();
        }

        [HttpPut]
        public async Task UpdateCAMQCL1QualityCheck(long itemcode, string? check, string? username)
        {
            await _iCamQcManager.UpdateCAMQCL1QualityCheck(itemcode, check, username);
        }

        [HttpPut]
        public async Task UpdateCAMQCQualityCheck(long itemcode, string? check, string? username)
        {
            await _iCamQcManager.UpdateCAMQCQualityCheck(itemcode, check, username);
        }

        [HttpPut]
        public async Task CAMQCActionState(long itemcode, string? camaction, string? rcomment, string? username)
        {
            await _iCamQcManager.CAMQCActionState(itemcode, camaction, rcomment, username);
        }

        [HttpPut]
        public async Task CAQCL2Submit(long[] itemcode, string? submit, string? username)
        {
            await _iCamQcManager.CAMQCL2Submit(itemcode, submit, username);
        }

        [HttpGet]
        public async Task<List<HandOverPhotoshootStatus>> GetPhotoShootStatus()
        {
            return await _iCamQcManager.GetPhotoShootStatus();
        }

        [HttpGet]
        public async Task<List<HandOverPhotoshootStatus>> GetHandOverStatus()
        {
            return await _iCamQcManager.GetHandOverStatus();
        }

        [HttpGet]
        public async Task<List<ItemCodeNumber>> GetItemCodeNumbers()
        {
            return await _iCamQcManager.GetItemCodeNumbers();
        }

        [HttpGet]
        public async Task<List<ProcessStatus>> CAMQCProcessStatus()
        {
            return await _iCamQcManager.CAMQCProcessStatus();
        }

        [HttpPut]
        public async Task UpdateAssigned(string[] cities, long[] itemcode, DateTime[] dates, string? username)
        {
            await _iCamQcManager.UpdateAssigned(cities, itemcode, dates, username);
        }

        [HttpPut]
        public async Task UpdateCAMQCL1Action(long itemcode, string? status, string? rejectioncomment, string? username)
        {
            await _iCamQcManager.UpdateCAMQCAction(itemcode, status, rejectioncomment, username);
        }

        [HttpGet]

        public async Task<List<CAMQcL2ExportExcel>> GetCAMQCL2Records(ProductImageModel productImageModel)
        {
            return await _iCamQcManager.GetCAMQCL2Records(productImageModel);
        }

        [HttpGet]
        public async Task<List<ReceivedAndHandoverStatus>> GetRejectedItemcode()
        {
            return await _iCamQcManager.GetRejectedItemcode();
        }

        [HttpPut]
        public async Task RejectedItemcode(long itemcode, string rejectedby, string rejctedreason, string? username)
        {
            await _iCamQcManager.RejectedItemcode(itemcode, rejectedby, rejctedreason, username);
        }

        [HttpPut]
        public async Task RejectedItemcodeCAML2(long itemcode, string rejectedby, string rejctedreason, string? userrole)
        {
            await _iCamQcManager.RejectedItemcodeCAML2(itemcode, rejectedby, rejctedreason, userrole);
        }
    }
}