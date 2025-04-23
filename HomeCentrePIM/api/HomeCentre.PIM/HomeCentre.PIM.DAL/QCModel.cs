using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class QCModel
    {
        public long ItemCode { get; set; }

        public long? EanCode { get; set; }

        public string ImageType { get; set; }

        public DateTime ReceivedDate { get; set; }

        public string HandOverDate { get; set; }

        public string? HandoverStatus { get; set; }

        public string? CamQCStatus { get; set; }

        public string SampleStatus { get; set; }

        public long DivisionId { get; set; }

        public string Camqcl1rejection { get; set; }

        public DateTime StatusDate { get; set; }

        public string Samplestatusrejection { get; set; }

        public string ImageUrl { get; set; }

        public DateTime? ProductSubmitDate { get; set; }
    }
}
