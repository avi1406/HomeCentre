using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.ExtendedProperties;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Model;
using HomeCentre.PIM.CommonWeb;
using HomeCentre.PIM.DAL;
using HomeCentre.PIM.DAL.Master;
using Microsoft.AspNetCore.Mvc;

namespace HomeCentre.PIM.Api.Controllers.Api
{
    [Route("/api/[controller]/[action]")]
    public class StudioController : BaseApiController
    {
        private readonly IStudioManager _studioManager;

        public StudioController(IStudioManager studioManager)
        {
            _studioManager = studioManager;
        }

        [HttpPost]
        public async Task<Studio> AddOrUpdateStudio([FromBody] Studio studio)
        {
            return await _studioManager.AddOrUpdateStudio(studio);
        }

        [HttpGet]
        public async Task<SearchResult<Studio>> SearchStudios(string searchKey, int pageNumber, int pageSize,
 bool isActive)
        {
            return await _studioManager.SearchStudios(searchKey, pageNumber, pageSize, isActive);
        }

        [HttpGet]
        public async Task<List<StudioModel>> GetStudiosDetails(string? isActive)
        {
            return await _studioManager.GetStudiosDetails(isActive);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadStudios(bool? isActive)
        {
            string format = "xlsx";
            var file = await _studioManager.DownloadStudio(isActive);
            return File(file,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Studio.{format}");
        }

        [HttpPost]
        public async Task UploadstudioCapacity(string blobUrl)
        {
            await _studioManager.Uploadstudio(blobUrl);
        }

        [HttpPut]
        public async Task DeactivateStudios([FromBody] long[] ids)
        {
            await _studioManager.DeactivateStudios(ids);
        }

        [HttpPut]
        public async Task ActivateStudios([FromBody] long[] ids)
        {
            await _studioManager.ActivateStudios(ids);
        }

        [HttpDelete]
        public async Task RemoveStudios([FromBody] long[] ids)
        {
            await _studioManager.RemoveStudios(ids);
        }

        [HttpGet]
        public async Task<List<Studio>> Getstudio()
        {
            return await _studioManager.GetStudio();
        }

        [HttpDelete]
        public async Task DeleteStudio(IEnumerable<long> ids)
        {
            await _studioManager.DeleteStudio(ids);
        }

        [HttpPut]

        public async Task UpdateIsactive()
        {
            await _studioManager.UpdateIsactive();
        }
    }
}
