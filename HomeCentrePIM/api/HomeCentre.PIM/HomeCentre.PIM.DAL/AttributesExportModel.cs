using System;

namespace HomeCentre.PIM.DAL
{
    public class AttributesExportModel
    {
        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public string? Season { get; set; }

        public string? DeptCode { get; set; }

        public string? ClassCode { get; set; }

        public string? SubClassCode { get; set; }

        public long[]? RoleIds { get; set; }   

        public string? Templatetype { get; set; }
    }
}
