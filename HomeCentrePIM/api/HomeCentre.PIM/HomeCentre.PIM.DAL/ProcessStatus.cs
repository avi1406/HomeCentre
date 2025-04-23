using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class ProcessStatus
    {
        public long ItemCode { get; set; }

        public string? RangeShot { get; set; }

        public string? IndividualStyle { get; set; }

        public string? CatologShot { get; set; }

        public string? Camqcl1qualitycheck { get; set; }

        public string? L2qualitycheck { get; set; }

        public string PictureId { get; set; }

        public string? StockReturnToQC { get; set; }
    }
}
