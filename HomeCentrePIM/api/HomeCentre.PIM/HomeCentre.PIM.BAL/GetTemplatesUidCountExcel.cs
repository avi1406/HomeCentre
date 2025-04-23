using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class GetTemplatesUidCountExcel
    {
        public string? Division_code { get; set; }

        public string? Dept_code { get; set; }

        public string? Class_code { get; set; }

        public string? Subclass_code { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public List<ExportDept> Data { get; set; }
    }

    public class ExportDept
    {
        public long? Division_code { get; set; }

        public bool Status { get; set; }
    }
}