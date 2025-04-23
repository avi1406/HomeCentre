using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class ItemCodeModel 
    {
        public string ItemCode { get; set; }

        public List<string> Status { get; set; }

        public DateTime? DateCreated { get; set; }
    }
}
