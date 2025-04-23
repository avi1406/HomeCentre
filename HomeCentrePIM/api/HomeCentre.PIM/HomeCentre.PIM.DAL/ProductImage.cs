using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class ProductImage : Entity
    {
        public long ItemCode { get; set; }

        public string? ImageType { get; set; }

        public string? ImageUrl { get; set; }

        public long? Divisionid { get; set; }

        public string? Userdivision { get; set; }

        public DateTime? ReceivedDate {get; set; }
    }
}
