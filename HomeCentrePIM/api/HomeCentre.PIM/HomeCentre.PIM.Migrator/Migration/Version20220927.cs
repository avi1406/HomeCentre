using FluentMigrator;

namespace HomeCentre.PIM.Migrator.Migration;
[Migration(20220927)]
public class Version20220927 : FluentMigrator.Migration
{
    public override void Up()
    {
        this.CreateMasterTable("Department");
        this.CreateForeignKeyColIfNotExists("AppUser", "DepartmentId", "Department", isnullable: true);

        this.CreateMasterTable("AppSettings");
        this.CreateColIfNotExists("AppSettings", "Value", col => col.AsString().NotNullable());
        this.CreateJsonBColIfNotExists("AppSettings", "JsonValue");

        this.CreateStagingTable("EmployeeStaging");
        this.CreateColIfNotExists("EmployeeStaging", "FirstName", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("EmployeeStaging", "LastName", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("EmployeeStaging", "Email", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("EmployeeStaging", "PhoneNo", col => col.AsString());
        this.CreateColIfNotExists("EmployeeStaging", "IsRemoved", col => col.AsBoolean().WithDefaultValue(false));

        this.CreateTableAndAddStandardColumns("FileConfiguration");
        this.CreateColIfNotExists("FileConfiguration", "Name", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("FileConfiguration", "FileDirPath", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("FileConfiguration", "FileDirUsername", col => col.AsString().Nullable());
        this.CreateColIfNotExists("FileConfiguration", "FileDirPassword", col => col.AsString().Nullable());
        this.CreateColIfNotExists("FileConfiguration", "TargetTable", col => col.AsString().NotNullable());

        this.CreateTableAndAddStandardColumns("ErrorLogTable");
        this.CreateColIfNotExists("ErrorLogTable", "StagingTableName", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("ErrorLogTable", "StagingTableColumnName", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("ErrorLogTable", "StagingTableRecordId", col => col.AsInt64().NotNullable());
        this.CreateColIfNotExists("ErrorLogTable", "ErrorDetails", col => col.AsString().NotNullable());

        this.CreateMasterTable("TableMetadata");
        this.CreateMasterTable("TaskStepInstanceStatus");
        this.CreateTableAndAddStandardColumns("WorkflowDesign");
        this.CreateColIfNotExists("WorkflowDesign", "Process", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("WorkflowDesign", "Description", col => col.AsString().NotNullable());
        this.CreateForeignKeyColIfNotExists("WorkflowDesign", "TableMetadataId", "TableMetadata");
        this.CreateColIfNotExists("WorkflowDesign", "Trigger", col => col.AsString().NotNullable());
        this.CreateJsonBColIfNotExists("WorkflowDesign", "ConfigJson");

        this.CreateTableAndAddStandardColumns("WorkflowRun");
        this.CreateColIfNotExists("WorkflowRun", "Process", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("WorkflowRun", "Description", col => col.AsString().NotNullable());
        this.CreateForeignKeyColIfNotExists("WorkflowRun", "TableMetadataId", "TableMetadata");
        this.CreateColIfNotExists("WorkflowRun", "Trigger", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("WorkflowRun", "RecordId", col => col.AsInt64().NotNullable());
        this.CreateJsonBColIfNotExists("WorkflowRun", "ConfigJson");

        this.CreateTableAndAddStandardColumns("TaskStep");
        this.CreateColIfNotExists("TaskStep", "Name", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("TaskStep", "TableName", col => col.AsString().Nullable());
        this.CreateColIfNotExists("TaskStep", "Sequence", col => col.AsInt64().NotNullable());
        this.CreateColIfNotExists("TaskStep", "IsParallel", col => col.AsBoolean().NotNullable().WithDefaultValue(false));
        this.CreateForeignKeyColIfNotExists("TaskStep", "NextStepId", "TaskStep");
        this.CreateColIfNotExists("TaskStep", "IsFinalStep", col => col.AsBoolean().NotNullable().WithDefaultValue(false));
        this.CreateForeignKeyColIfNotExists("TaskStep", "OwnerId", "AppUser");
        this.CreateColIfNotExists("TaskStep", "IsAll", col => col.AsBoolean().NotNullable().WithDefaultValue(false));
        this.CreateForeignKeyColIfNotExists("TaskStep", "WorkflowDesignId", "WorkflowDesign");
        this.CreateForeignKeyColIfNotExists("TaskStep", "RejectedStageId", "TaskStep");

        this.CreateTableAndAddStandardColumns("TaskStepInstance");
        this.CreateColIfNotExists("TaskStepInstance", "Name", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("TaskStepInstance", "TableName", col => col.AsString().Nullable());
        this.CreateColIfNotExists("TaskStepInstance", "Sequence", col => col.AsInt64().NotNullable());
        this.CreateColIfNotExists("TaskStepInstance", "IsParallel", col => col.AsBoolean().NotNullable().WithDefaultValue(false)); 
        this.CreateForeignKeyColIfNotExists("TaskStepInstance", "NextStepId", "TaskStepInstance");
        this.CreateColIfNotExists("TaskStepInstance", "IsFinalStep", col => col.AsBoolean().NotNullable().WithDefaultValue(false));
        this.CreateForeignKeyColIfNotExists("TaskStepInstance", "OwnerId", "AppUser");
        this.CreateColIfNotExists("TaskStepInstance", "IsAll", col => col.AsBoolean().NotNullable().WithDefaultValue(false));
        this.CreateForeignKeyColIfNotExists("TaskStepInstance", "WorkflowDesignId", "WorkflowDesign");
        this.CreateForeignKeyColIfNotExists("TaskStepInstance", "WorkflowRunId", "WorkflowRun");
        this.CreateForeignKeyColIfNotExists("TaskStepInstance", "RejectedStageId", "TaskStep");
        this.CreateForeignKeyColIfNotExists("TaskStepInstance", "TaskStepId", "TaskStep");
        this.CreateForeignKeyColIfNotExists("TaskStepInstance", "TaskStepInstanceStatusId", "TaskStepInstanceStatus");
        this.CreateColIfNotExists("TaskStepInstance", "UserComments", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("TaskStepInstance", "Action", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("TaskStepInstance", "Title", col => col.AsString().Nullable());

        this.CreateTableAndAddStandardColumns("UidInitiation");
        this.CreateColIfNotExists("UidInitiation", "Planogram", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("UidInitiation", "FamilyName", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("UidInitiation", "Status", col => col.AsString().NotNullable());
        this.CreateForeignKeyColIfNotExists("UidInitiation", "FirstLevelApprovedBy", "AppUser");
        this.CreateColIfNotExists("UidInitiation", "FirstLevelApprovedDate", col => col.AsDateTime().Nullable());
        this.CreateForeignKeyColIfNotExists("UidInitiation", "SecondLevelApprovedBy", "AppUser");
        this.CreateColIfNotExists("UidInitiation", "SecondLevelApprovedDate", col => col.AsDateTime().Nullable());
        this.CreateColIfNotExists("UidInitiation", "FirstLevelComments", col => col.AsString().Nullable());
        this.CreateColIfNotExists("UidInitiation", "SecondLevelComments", col => col.AsString().Nullable());
        this.CreateColIfNotExists("UidInitiation", "Description", col => col.AsString().Nullable());
        this.CreateColIfNotExists("UidInitiation", "FirstLevelAction", col => col.AsString().Nullable());
        this.CreateColIfNotExists("UidInitiation", "SecondLevelAction", col => col.AsString().Nullable());
        
        this.CreateMasterTable("Division");
        this.CreateForeignKeyColIfNotExists("Department", "DivisionId", "Division", isnullable: true);
        this.CreateForeignKeyColIfNotExists("AppUserRole", "DepartmentId", "Department", isnullable: true);
        this.DropColIfExists("AppUser", "B2CObjectionId");
        this.CreateColIfNotExists("AppUser", "B2CObjectId", column => column.AsString().Nullable());
    }

    public override void Down()
    {
    }
}