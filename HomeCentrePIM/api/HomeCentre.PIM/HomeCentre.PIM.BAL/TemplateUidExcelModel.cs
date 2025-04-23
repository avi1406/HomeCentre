using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class TemplateUidExcelModel
    {
        public string Division { get; set; }

        public string ToatalUIDGenerated { get; set; }

        public string BuyerTemp1 { get; set; }

        public string BuyerTemp2 { get; set; }

        public string QcTemp { get; set; }

        public string FABTemp { get; set; }

        public string DesignerTemp { get; set; }

        public string ItemcodeReady { get; set; }

        public string CAMReady { get; set; }
    }
}