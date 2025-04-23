using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class ContentExportModel
    {
        public long Itemcode { get; set; }

        public long EANCode { get; set; }

        public string Image { get; set; }

        public string Division { get; set; }

        public string? ReceivedDate { get; set; }

        public string Priority { get; set; }

        public string StudioLocation { get; set; }

        public string StudioName { get; set; }

        public string PhotoShootStatus { get; set; }

        public string CatalogueShotDate { get; set; }

        public string RangeShotDate { get; set; }

        public string IndividualShotDate { get; set; }

        public string WebsiteLiveDate { get; set; }

        public string? StockReturnToQC { get; set; }

        public string? CamQcL2SubmitDate { get; set; }
    }
}
