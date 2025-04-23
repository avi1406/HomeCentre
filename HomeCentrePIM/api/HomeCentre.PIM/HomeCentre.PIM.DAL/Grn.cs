using System;

namespace HomeCentre.PIM.DAL
{
    public class Grn : Entity
    {
        public long? ItemCode { get; set; }

        public long? Grn_Location { get; set; }

        public DateTime? Grn_Date { get; set; }
    }
}
