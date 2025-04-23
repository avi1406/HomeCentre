using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class GetAllFiltersPSModal
    {
        public long ItemCode { get; set; }

        public long EANCode { get; set; }

        public string Division { get; set; }

        public string SampleType { get; set; }

        public string ReceivedDate { get; set; }

        public string QCApprovedDate { get; set; }

        public string ImageURL { get; set; }

        public string StudioName { get; set; }

        public string City { get; set; }

        public string TotalCapacity { get; set; }

        public string StudioAllocationDate { get; set; }

        public string ProductStatus { get; set; }

        public string QCStatus { get; set; }

        public string CAMQCL1ACKStatus { get; set; }

        public string CAMQCL1Status { get; set; }

        public string CAMQCL1SubmissionStatus { get; set; }

        public string PhotoshootActionStatus { get; set; }

        public string? PhotoShootStatus { get; set; }

        public string CAMQCL2Status { get; set; }

        public string CAMQCL2SubmissionStatus { get; set; }

        public string StockReturnToQC { get; set; }

        public string HandOverDate { get; set; }

        public string? HandoverToBusinessStatus { get; set; }

        public string? SampleReceivedByCamQC { get; set; }

        public string RejectedCount { get; set; }

        public string QCAction { get; set; }

        public string SampleStatus { get; set; }

        public string RejectedItemSubmit { get; set; }

        public string CAMQCL1Action { get; set; }

        public string CAMQCL1QualityCheck { get; set; }

        public string ProductReceived { get; set; }

        public string CamQCStatus { get; set; }

        public string QualityCheck { get; set; }

        public string CamQcL2Submit { get; set; }

        public DateTime? DateCreated { get; set; }

        public DateTime? ProductSubmitDate { get; set; }

        public DateTime? RejectedItemSubmitDate { get; set; }

        public string? WebSiteLiveStatus { get; set; }

        public string? WebsiteLiveDate { get; set; }
    }
}
