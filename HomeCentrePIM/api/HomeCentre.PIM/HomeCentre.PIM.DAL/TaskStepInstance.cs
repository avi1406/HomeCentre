using System.Collections.Generic;

namespace HomeCentre.PIM.DAL;

public class TaskStepInstance : TaskStep
{
    public long TaskStepId { get; set; }

    public long WorkflowRunId { get; set; }

    public long TaskStepInstanceStatusId { get; set; }

    public string UserComments { get; set; }

    public string Title { get; set; } = string.Empty;
}

public class TasklevelSequence
{
    public long? TaskStepId { get; set; }

    public long TaskStepInstanceId { get; set; }

    public long? TaskNextStepId { get; set; }

    public long? RecordId { get; set; }

    public string UserAction { get; set; }

    public string? SuccessMessge { get; set; }

    public string? UserComments { get; set; }

    public string? RejectDescription { get; set; }
}

public class FieldUpdateAction
{
    public string ActionValue { get; set; }

    public bool IsNextStepId { get; set; }

    public List<FieldMapper> FieldName { get; set; }
}

public class FieldMapper
{
    public string Value { get; set; }

    public string DataType { get; set; }

    public string FieldName { get; set; }
}