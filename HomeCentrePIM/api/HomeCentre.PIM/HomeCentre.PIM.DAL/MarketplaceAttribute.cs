using HomeCentre.PIM.Common;

namespace HomeCentre.PIM.DAL;

public class MarketplaceAttribute : Entity
{
    public string Name { get; set; }
    
    public string Description { get; set; }

    public AttrDataType DataType { get; set; }

    public bool IsRequired { get; set; }

    public long MarketplaceId { get; set; }

    public long? DivisionId { get; set; }
}