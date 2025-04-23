using System.Runtime.InteropServices;

namespace HomeCentre.PIM.DAL;

public class MarketplaceTemplateAttribute : Entity
{
    public long MarketplaceTemplateId { get; set; }

    public long? MarketplaceAttributeId { get; set; }

    public long ProductAttributeId { get; set; }

    public long? ProductDepartmentId { get; set; }

    public long? MarketPlaceId { get; set; }

    public string? MarketplaceAttribute { get; set; }

    public long? Datatype { get; set; }

    public string? ColumnOrder { get; set; }
}