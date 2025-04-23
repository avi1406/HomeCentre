using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class PlaningExcel
    {
        public long Id { get; set; }

        public long ItemCode { get; set; }

        public long EANCode { get; set; }

        public string? GRNDate { get; set; }

        public string? Division { get; set; }

        public string? GRNLocation { get; set; }

        public string? TransferOrder { get; set; }

        public string? Status { get; set; }

        public string? PhotoShootStatus { get; set; }

        public string? StockReturnToQc { get; set; }

        public DateTime? CamQcL1SubmitDate { get; set; }

        public DateTime? SubmitDate { get; set; }

        public string? GrnDate { get; set; }

        public long? GrnLocation { get; set; }

        public long? From_Loc { get; set; }

        public long? To_Loc { get; set; }

        public string? TO_Date { get; set; }

        public string? Studio { get; set; }

        public string? ProductToStatus { get; set; }

        public string? CamQcStatus { get; set; }
    }
}
