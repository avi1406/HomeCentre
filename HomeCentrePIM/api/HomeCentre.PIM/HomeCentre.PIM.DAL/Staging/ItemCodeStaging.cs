using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL.Staging
{
    public class ItemCodeStaging :Entity
    {
        public long? Code { get; set; }

        public long? EanCode { get; set; }

        public string? UidCode { get; set; }

        public DateTime? SourceDateCreated { get; set; }
    }
}
