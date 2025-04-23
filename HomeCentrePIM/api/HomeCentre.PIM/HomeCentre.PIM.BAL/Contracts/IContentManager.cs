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
    public interface IContentManager
    {
        Task<List<ContentModel>> GetContentRecords([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] string imageid);

        Task UpdateContentstate(long itemcode, string? action, string? rcomment);

        Task<Stream> ContentExportExcel([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, string? imageType);
    }
}
