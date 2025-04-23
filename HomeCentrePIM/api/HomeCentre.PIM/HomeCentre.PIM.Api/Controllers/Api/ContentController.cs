using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Office2021.DocumentTasks;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Implementations;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.CommonWeb;
using HomeCentre.PIM.DAL;
using Microsoft.AspNetCore.Mvc;
using Task = System.Threading.Tasks.Task;

namespace HomeCentre.PIM.Api.Controllers.Api
{
    [Route("/api/[controller]/[action]")]
    public class ContentController : BaseApiController
    {
        private readonly IContentManager _contentManager;

        public ContentController(IContentManager contentManager)
        {
            _contentManager = contentManager;
        }

        [HttpGet]
        public async Task<List<ContentModel>> GetContentRecords([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] string imageid)
        {
            return await _contentManager.GetContentRecords(fromDate, toDate, imageid);
        }

        [HttpPost]
        public async Task<FileResult> ContentExportExcel([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, string? imageType)
        {
            var fileStream = await _contentManager.ContentExportExcel(fromDate, toDate, imageType);
            return File(fileStream, new FileExportHelper().GetMimeType("*.xlsx"), $"ContentExcel.xlsx");
        }

        [HttpPut]
        public async Task UpdateContentstate(long itemcode, string? contentaction, string? rcomment)
        {
            await _contentManager.UpdateContentstate(itemcode, contentaction, rcomment);
        }
    }
}
