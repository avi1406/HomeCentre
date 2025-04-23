namespace HomeCentre.PIM.DAL;

public class ErrorLogTable : Entity
{
    public string StagingTableName { get; set; }

    public string StagingTableColumnName { get; set; }

    public long StagingTableRecordId { get; set; }

    public string ErrorDetails { get; set; }
}