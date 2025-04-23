using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCentre.PIM.DAL
{
    public class PlanoExportExcelModel
    {
        public string Division { get; set; }

        public long TotalPlanoFamilyCreated { get; set; }

        public long TotalPlanoFamilyApproved { get; set; }

        public long TotalPlanoFamilyRejected { get; set; }

        public long TotalPlanoFamilyApprovalPending { get; set; }

        public long ToatalUIDGenerated { get; set; }
    }

    public class ExportDivision
    {
        public long? DivisionId { get; set; }

        public bool Status { get; set; }
    }

    public class GetPlanoModalDto
    {
        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public List<ExportDivision> Data { get; set; }
    }
}