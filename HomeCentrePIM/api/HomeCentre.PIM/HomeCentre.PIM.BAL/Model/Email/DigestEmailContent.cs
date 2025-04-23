using System.Collections.Generic;

namespace HomeCentre.PIM.BAL.Model.Email
{
    public class DigestEmailContent
    {
        public List<EmailItem> EmailContent { get; set; }
    }

    public class EmailItem
    {
        public string PlanoOrFamilyName { get; set; }

        public string CreatedDate { get; set; }

        public string UserName { get; set; }

        public string Division { get; set; }
    }
}
