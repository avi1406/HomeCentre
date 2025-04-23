using FluentMigrator;

namespace HomeCentre.PIM.Migrator.Migration;

[Migration(20221226)]
public class Version20221226: FluentMigrator.Migration
{
    public override void Up()
    {
        this.CreateMasterTable("ProductDepartment");
        this.CreateMasterTable("ProductClass");
        this.CreateMasterTable("ProductSubclass");
        this.DropTableIfExists("UploadedFileDataStageStatus");
        this.CreateMasterTable("HierarchicalMaster");
        this.CreateMasterTable("ProductPurpose");
        this.CreateMasterTable("PriceGrouping");
        this.CreateMasterTable("ProductSeason");
        this.DropTableIfExists("TemplateStageType");
        this.CreateMasterTable("ProductTemplateStatus");
        this.CreateMasterTable("TemplateStatus");
        
        this.CreateForeignKeyColIfNotExists("ProductDepartment", "DivisionId", "Division");
        this.CreateColIfNotExists("ProductDepartment", "Code", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("Division", "Code", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductClass", "Code", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("ProductSubclass", "Code", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("PriceGrouping", "Code", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("ProductSeason", "Code", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("ProductPurpose", "Code", col => col.AsString().NotNullable());
        
        this.CreateMasterTable("Stage");
        this.DropTableIfExists("UploadedFileDataStage");
        this.DropTableIfExists("ProductStage");

        this.CreateTableAndAddStandardColumns("ProductTemplate");
        
        this.CreateForeignKeyColIfNotExists("Template", "DivisionId", "Division");
        this.DropColIfExists("Template", "TemplateStageTypeId", "TemplateStageType");
        this.CreateForeignKeyColIfNotExists("Stage", "TemplateId", "Template");
        this.CreateForeignKeyColIfNotExists("Stage", "DepartmentId", "Department");
        this.CreateColIfNotExists("Template", "Order", col => col.AsInt32().Nullable());
        this.CreateColIfNotExists("Template", "IsSequential", col => col.AsBoolean().WithDefaultValue(false));
        
        this.CreateForeignKeyColIfNotExists("ProductTemplate", "TemplateId", "Template");
        this.CreateForeignKeyColIfNotExists("ProductTemplate", "ProductId", "Product");
        this.CreateForeignKeyColIfNotExists("ProductTemplate", "ProductTemplateStatusId", "ProductTemplateStatus");
        this.DropColIfExists("UploadedFileDataStage", "Errors");
        
        this.CreateColIfNotExists("HierarchicalMaster", "DivisionCode", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("HierarchicalMaster", "DepartmentCode", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("HierarchicalMaster", "DepartmentName", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("HierarchicalMaster", "ClassCode", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("HierarchicalMaster", "ClassName", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("HierarchicalMaster", "SubclassCode", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("HierarchicalMaster", "SubclassName", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("HierarchicalMaster", "DepartmentShortCode", col => col.AsString().NotNullable());
        
        this.CreateColIfNotExists("UploadedFile", "ConfigName", col => col.AsString().Nullable());
        this.CreateForeignKeyColIfNotExists("UploadedFile", "TemplateId", "Template");
        
        this.CreateColIfNotExists("Template", "IsUidTemplate", col => col.AsBoolean().Nullable().WithDefaultValue(false));
        
        this.CreateJsonBColIfNotExists("Product", "UidData");
        this.CreateJsonBColIfNotExists("Product", "ErrorData");
        this.CreateColIfNotExists("Product", "IsUidGenerated", col => col.AsBoolean().WithDefaultValue(false));
        this.CreateColIfNotExists("Department", "UidKeys", col => col.AsString().Nullable());
        this.CreateColIfNotExists("Division", "UidKeys", col => col.AsString().Nullable());
        
        this.Execute.Sql("SELECT add_constraint('producttemplate', 'producttemplate_productid_templateid_uq_idx', 'ALTER TABLE producttemplate ADD CONSTRAINT producttemplate_productid_templateid_uq_idx UNIQUE (productid, templateid);');");
        this.Execute.Sql("SELECT add_constraint('product', 'product_uid_uq_idx', 'ALTER TABLE product ADD CONSTRAINT product_uid_uq_idx UNIQUE (uid);');");
        
        this.Execute.Sql(@"CREATE SEQUENCE IF NOT EXISTS product_sequence
                                INCREMENT 1
                                START 1;");
        
        this.DropColIfExists("ProductAttribute", "DepartmentId");
        this.CreateForeignKeyColIfNotExists("ProductAttribute", "DivisionId", "Division");
        
        // ---
        this.CreateTableAndAddStandardColumns("EmailTemplate");
        this.CreateColIfNotExists("EmailTemplate", "Action", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("EmailTemplate", "Template", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("EmailTemplate", "Query", col => col.AsString().NotNullable());
        
        this.CreateForeignKeyColIfNotExists("TaskStep", "ActionTemplateId", "EmailTemplate");
        this.CreateForeignKeyColIfNotExists("TaskStep", "RejectTemplateId", "EmailTemplate");
        
        this.CreateForeignKeyColIfNotExists("TaskStepInstance", "ActionTemplateId", "EmailTemplate");
        this.CreateForeignKeyColIfNotExists("TaskStepInstance", "RejectTemplateId", "EmailTemplate");
        
        this.CreateForeignKeyColIfNotExists("Template", "TemplateStatusId", "TemplateStatus");
        
        this.CreateTableAndAddStandardColumns("TemplateRole");
        this.CreateForeignKeyColIfNotExists("TemplateRole", "TemplateId", "Template");
        this.CreateForeignKeyColIfNotExists("TemplateRole", "RoleId", "Role");
        
        this.CreateColIfNotExists("UploadedFile", "ErrorCount", col => col.AsInt32().Nullable());
    }

    public override void Down()
    {
    }
}