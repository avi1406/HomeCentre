using Dapper;

namespace HomeCentre.PIM.DAL;

public class WorkflowDesign : Entity
{
    public string Process { get; set; }

    public string Description { get; set; }

    public long TableMetadataId { get; set; }

    public string Trigger { get; set; }

    public string Name { get; set; }

    [ReadOnly(true)]
    [IgnoreInsert]
    [IgnoreUpdate]
    public string ConfigJson { get; set; }
}