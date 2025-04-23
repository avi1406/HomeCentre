namespace HomeCentre.PIM.DAL.Master;

public class Department: MasterEntity
{
    public string UidKeys { get; set; }

    public long DivisionId { get; set; }

    public bool IsPlanogram { get; set; }
}