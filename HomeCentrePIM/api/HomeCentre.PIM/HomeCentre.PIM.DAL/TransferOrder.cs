using System;

namespace HomeCentre.PIM.DAL
{
    public class TransferOrder : Entity
    {
        public long? ItemCode { get; set; }

        public long? From_Loc { get; set; }

        public long? To_Loc { get; set; }

        public DateTime? To_Date { get; set; }
    }
}
