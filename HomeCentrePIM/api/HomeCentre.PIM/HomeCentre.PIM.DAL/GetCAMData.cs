using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class GetCAMData
    {
        public string ItemCode { get; set; }

        public List<string> Statusfilter { get; set; }

        public DateTime? DateCreated { get; set; }

        public string Division { get; set; }

        public string Deptcode { get; set; }

        public long? Eancode { get; set; }

        public string? Uid { get; set; }

        public string? PlanogramName { get; set; }
    }
}
