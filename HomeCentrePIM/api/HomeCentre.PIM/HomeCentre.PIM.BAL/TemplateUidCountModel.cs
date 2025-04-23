using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class TemplateUidCountModel
    {
        public string DivisionCode { get; set; }

        public string UidCount { get; set; }

        public string BuyerTemplate1 { get; set; }

        public string BuyerTemplate2 { get; set; }

        public string QcTemplate { get; set; }

        public string Fab { get; set; }

        public string PackagingDesigner { get; set; }

        public string Itemcode_Ready { get; set; }

        public string Cam_Ready { get; set; }
    }
}