using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class ReceivedAndHandoverStatus
    {
        public long Id { get; set; }

        public long ItemCode { get; set; }

        public long? EanCode { get; set; }

        public string ImageType { get; set; }

        public string ImageUrl { get; set; }

        public string ReceivedDate { get; set; }

        public string HandOverDate { get; set; }

        public string? HandoverStatus { get; set; }

        public string? PhotoShootStatus { get; set; }

        public string? Stockreturn { get; set; }

        public string? CamQCStatus { get; set; }

        public string? Comments { get; set; }

        public long DivisionId { get; set; }

        public string SampleStatus { get; set; }

        public string Camqcl1rejection { get; set; }

        public DateTime StatusDate { get; set; }

        public string Qcaction { get; set; }

        public string Rejecteditemsubmit { get; set; }

        public string Rejectedreason { get; set; }

        public string Rejectedby { get; set; }

        public string Rejectedcount { get; set; }

        public string StudioName { get; set; }

        public string StudioLocation { get; set; }

        public string StudioCapacity { get; set; }

        public string StudioAllocationDate { get; set; }

        public string PhotoshootAction { get; set; }

        public string Camqcl1qualitycheck { get; set; }

        public string Camqcl1action { get; set; }

        public string Productreceived { get; set; }

        public string Qualitycheck { get; set; }

        public string Camqcl2submit { get; set; }

        public string Rejecteddate { get; set; }

        public string PictureId { get; set; }

        public string? ProductStatus { get; set; }

        public DateTime? DateCreated { get; set; }

        public DateTime? ProductSubmitDate { get; set; }

        public DateTime? RejectedItemSubmitDate { get; set; }
    }
}