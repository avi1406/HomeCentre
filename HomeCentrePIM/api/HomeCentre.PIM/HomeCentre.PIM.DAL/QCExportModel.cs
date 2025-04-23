using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class QCExportModel
    {
        public long ItemCode { get; set; }

        public long EANCode { get; set; }

        public string SampleType { get; set; }

        public string ReceivedDate { get; set; }

        // public string HandOverDate { get; set; }

        // public string? HandOverToBusinessStatus { get; set; }

        // public string? SampleReceivedByCamQC { get; set; }

        //// public string SampleStatus { get; set; }

        public string Division { get; set; }

        public string ImageURL { get; set; }

        public DateTime? ProductSubmitDate { get; set; }
    }
}
