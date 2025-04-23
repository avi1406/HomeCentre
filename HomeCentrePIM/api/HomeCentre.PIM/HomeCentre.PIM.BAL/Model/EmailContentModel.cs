using System;

namespace HomeCentre.PIM.BAL.Model
{
    public class EmailContentModel
    {
        public long UserId { get; set; }

        public string Emailcontent { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}