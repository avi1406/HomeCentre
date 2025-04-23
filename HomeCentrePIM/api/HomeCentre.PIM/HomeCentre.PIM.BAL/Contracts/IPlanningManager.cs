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
    public interface IPlanningManager
    {
        Task<List<Planning>> GetPlanningDetails();

        Task<byte[]> PlanningExportExcel([FromBody] ProductImageModel productImageModel);
    }
}
