using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.BAL.Model
{
    public class ExcelJsonModel
    {
        public string Container { get; set; }

        public string FileName { get; set; }

        public string Extension { get; set; }

        public string StagingTable { get; set; }

        public string IntegrationType { get; set; }

        public long TemplateID { get; set; }

        public string MessageName { get; set; }
    }
}
