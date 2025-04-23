namespace HomeCentre.PIM.DAL;

public class Marketplace : Entity
{
    private readonly object? divisionId;

    public string Name { get; set; }

    public string ShortCode { get; set; }
}