using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class PlanoCountModel
    {
        public int? Division_code { get; set; }

        public long Total_Count { get; set; }

        public long Approved_Count { get; set; }

        public long Rejected_Count { get; set; }

        public long Pending_Count { get; set; }

        public long Uid_Count { get; set; }
    }
}