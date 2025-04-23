namespace HomeCentre.PIM.DAL;

public class TemplateMapping: Entity
{
    public string SourceColumn { get; set; }
    
    public string DestinationColumn { get; set; }
    
    public bool IsIdColumn { get; set; }

    public string Datatype { get; set; }

    public long TemplateId { get; set; }
}