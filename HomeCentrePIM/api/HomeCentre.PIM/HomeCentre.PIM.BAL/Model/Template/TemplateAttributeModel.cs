using HomeCentre.PIM.Common;

namespace HomeCentre.PIM.BAL.Model.Template;

public class TemplateAttributeModel
{
    public string Name { get; set; }

    public long TemplateAttributeId { get; set; }

    public long TemplateId { get; set; }

    public long AttributeId { get; set; }

    public AttrDataType? DataType { get; set; }

    public bool IsDataList { get; set; }
}