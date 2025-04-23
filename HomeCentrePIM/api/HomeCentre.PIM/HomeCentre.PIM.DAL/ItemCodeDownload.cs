using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class ItemCodeDownload
    {
        public long ItemCode { get; set; }

        public string Uid { get; set; }

        public string? PlanoOrFamilyName { get; set; }

        public string Eancode { get; set; }

        public string Statusfilter { get; set; }

        public DateTime DateCreated { get; set; }

        public string Division { get; set; }

        public string DeptName { get; set; }
    }
}
