using System.Collections.Generic;
using System.Threading.Tasks;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Implementations;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.CommonWeb;
using HomeCentre.PIM.DAL;
using Microsoft.AspNetCore.Mvc;

namespace HomeCentre.PIM.Api.Controllers.Api
{
    [Route("/api/[controller]/[action]")]
    public class PlanningController : BaseApiController
    {
        private readonly IPlanningManager _planningManager;

        public PlanningController(IPlanningManager planningManager)
        {
            _planningManager = planningManager;
        }

        [HttpGet]
        public async Task<List<Planning>> GetPlanningDetails()
        {
            return await _planningManager.GetPlanningDetails();
        }

        [HttpPost]
        public async Task<FileResult> PlanningExportExcel([FromBody] ProductImageModel productImageModel)
        {
            var fileStream = await _planningManager.PlanningExportExcel(productImageModel);
            return File(fileStream, new FileExportHelper().GetMimeType("*.xlsx"), $"PlanningExcel.xlsx");
        }
    }
}
