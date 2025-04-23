using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class PhotoShootProduct : Entity
    {
        public long Itemcode { get; set; }

        public long? EanCode { get; set; }

        public string? Name { get; set; }
    }
}
