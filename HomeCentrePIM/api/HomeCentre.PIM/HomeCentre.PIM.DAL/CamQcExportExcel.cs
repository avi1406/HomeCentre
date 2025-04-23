using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class CamQcExportExcel
    {
        public long ItemCode { get; set; }

        public long EanCode { get; set; }

        public string ImageType { get; set; }

        public string PSImage { get; set; }

        public DateTime ReceivedDate { get; set; }

        public string? HandoverStatus { get; set; }

        public string? PhotoShootStatus { get; set; }

        public string Division { get; set; }

        public DateTime AllocationDate { get; set; }

        public string Priority { get; set; }
    }
}
