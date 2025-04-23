using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class TemplateDeptCount
    {
        public string Dept_name { get; set; }

        public long? Uid_Count { get; set; }

        public string Division_Code { get; set; }

        public long? Buyer1_Count { get; set; }

        public long? Buyer2_Count { get; set; }

        public long? Qc_Count { get; set; }

        public long? Packaging_Count { get; set; }

        public long? Fab_Count { get; set; }

        public long? Dept_counts { get; set; }

        public long? Itemcode_Ready { get; set; }

        public long? Cam_Ready { get; set; }
    }
}