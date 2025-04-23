namespace HomeCentre.PIM.DAL;

public class TaskStep : Entity
{
    public string Name { get; set; }

    public string TableName { get; set; }

    public int Sequence { get; set; }

    public bool IsParallel { get; set; }

    public long? NextStepId { get; set; }

    public bool IsFinalStep { get; set; }

    public bool IsNextStep { get; set; }

    public long OwnerId { get; set; }

    public long? OwnerRoleId { get; set; }

    public bool IsAll { get; set; }

    public long WorkflowDesignId { get; set; }

    public long? RejectedStageId { get; set; }

    public string Action { get; set; }

    public string UserActionJson { get; set; }

    public long Activity { get; set; }

    public long TypeId { get; set; }

    public long? ActionTemplateId { get; set; }
    
    public long? RejectTemplateId { get; set; }
}