using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class ReceivedProductsModel
    {
        public string? Status { get; set; }

        public DateTime? ReceivedFromDate { get; set; }

        public DateTime? ReceivedToDate { get; set; }

        public DateTime? HandOverFromDate { get; set; }

        public DateTime? HandOverToDate { get; set; }

        public string? Division { get; set; }

        public string? UserDivision { get; set; }

        public string? PSstatus { get; set; }

        public string? CamPstatus { get; set; }

        public string? CamRstatus { get; set; }
    }
}
