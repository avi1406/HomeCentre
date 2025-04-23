using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class CamQc
    { 
        public long ItemCode { get; set; }

        public long? EANCode { get; set; }

        public string ImageType { get; set; }

        public string ImageUrl { get; set; }

        public string? L2Action { get; set; }

        public string? L1Action { get; set; }

        public string ReceivedDate { get; set; }

        public string HandOverDate { get; set; }

        public string? HandoverStatus { get; set; }

        public string? PhotoShootStatus { get; set; }

        public string Division { get; set; }

        public long DivisionId { get; set; }

        public string AllocationDate { get; set; }

        public string Name { get; set; }

        public string City { get; set; }

        public string Capacity { get; set; } 

        public string Priority { get; set; }

        public string? Productreceived { get; set; }

        public string? Camqcl1qualitycheck { get; set; }

        public string? L2qualitycheck { get; set; }

        public string? Pictureid { get; set; }

        public string? Rejectioncomments { get; set; }

        public string? StockReturnToQC { get; set; }

        public string? Camqcl2submit { get; set; }

        public string? Comments { get; set; }

        public string? Camqcstatus { get; set; }

        public DateTime? StatusDate { get; set; }

        public DateTime? SubmitDate { get; set; }
    }
}
