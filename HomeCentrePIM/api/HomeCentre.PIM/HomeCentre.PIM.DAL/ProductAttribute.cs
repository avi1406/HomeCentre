using System.ComponentModel.DataAnnotations;
using HomeCentre.PIM.Common;

namespace HomeCentre.PIM.DAL;

public class ProductAttribute: Entity  
{
    public string Name { get; set; }
    
    public string Label { get; set; }

    public AttrDataType? DataType { get; set; }

    public string LookupTable { get; set; }

    public string LookupTableColumn { get; set; }

    public bool IsRequired { get; set; }

    public long? DivisionId { get; set; }

    public bool IsDataList { get; set; }
}