using System.IO;
using FluentMigrator;

namespace HomeCentre.PIM.Migrator.Migration;

[Migration(20221205)]
public class Version20221205 : FluentMigrator.Migration
{
    public override void Up()
    {
        this.CreateTableAndAddStandardColumns("Template");
        this.CreateTableAndAddStandardColumns("ProductAttribute");
        this.CreateTableAndAddStandardColumns("TemplateAttribute");
        this.CreateTableAndAddStandardColumns("UploadedFile");
        this.CreateTableAndAddStandardColumns("UploadedFileData");
        this.CreateTableAndAddStandardColumns("Product");
        this.CreateMasterTable("RejectionReason");

        this.CreateMasterTable("UploadedFileStatus");
        this.CreateMasterTable("UploadedFileType");

        this.CreateColIfNotExists("Template", "Name", col => col.AsString().NotNullable());
        this.CreateForeignKeyColIfNotExists("Template", "DepartmentId", "Department");

        this.CreateColIfNotExists("ProductAttribute", "Name", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("ProductAttribute", "Label", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("ProductAttribute", "DataType", col => col.AsInt32().NotNullable());
        this.CreateColIfNotExists("ProductAttribute", "LookupTable", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductAttribute", "LookupTableColumn", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductAttribute", "IsRequired", col => col.AsBoolean().Nullable());

        this.CreateForeignKeyColIfNotExists("TemplateAttribute", "TemplateId", "Template");
        this.CreateForeignKeyColIfNotExists("TemplateAttribute", "AttributeId", "ProductAttribute");

        this.CreateColIfNotExists("UploadedFile", "Name", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("UploadedFile", "BlobUrl", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("UploadedFile", "TableName", col => col.AsString().NotNullable());
        this.CreateForeignKeyColIfNotExists("UploadedFile", "UploadedFileStatusId", "UploadedFileStatus");

        this.CreateForeignKeyColIfNotExists("UploadedFileData", "UploadedFileId", "UploadedFile");
        this.CreateJsonBColIfNotExists("UploadedFileData", "ColumnData");
        this.CreateJsonBColIfNotExists("UploadedFileData", "ErrorData");

        this.CreateColIfNotExists("Product", "Uid", col => col.AsString().NotNullable());
        this.CreateJsonBColIfNotExists("Product", "ColumnData");

        this.CreateForeignKeyColIfNotExists("TaskStep", "OwnerRoleId", "Role");
        this.CreateForeignKeyColIfNotExists("TaskStepInstance", "OwnerRoleId", "Role");
        this.CreateColIfNotExists("TaskStepInstance", "UserActionJson", col => col.AsString().Nullable());
        this.CreateColIfNotExists("TaskStep", "UserActionJson", col => col.AsString().Nullable());
        this.CreateColIfNotExists("TaskStep", "IsNextStep", col => col.AsBoolean().Nullable());
        this.CreateColIfNotExists("TaskStepInstance", "IsNextStep", col => col.AsBoolean().Nullable());
        this.CreateColIfNotExists("Department", "IsPlanogram", col => col.AsBoolean().Nullable());

        this.CreateColIfNotExists("AppUser", "EmployeeNo", col => col.AsString().Nullable());
        this.CreateForeignKeyColIfNotExists("UidInitiation", "DepartmentId", "Department");
        this.CreateForeignKeyColIfNotExists("UidInitiation", "DivisionId", "Division");
        
        this.CreateColIfNotExists("RejectionReason", "Code", col => col.AsString().NotNullable());
        this.DropColIfExists("UidInitiation", "Planocode");
        
        this.CreateTableAndAddStandardColumns("SentEmail");
        this.CreateColIfNotExists("SentEmail", "EmailFrequency", col => col.AsInt32());
        this.CreateColIfNotExists("SentEmail", "EmailContent", col => col.AsString());
        this.CreateColIfNotExists("SentEmail", "Status", col => col.AsInt32());
        this.DropColIfExists("SentEmail", "ObjectId");
        this.CreateForeignKeyColIfNotExists("SentEmail", "ObjectId", "TableMetadata");
        this.CreateColIfNotExists("SentEmail", "RecordId", col => col.AsInt64());
        this.CreateForeignKeyColIfNotExists("SentEmail", "TaskStepInstanceId", "TaskStepInstance");
        
        this.CreateTableAndAddStandardColumns("Actor");
        this.CreateForeignKeyColIfNotExists("Actor", "AppUserId", "AppUser");
        this.CreateForeignKeyColIfNotExists("Actor", "TaskStepInstanceId", "TaskStepInstance");
        
        this.CreateColIfNotExists("TaskStep", "Activity", col => col.AsInt32().Nullable());
        this.DropColIfExists("TaskStep", "Type");
        this.CreateColIfNotExists("TaskStep", "TypeId", col => col.AsInt32().Nullable());
        
        this.DropColIfExists("TaskStepInstance", "Type");
        this.CreateColIfNotExists("TaskStepInstance", "Activity", col => col.AsInt32().Nullable());
        this.CreateColIfNotExists("TaskStepInstance", "TypeId", col => col.AsInt32().Nullable());
        
        this.DropColIfExists("FileConfiguration", "ConfigName");
        this.CreateColIfNotExists("UploadedFile", "ConfigName", col => col.AsString().Nullable());
        
        this.Execute.Sql(File.ReadAllText("Migration/Version20221205.sql"));
    }

    public override void Down()
    {
    }
}