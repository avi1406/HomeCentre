using System.Collections.Generic;

namespace HomeCentre.PIM.BAL.Model.Email
{
    public class ItemCodeEmailContent
    {
        public List<ItemCodecontent> ItemContent { get; set; }

        public int TotalItemcodeCount { get; set; }
    }

    public class ItemCodecontent
    {
        public string Uid { get; set; }

        public string Division { get; set; }
    }
}
