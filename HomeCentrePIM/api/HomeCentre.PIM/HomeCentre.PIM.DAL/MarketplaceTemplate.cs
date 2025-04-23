namespace HomeCentre.PIM.DAL;

public class MarketplaceTemplate : Entity
{
    public string Name { get; set; }

    public long MarketplaceId { get; set; }

    public long ProductDepartmentId { get; set; }

    public long DivisionId { get; set; }
}