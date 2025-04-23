using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class ProductImageExportModel
    {
        public long ItemCode { get; set; }

        public long EanCode { get; set; }

        public string Type { get; set; }

        public string Division { get; set; }

        public string Image { get; set; }

        public string ReceivedDate { get; set; }

        public string HandOverDate { get; set; }

        public string HandOverToBusinessStatus { get; set; }

        public string PhotoShootStatus { get; set; }

        public string SampleReceivedByCamQc_QC { get; set; }

        public string StockReturnToQC { get; set; }
    }
}
