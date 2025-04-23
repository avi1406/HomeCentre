namespace HomeCentre.PIM.DAL;

public class Actor: Entity
{
    public long AppUserId { get; set; }
    
    public long TaskStepInstanceId { get; set; }
}