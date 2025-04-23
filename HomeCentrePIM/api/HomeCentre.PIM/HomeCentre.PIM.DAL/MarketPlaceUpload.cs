using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class MarketPlaceUpload : Entity
    {
        public long MarketplacetemplateId { get; set; }

        public string BlobUrl { get; set; }

        public long? Divisioid { get; set; }
    }
}
