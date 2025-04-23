using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class Planning : Entity
    {
        public long ItemCode { get; set; }

        public long? EanCode { get; set; }

        public string? PhotoShootStatus { get; set; }

        public string Division { get; set; }

        public long DivisionId { get; set; }

        public string? PhotoshootComment { get; set; }

        public string? StockReturnToQc { get; set; }

        public DateTime? CamQcL1SubmitDate { get; set; }

        public DateTime? SubmitDate { get; set; }

        public string? GrnDate { get; set; }

        public string? GrnLocation { get; set; }

        public string? From_Loc { get; set; }

        public string? To_Loc { get; set; }

        public string? TO_Date { get; set; }

        public string? Studio { get; set; }

        public string? ProductToStatus { get; set; }

        public string? CamQcStatus { get; set; }
    }
}
