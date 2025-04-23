using System.Collections.Generic;

namespace HomeCentre.PIM.BAL.Model.Email
{
    public class CamEmailContent
    {
        public List<Camcontent> CamContent { get; set; }

        public int TotalCamCount { get; set; }
    }

    public class Camcontent
    {
        public string Uid { get; set; }

        public string Division { get; set; }

        public string PlanoOrFamily { get; set; }

        public string Itemcode { get; set; }
    }
}
