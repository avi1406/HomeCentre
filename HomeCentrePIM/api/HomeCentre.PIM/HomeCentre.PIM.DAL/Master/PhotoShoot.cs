using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL.Master
{
    public class PhotoShoot : Entity
    {
        public long? ItemCode { get; set; }

        public long? Eancode { get; set; }

        public string? ImageUrl { get; set; }

        public string? Imagetype { get; set; }

        public string? ReceivedDate { get; set; }

        public string? Priority { get; set; }

        public string? StudioName { get; set; }

        public string? StudioLocation { get; set; }

        public string? InwardDate { get; set; }

        public string? PhotoShootStatus { get; set; }

        public string Division { get; set; }

        public long DivisionId { get; set; }

        public string? Comments { get; set; }   

        public string? StockReturnToQC { get; set; }

        public string? StockReturnDate { get; set; }

        public string? RangeShot { get; set; }

        public string? IndividualStyle { get; set; }

        public string? CatologShot { get; set; }

        public string? Pictureid { get; set; }

        public string? Productreceived { get; set; }

        public DateTime? Rangeshotdate { get; set; }

        public long StudioId { get; set; }

        public DateTime? CamQcL1SubmitDate { get; set; }

        public DateTime? SubmitDate { get; set; }
    }
}
