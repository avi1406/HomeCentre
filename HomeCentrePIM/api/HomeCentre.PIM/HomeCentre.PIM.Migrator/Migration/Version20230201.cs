using DocumentFormat.OpenXml.Presentation;
using FluentMigrator;

namespace HomeCentre.PIM.Migrator.Migration;

[Migration(20230201)]
public class Version20230201 : FluentMigrator.Migration
{
    public override void Up()
    {
        this.CreateMasterTable("QueryType");
        this.CreateMasterTable("QueryPriority");
        this.CreateMasterTable("ProductQcStatus");
        this.CreateMasterTable("ProductQueryStatus");
        this.CreateMasterTable("Season");
        this.CreateMasterTable("ProductGroup");
        this.CreateMasterTable("Business");
        this.CreateMasterTable("ItemCode");
        this.CreateMasterTable("MtrProductColour");
        this.CreateMasterTable("MtrSetPcs");
        this.CreateMasterTable("MtrLeadTime");
        this.CreateMasterTable("MtrItemShape");
        this.CreateMasterTable("MtrPackagingType");
        this.CreateMasterTable("MtrFinish");
        this.CreateMasterTable("MtrPrimaryFurnitureMaterial");
        this.CreateMasterTable("MtrSecondaryFurnitureMaterial");
        this.CreateMasterTable("Mtrproducttype1");
        this.CreateMasterTable("MtrProductSubtype1");
        this.CreateMasterTable("Mtrproducttype2");
        this.CreateMasterTable("MtrProductSubtype2");
        this.CreateMasterTable("MtrProductAttribute");
        this.CreateMasterTable("TableMetaDataColumn");
        this.CreateMasterTable("Purchasetype");
        this.CreateMasterTable("PriceGrouping");
        this.CreateMasterTable("ProductPurpose");
        this.CreateMasterTable("Brand");
        this.CreateMasterTable("UploadPicture");
        this.CreateMasterTable("MtrItemTypeCode");
        this.CreateMasterTable("MtrProductClassificationCode");
        this.CreateMasterTable("MtrPLCCode");
        this.CreateMasterTable("MtrUniqueIdStatus");
        this.CreateMasterTable("MtrProductGrouping");
        this.CreateMasterTable("MtrPrimarySecondarySKU");
        this.CreateMasterTable("MtrCountryOfOrigin");
        this.CreateMasterTable("MtrCurrency");
        this.CreateMasterTable("MtrTaxCategory");
        this.CreateMasterTable("MtrInnerCartonCode");
        this.CreateMasterTable("MtrMasterCartonCode");
        this.CreateMasterTable("MtrDiffGroup2Val");
        this.CreateMasterTable("MtrMadeOfValue");
        this.CreateMasterTable("MtrSupplierCode");
        this.CreateMasterTable("MtrProductImageType");
        this.CreateMasterTable("MtrHandoverStatus");
        this.CreateMasterTable("MtrCAMQCStatus");
        this.CreateMasterTable("MtrPhotoshootStatus");
        this.CreateMasterTable("MtrHSNCode");
        this.CreateMasterTable("MasterDivision");
        this.CreateMasterTable("HelpEmail");
        this.CreateMasterTable("Warehouse");

        this.CreateTableAndAddStandardColumns("Marketplace");
        this.CreateTableAndAddStandardColumns("MarketplaceAttribute");
        this.CreateTableAndAddStandardColumns("MarketplaceTemplate");
        this.CreateTableAndAddStandardColumns("MarketplaceTemplateAttribute");
        this.CreateTableAndAddStandardColumns("ProductQuery");
        this.CreateTableAndAddStandardColumns("ProductQueryItem");
        this.CreateTableAndAddStandardColumns("ProductQueryComment");
        this.CreateTableAndAddStandardColumns("AppuserMarketplace");
        this.CreateTableAndAddStandardColumns("DataRequest");
        this.CreateTableAndAddStandardColumns("DataRequestItem");
        this.CreateTableAndAddStandardColumns("TemplateMapping");
        this.CreateTableAndAddStandardColumns("RmsStaging");
        this.CreateTableAndAddStandardColumns("ItemCodeStaging");
        this.CreateTableAndAddStandardColumns("Studio");
        this.CreateTableAndAddStandardColumns("StudioCapacity");
        this.CreateTableAndAddStandardColumns("ProductStudio");
        this.CreateTableAndAddStandardColumns("ProductImage");
        this.CreateTableAndAddStandardColumns("PhotoShootProduct");
        this.CreateTableAndAddStandardColumns("TemplateHistory");
        this.CreateTableAndAddStandardColumns("MtrProductype_Subtype");
        this.CreateTableAndAddStandardColumns("ProductColumndata");
        this.CreateTableAndAddStandardColumns("PhotoshootUploadPicture");
        this.CreateTableAndAddStandardColumns("Grn");
        this.CreateTableAndAddStandardColumns("TransferOrder");

        this.CreateColIfNotExists("Marketplace", "ShortCode", x => x.AsString().NotNullable());
        this.CreateColIfNotExists("Marketplace", "Name", col => col.AsString());
        this.CreateColIfNotExists("Marketplace", "Description", col => col.AsString().Nullable());
        this.CreateColIfNotExists("Marketplace", "DataType", col => col.AsInt32().NotNullable().WithDefaultValue(0));

        this.CreateColIfNotExists("TemplateHistory", "Uid", x => x.AsString().NotNullable());
        this.CreateColIfNotExists("TemplateHistory", "Divisioncode", x => x.AsString().NotNullable());
        this.CreateColIfNotExists("TemplateHistory", "Departmentcode", x => x.AsString().NotNullable());
        this.CreateColIfNotExists("TemplateHistory", "TemplateId", x => x.AsInt64());

        this.CreateForeignKeyColIfNotExists("MarketplaceAttribute", "MarketplaceId", "Marketplace");
        this.CreateForeignKeyColIfNotExists("MarketplaceAttribute", "DivisionId", "Division");
        this.CreateColIfNotExists("MarketplaceAttribute", "Name", col => col.AsString());
        this.CreateColIfNotExists("MarketplaceAttribute", "Description", col => col.AsString().Nullable());
        this.CreateColIfNotExists("MarketplaceAttribute", "DataType", col => col.AsInt32().WithDefaultValue(10));
        this.CreateColIfNotExists("MarketplaceAttribute", "IsRequired", col => col.AsBoolean().WithDefaultValue(false));

        this.CreateForeignKeyColIfNotExists("MarketplaceTemplate", "MarketplaceId", "Marketplace");
        this.CreateForeignKeyColIfNotExists("MarketplaceTemplate", "ProductDepartmentId", "ProductDepartment");
        this.CreateForeignKeyColIfNotExists("MarketplaceTemplate", "DivisionId", "Division");
        this.CreateColIfNotExists("MarketplaceTemplate", "Name", col => col.AsString());

        this.CreateForeignKeyColIfNotExists("MarketplaceTemplateAttribute", "MarketplaceTemplateId", "MarketplaceTemplate");
        this.CreateForeignKeyColIfNotExists("MarketplaceTemplateAttribute", "MarketplaceAttributeId", "MarketplaceAttribute");
        this.CreateForeignKeyColIfNotExists("MarketplaceTemplateAttribute", "ProductAttributeId", "ProductAttribute");

        this.CreateForeignKeyColIfNotExists("ProductQuery", "QueryTypeId", "QueryType");
        this.CreateForeignKeyColIfNotExists("ProductQuery", "QueryPriorityId", "QueryPriority");
        this.CreateForeignKeyColIfNotExists("ProductQuery", "AssignedToId", "AppUser");
        this.CreateColIfNotExists("ProductQuery", "Description", col => col.AsString());
        this.CreateColIfNotExists("ProductQuery", "Picture", col => col.AsString().Nullable());
        this.CreateForeignKeyColIfNotExists("ProductQuery", "ProductQueryStatusId", "ProductQueryStatus");

        this.CreateForeignKeyColIfNotExists("ProductQueryItem", "ProductId", "Product");
        this.CreateForeignKeyColIfNotExists("ProductQueryItem", "ProductQueryId", "ProductQuery");

        this.CreateForeignKeyColIfNotExists("Product", "QcStatusId", "ProductQcStatus");
        this.CreateForeignKeyColIfNotExists("Product", "itemcodeid", "itemcode");

        this.CreateForeignKeyColIfNotExists("AppuserMarketplace", "MarketplaceId", "Marketplace");
        this.CreateForeignKeyColIfNotExists("AppuserMarketplace", "AppUserId", "AppUser");

        this.CreateColIfNotExists("DataRequest", "Name", col => col.AsString());
        this.CreateForeignKeyColIfNotExists("DataRequest", "TemplateId", "MarketplaceTemplate");

        this.CreateForeignKeyColIfNotExists("DataRequestItem", "DataRequestId", "DataRequest");
        this.CreateForeignKeyColIfNotExists("DataRequestItem", "ProductId", "Product");

        this.CreateColIfNotExists("UidInitiation", "L1RejectDescription", col => col.AsString().Nullable());
        this.CreateColIfNotExists("UidInitiation", "L2RejectDescription", col => col.AsString().Nullable());

        this.CreateForeignKeyColIfNotExists("ProductQueryComment", "ProductQueryId", "ProductQuery");
        this.CreateColIfNotExists("ProductQueryComment", "Comment", col => col.AsString());

        this.CreateColIfNotExists("TemplateMapping", "SourceColumn", col => col.AsString());
        this.CreateColIfNotExists("TemplateMapping", "DestinationColumn", col => col.AsString());
        this.CreateColIfNotExists("TemplateMapping", "IsIdColumn", col => col.AsBoolean());
        this.CreateColIfNotExists("TemplateMapping", "Datatype", col => col.AsString().Nullable());
        this.CreateColIfNotExists("TemplateMapping", "TemplateId", col => col.AsInt64().Nullable());

        this.CreateColIfNotExists("RmsStaging", "UDA_VALUE", col => col.AsInt64());
        this.CreateColIfNotExists("RmsStaging", "UDA_VALUE_DESC", col => col.AsString());

        this.CreateColIfNotExists("UidInitiation", "RmsIntegrationDate", col => col.AsDateTime().Nullable());
        this.CreateColIfNotExists("UidInitiation", "PlanoCode", col => col.AsString().Nullable());

        this.CreateForeignKeyColIfNotExists("ProductSubclass", "ClassId", "ProductClass");
        this.CreateColIfNotExists("HierarchicalMaster", "BusinessCode", col => col.AsString().Nullable());
        this.CreateColIfNotExists("HierarchicalMaster", "BusinessName", col => col.AsString().Nullable());
        this.CreateColIfNotExists("HierarchicalMaster", "DivisionName", col => col.AsString().Nullable());
        this.CreateColIfNotExists("HierarchicalMaster", "GroupCode", col => col.AsString().Nullable());
        this.CreateColIfNotExists("HierarchicalMaster", "GroupName", col => col.AsString().Nullable());

        this.CreateColIfNotExists("ItemCode", "UidCode", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("ItemCode", "Code", col => col.AsInt32().NotNullable());
        this.CreateColIfNotExists("ItemCode", "SourceDateCreated", col => col.AsDateTime().NotNullable());
        this.CreateColIfNotExists("ItemCodeStaging", "UidCode", col => col.AsString().NotNullable());
        this.CreateColIfNotExists("ItemCodeStaging", "Code", col => col.AsInt32().NotNullable());
        this.CreateColIfNotExists("ItemCodeStaging", "SourceDateCreated", col => col.AsDateTime().NotNullable());

        this.CreateColIfNotExists("MtrProductAttribute", "IsDataList", col => col.AsBoolean());
        this.CreateColIfNotExists("Brand", "BrandCode", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("Business", "BusinessCode", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("ProductDepartment", "GroupCode", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("ProductClass", "Department", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("ProductSubclass", "Department", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("ProductGroup", "GroupCode", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("ProductGroup", "Division", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("ProductDepartment", "DepartmentCode", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("ProductClass", "ClassCode", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("ProductSubclass", "SubClassCode", col => col.AsInt64().Nullable());

        this.CreateColIfNotExists("ProductAttribute", "IsDataList", col => col.AsBoolean().Nullable());
        this.CreateColIfNotExists("TemplateAttribute", "Order", col => col.AsInt64().Nullable());

        this.CreateColIfNotExists("UploadPicture", "ImageUrl", col => col.AsString().Nullable());

        this.CreateColIfNotExists("Product", "PictureId", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("Product", "Status", col => col.AsString().Nullable());

        this.CreateColIfNotExists("AppUser", "RoleLevel", col => col.AsString().Nullable());

        this.CreateForeignKeyColIfNotExists("MarketplaceTemplateAttribute", "ProductDepartmentId", "ProductDepartment");
        this.CreateForeignKeyColIfNotExists("MarketplaceTemplateAttribute", "MarketplaceId", "Marketplace");
        this.CreateColIfNotExists("MarketplaceTemplateAttribute", "Datatype", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("MarketplaceTemplateAttribute", "MarketplaceAttribute", col => col.AsString().Nullable());

        this.CreateColIfNotExists("MtrItemTypeCode", "Code", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("MtrProductClassificationCode", "Code", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("MtrPLCCode", "Code", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("MtrUniqueIdStatus", "Code", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("MtrProductGrouping", "Code", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("MtrPrimarySecondarySKU", "Code", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("MtrTaxCategory", "Code", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("MtrInnerCartonCode", "Code", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("MtrMasterCartonCode", "Code", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("MtrDiffGroup2Val", "Code", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("MtrMadeOfValue", "Code", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("MtrSupplierCode", "Code", col => col.AsInt64().Nullable());

        this.CreateColIfNotExists("Studio", "Code", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("Studio", "Address", col => col.AsString().Nullable());
        this.CreateColIfNotExists("Studio", "City", col => col.AsString().Nullable());
        this.CreateColIfNotExists("Studio", "Week1Capacity", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("Studio", "Week2Capacity", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("Studio", "Week3Capacity", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("Studio", "Week4Capacity", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("Studio", "Week5Capacity", col => col.AsInt64().Nullable());
        this.CreateForeignKeyColIfNotExists("Studio", "DivisionId", "Division");
        this.CreateColIfNotExists("Studio", "EffectiveFromDate", col => col.AsDateTime().Nullable());
        this.CreateColIfNotExists("Studio", "Name", col => col.AsString().Nullable());
        this.CreateColIfNotExists("Studio", "TotalCapacity", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("Studio", "RemainingCapacity", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("Studio", "Priority", col => col.AsString().Nullable());

        this.CreateColIfNotExists("ProductStudio", "StudioId", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("ProductStudio", "ProductId", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("ProductStudio", "AllocationDate", col => col.AsDateTime().Nullable());
        this.CreateColIfNotExists("ProductStudio", "Priority", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductStudio", "Itemcode", col => col.AsInt64().Nullable());

        this.CreateColIfNotExists("StudioCapacity", "Year", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("StudioCapacity", "Month", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("StudioCapacity", "Week", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("StudioCapacity", "AllocatedCapacity", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("StudioCapacity", "UsedCapacity", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("StudioCapacity", "Assigned", col => col.AsString().Nullable());
        this.CreateForeignKeyColIfNotExists("StudioCapacity", "StudioId", "Studio");

        this.CreateColIfNotExists("Product", "ItemCode", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("Product", "EanCode", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("Product", "Name", col => col.AsString().Nullable());

        this.CreateColIfNotExists("ProductImage", "ImageUrl", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "Imagetype", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "ReceivedDate", col => col.AsDateTime().Nullable());
        this.CreateColIfNotExists("ProductImage", "HandOverDate", col => col.AsDateTime().Nullable());
        this.CreateColIfNotExists("ProductImage", "HandOverStatus", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("ProductImage", "CamQCStatus", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("ProductImage", "PhotoshootStatus", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("ProductImage", "BlobImageFolderUrl", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "AllocationDate", col => col.AsDateTime().Nullable());
        this.CreateColIfNotExists("ProductImage", "InwardDate", col => col.AsDateTime().Nullable());
        this.CreateColIfNotExists("ProductImage", "StockReturn", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "StockReturnDate", col => col.AsDateTime().Nullable());
        this.CreateColIfNotExists("ProductImage", "RangeShot", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "IndividualStyle", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "CatalogShot", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "ProductReceived", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "ItemCode", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("ProductImage", "EanCode", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("ProductImage", "DivisionId", col => col.AsInt64().Nullable());

        this.CreateColIfNotExists("ProductImage", "RejectionComments", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "pictureId", col => col.AsString().Nullable());
        this.CreateColIfNotExists("PhotoShootProduct", "Name", col => col.AsString().Nullable());
        this.CreateColIfNotExists("PhotoShootProduct", "ItemCode", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("PhotoShootProduct", "EanCode", col => col.AsInt64().Nullable());
        this.CreateForeignKeyColIfNotExists("ProductImage", "ProductId", "PhotoShootProduct");

        this.CreateColIfNotExists("ProductImage", "SampleStatus", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "StatusDate", col => col.AsDateTime().Nullable());
        this.CreateColIfNotExists("ProductImage", "StockReturnComments", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "RangeShotDate", col => col.AsDateTime().Nullable());
        this.CreateColIfNotExists("ProductImage", "CamQCl2Action", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "CamQCl2Rejection", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "QualityCheck", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "CamQCl1Action", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "CamQCl1Rejection", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "ContentAction", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "ContentRejection", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "CAMQCL1QualityCheck", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "QCAction", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "CatalogShotDate", col => col.AsDateTime().Nullable());
        this.CreateColIfNotExists("ProductImage", "IndividualStyleDate", col => col.AsDateTime().Nullable());
        this.CreateColIfNotExists("ProductImage", "SubmitDate", col => col.AsDateTime().Nullable());
        this.CreateColIfNotExists("ProductImage", "CAMQCL2Submit", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "SampleStatusRejection", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "RejectedReason", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "Rejectedby", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "RejectedCount", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "RejectedItemSubmit", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "UserDivision", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "ProductSubmitDate", col => col.AsDateTime().Nullable());
        this.CreateColIfNotExists("ProductImage", "ProductSubmitBy", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "RejectedItemsubmitDate", col => col.AsDateTime().Nullable());
        this.CreateColIfNotExists("ProductImage", "RejectedItemsubmitBy", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "CAMQCl1ActionDate", col => col.AsDateTime().Nullable());
        this.CreateColIfNotExists("ProductImage", "CAMQCl1ActionBy", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "CAMQCl1QCCheckDate", col => col.AsDateTime().Nullable());
        this.CreateColIfNotExists("ProductImage", "CAMQCl1QCCheckBy", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "CAMQCl1submitDate", col => col.AsDateTime().Nullable());
        this.CreateColIfNotExists("ProductImage", "CAMQCl1submitBy", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "CAMQCl2submitDate", col => col.AsDateTime().Nullable());
        this.CreateColIfNotExists("ProductImage", "CAMQCl2submitBy", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "CAMQCl2QCCheckDate", col => col.AsDateTime().Nullable());
        this.CreateColIfNotExists("ProductImage", "CAMQCl2QCCheckBy", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "PSActionDate", col => col.AsDateTime().Nullable());
        this.CreateColIfNotExists("ProductImage", "PSActionBy", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "PSIndividualBy", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "PSCatalogBy", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "PSsubmitedBy", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "PSReturnSubmitedBy", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "PSRangeShotBy", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "PSImageUploadBy", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "PSImageUploadDate", col => col.AsDateTime().Nullable());
        this.CreateColIfNotExists("ProductImage", "RejectedDate", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "AllAction", col => col.AsString().Nullable());

        this.CreateColIfNotExists("Product", "TemplateId", col => col.AsString().Nullable());
        this.CreateJsonBColIfNotExists("Product", "UidColumnData");
        this.CreateJsonBColIfNotExists("Product", "Buyer1ColumnData");
        this.CreateJsonBColIfNotExists("Product", "Buyer2ColumnData");
        this.CreateJsonBColIfNotExists("Product", "QcColumnData");
        this.CreateJsonBColIfNotExists("Product", "PackagingColumndata");
        this.CreateJsonBColIfNotExists("Product", "FabColumndata");

        this.CreateColIfNotExists("UploadedFile", "TemplateFilestatus", col => col.AsString().Nullable());

        this.CreateColIfNotExists("StudioCapacity", "City", col => col.AsString().Nullable());

        this.CreateColIfNotExists("MtrProductype_Subtype", "Div_Code", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("MtrProductype_Subtype", "Div_Name", col => col.AsString().Nullable());
        this.CreateColIfNotExists("MtrProductype_Subtype", "Dept_Code", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("MtrProductype_Subtype", "Dept_Name", col => col.AsString().Nullable());
        this.CreateColIfNotExists("MtrProductype_Subtype", "Class_Code", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("MtrProductype_Subtype", "Class_Name", col => col.AsString().Nullable());
        this.CreateColIfNotExists("MtrProductype_Subtype", "Subclass_Code", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("MtrProductype_Subtype", "Sub_Name", col => col.AsString().Nullable());
        this.CreateColIfNotExists("MtrProductype_Subtype", "Producttype1", col => col.AsString().Nullable());
        this.CreateColIfNotExists("MtrProductype_Subtype", "Productsubtype1", col => col.AsString().Nullable());
        this.CreateColIfNotExists("MtrProductype_Subtype", "Producttype2", col => col.AsString().Nullable());
        this.CreateColIfNotExists("MtrProductype_Subtype", "Productsubtype2", col => col.AsString().Nullable());

        this.CreateColIfNotExists("PhotoshootUploadPicture", "Name", col => col.AsString().Nullable());
        this.CreateColIfNotExists("PhotoshootUploadPicture", "ImageURL", col => col.AsString().Nullable());

        this.CreateColIfNotExists("MtrHSNCode", "Code", col => col.AsInt64().Nullable());

        this.CreateColIfNotExists("ProductImage", "SamplestatusBy", col => col.AsString().Nullable());

        this.CreateColIfNotExists("ProductColumndata", "Uid", col => col.AsString().NotNullable());
        this.CreateJsonBColIfNotExists("ProductColumndata", "UidColumnData");
        this.CreateJsonBColIfNotExists("ProductColumndata", "Buyer1ColumnData");
        this.CreateJsonBColIfNotExists("ProductColumndata", "Buyer2ColumnData");
        this.CreateJsonBColIfNotExists("ProductColumndata", "QcColumnData");
        this.CreateJsonBColIfNotExists("ProductColumndata", "PackagingColumndata");
        this.CreateJsonBColIfNotExists("ProductColumndata", "FabColumndata");

        this.CreateColIfNotExists("MasterDivision", "Code", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("MasterDivision", "DivisionId", col => col.AsInt64().Nullable());

        this.CreateColIfNotExists("MtrProductColour", "DivisionId", col => col.AsString().Nullable());
        this.CreateColIfNotExists("MtrPackagingType", "DivisionId", col => col.AsString().Nullable());
        this.CreateColIfNotExists("Season", "DivisionId", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductPurpose", "DivisionId", col => col.AsString().Nullable());
        this.CreateColIfNotExists("PurchaseType", "DivisionId", col => col.AsString().Nullable());
        this.CreateColIfNotExists("PriceGrouping", "DivisionId", col => col.AsString().Nullable());
        this.CreateColIfNotExists("MtrItemTypeCode", "DivisionId", col => col.AsString().Nullable());
        this.CreateColIfNotExists("MtrProductClassificationCode", "DivisionId", col => col.AsString().Nullable());
        this.CreateColIfNotExists("MtrPLCCode", "DivisionId", col => col.AsString().Nullable());
        this.CreateColIfNotExists("MtrProductGrouping", "DivisionId", col => col.AsString().Nullable());
        this.CreateColIfNotExists("MtrInnerCartonCode", "DivisionId", col => col.AsString().Nullable());
        this.CreateColIfNotExists("MtrMasterCartonCode", "DivisionId", col => col.AsString().Nullable());
        this.CreateColIfNotExists("MtrSupplierCode", "DivisionId", col => col.AsString().Nullable());

        this.CreateColIfNotExists("Grn", "ItemCode", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("Grn", "Grn_Location", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("Grn", "Grn_Date", col => col.AsDateTime().Nullable());

        this.CreateColIfNotExists("TransferOrder", "ItemCode", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("TransferOrder", "From_Loc", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("TransferOrder", "To_Loc", col => col.AsInt64().Nullable());
        this.CreateColIfNotExists("TransferOrder", "TO_Date", col => col.AsDateTime().Nullable());

        this.CreateColIfNotExists("ProductImage", "Grnstatus", col => col.AsString().Nullable());
        this.CreateColIfNotExists("ProductImage", "ProductTOStatus", col => col.AsString().Nullable());

        this.CreateColIfNotExists("Warehouse", "Code", col => col.AsInt64().Nullable());

        this.Execute.Sql(@"CREATE SEQUENCE IF NOT EXISTS market_template_sequence
                                INCREMENT 1
                                START 1;");
        this.CreateColIfNotExists("Season", "Code", col => col.AsString());

        this.Execute.Sql("SELECT add_constraint('marketplace', 'marketplace_name_uq_idx', 'ALTER TABLE marketplace ADD CONSTRAINT marketplace_name_uq_idx UNIQUE (name);');");
    }

    public override void Down()
    {
    }
}