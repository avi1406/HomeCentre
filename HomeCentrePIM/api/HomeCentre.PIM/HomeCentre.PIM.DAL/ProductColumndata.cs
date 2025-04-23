using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class ProductColumndata
    {
        public string Uid { get; set; }

        public Dictionary<string, object> UidColumndata { get; set; }

        public Dictionary<string, object> Buyer1Columndata { get; set; }

        public Dictionary<string, object> Buyer2Columndata { get; set; }

        public Dictionary<string, object> QcColumnData { get; set; }

        public Dictionary<string, object> PackagingColumnData { get; set; }

        public Dictionary<string, object> FabColumnData { get; set; }
    }
}
