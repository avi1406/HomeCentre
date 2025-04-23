using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeCentre.PIM.Common;

namespace HomeCentre.PIM.DAL
{
    public class AttributesData
    {
        public string Uid { get; set; }

        public long PictureId { get; set; }

        public Dictionary<string, object> ColumnData { get; set; }

        public bool IsUidGenerated { get; set; }

        public Dictionary<string, List<string>> ErrorData { get; set; }

        public long? QcStatusId { get; set; }

        public string? Planogram_name { get; set; }

        public string? Div_name { get; set; }

        public string? Dept_name { get; set; }

        public string? Group_name { get; set; }

        public string? Class_name { get; set; }

        public string? Sub_class_name { get; set; }

        // Properties from ProductAttribute
        public string Name { get; set; }

        public string Label { get; set; }

        public AttrDataType? DataType { get; set; }

        public string LookupTable { get; set; }

        public string LookupTableColumn { get; set; }

        public bool IsRequired { get; set; }

        public long? DivisionId { get; set; }

        public bool IsDataList { get; set; }
    }
}
