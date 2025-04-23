using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class RejectedItemcodeExportModel
    {
        public long ItemCode { get; set; }

        public long EANCode { get; set; }

        public string SampleType { get; set; }

        public string Division { get; set; }

        public string ReceivedDate { get; set; }

        public string StockReturnToQC { get; set; }

        public string PhotoShootStatus { get; set; }

        public string RejectedBy { get; set; }

        public string RejectedReason { get; set; }

        public string RejectedCount { get; set; }

        public string? SampleStatus { get; set; }

        public string? Rejecteditemsubmit { get; set; }

        public DateTime? ProductSubmitDate { get; set; }

        public DateTime? RejectedItemSubmitDate { get; set; }

        public string? RejectedDate { get; set; }

        public string? Rolename { get; set; }

        public string? Status { get; set; }
    }
}
