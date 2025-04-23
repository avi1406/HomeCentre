using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class ContentModel
    {
        public long Itemcode { get; set; }

        public long? EanCode { get; set; }

        public string Image { get; set; }

        public string Contentaction { get; set; }

        public string? ReceivedDate { get; set; }

        public string? Rangeshotdate { get; set; }

        public string? Catalogshotdate { get; set; }

        public string? Individualstyledate { get; set; }

        public long DivisionId { get; set; }

        public string Priority { get; set; }

        public string StudioLocation { get; set; }

        public string StudioName { get; set; }

        public string PhotoShootStatus { get; set; }

        public string? Pictureid { get; set; }

        public string? PhotoshootComment { get; set; }

        public string? Stockreturn { get; set; }

        public DateTime? CamQcL2SubmitDate { get; set; }
    }
}
