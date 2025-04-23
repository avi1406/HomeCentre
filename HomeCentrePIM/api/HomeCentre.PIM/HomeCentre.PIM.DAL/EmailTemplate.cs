namespace HomeCentre.PIM.DAL;

public class EmailTemplate: Entity
{
    public string Action { get; set; }

    public string Template { get; set; }

    public string Query { get; set; }
}