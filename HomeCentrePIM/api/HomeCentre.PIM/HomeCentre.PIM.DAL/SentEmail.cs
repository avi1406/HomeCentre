namespace HomeCentre.PIM.DAL;

public class SentEmail: Entity
{
    public int EmailFrequency { get; set; }

    public string EmailContent { get; set; }

    public int Status { get; set; }

    public long ObjectId { get; set; }

    public long RecordId { get; set; }

    public long TaskStepInstanceId { get; set; }
}