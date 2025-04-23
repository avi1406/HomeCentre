using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class CAMQcL2ExportExcel
    {
        public long ItemCode { get; set; }

        public long EANCode { get; set; }

        public string Division { get; set; }

        public string ReceivedDate { get; set; }

        public string StudioLocation { get; set; }

        public string StudioName { get; set; }

        public string StudioAvailableCapacity { get; set; }

        public string? HandoverStatus { get; set; }

        public string AllocationDate { get; set; }

        public string ImageType { get; set; }

        public string Priority { get; set; }

        public string PhotoShootStatus { get; set; }

        public string PSImage { get; set; } 

        public string StockReturnToQC { get; set; }

        public DateTime? SubmitDate { get; set; }
    }
}
