using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class PhotoShootExportModel
    {
        public long Itemcode { get; set; }

        public long EANCode { get; set; }

        public string ImageURL { get; set; }

        public string ReceivedDate { get; set; }

        public string Division { get; set; }

        public string SampleType { get; set; }

        public string Priority { get; set; }

        public string StudioLocation { get; set; }

        public string StudioName { get; set; }

        public string InwardDate { get; set; }

        public string PhotoShootStatus { get; set; }

        public string StockReturnToQC { get; set; }

        public string StockReturnDate { get; set; }

        public string RangeShot { get; set; }

        public string IndividualShot { get; set; }

        public string CatalogueShot { get; set; }

        public DateTime? CamQcL1SubmitDate { get; set; }

        public DateTime? SubmitDate { get; set; }
    }
}
