namespace HomeCentre.PIM.DAL;

public class ProductQueryComment: Entity
{
    public string Comment { get; set; }

    public long ProductQueryId { get; set; }
}