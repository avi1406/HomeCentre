using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class PlanoUserCountModel
    {
        public long Planogram_Total_Count { get; set; }

        public long Planogram_Approved_Count { get; set; }

        public long Planogram_Rejected_Count { get; set; }

        public long Planogram_Pending_Count { get; set; }

        public long Uid_Count { get; set; }

        public string Name { get; set; }

        public long? Division { get; set; }
    }
}