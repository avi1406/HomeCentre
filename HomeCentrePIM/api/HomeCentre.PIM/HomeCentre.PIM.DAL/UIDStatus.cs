using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class UIDStatus
    {
        public string ItemCode { get; set; }

        public string Uidcode { get; set; }

        public int CountOfKeysInLabels { get; set; }

        public bool AllKeysPresentInLabels { get; set; }

        public int PercentageOfKeysInLabels { get; set; }

        public string Uidstatus { get; set; }

        public Dictionary<string, object> ColumnData { get; set; }

        public long Division { get; set; }
    }
}
