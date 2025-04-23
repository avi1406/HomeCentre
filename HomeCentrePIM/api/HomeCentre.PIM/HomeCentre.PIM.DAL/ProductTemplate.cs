namespace HomeCentre.PIM.DAL;

public class ProductTemplate: Entity
{
    public long TemplateId { get; set; }

    public long ProductId { get; set; }

    public long ProductTemplateStatusId { get; set; }
}