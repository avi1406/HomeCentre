using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Implementations;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.CommonWeb;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.Master;
using Microsoft.AspNetCore.Mvc;

namespace HomeCentre.PIM.Api.Controllers.Api
{
    [Route("/api/[controller]/[action]")]
    public class PhotoShootController : BaseApiController
    {
        private readonly IPhotoShootManager _photoShootManager;

        public PhotoShootController(IPhotoShootManager photoShootManager)
        {
            _photoShootManager = photoShootManager;
        }

        [HttpGet]
        public async Task<List<MtrProductImageType>> Photoshootstatus()
        {
            return await _photoShootManager.Photoshootstatus();
        }

        [HttpPut]

        public async Task UpdateInwardDate(long itemcode, DateTime? inwardDate)
        {
            await _photoShootManager.UpdateInward(itemcode, inwardDate);
        }

        [HttpPut]

        public async Task UpdateStockReturnDate(long itemcode, DateTime? stockreturndate)
        {
            await _photoShootManager.UpdateStockReturnDate(itemcode, stockreturndate);
        }

        [HttpPost]
        public async Task<FileResult> PhotoShootExportExcel([FromBody] ImageTypeDropdown imageTypeDropdown)
        {
            var fileStream = await _photoShootManager.PhotoShootExportExcel(imageTypeDropdown);
            return File(fileStream, new FileExportHelper().GetMimeType("*.xlsx"), $"PhotoShootExcel.xlsx");
        }
    }
}
