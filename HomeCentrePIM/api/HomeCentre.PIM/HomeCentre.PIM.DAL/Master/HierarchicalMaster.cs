using DocumentFormat.OpenXml.Office.CoverPageProps;

namespace HomeCentre.PIM.DAL.Master;

public class HierarchyValidation
{
    public string Attribute { get; set; }

    public string Value { get; set; }

    public string Templateid { get; set; }
}

public class NameIdCollection
{
    public string Name { get; set; }

    public int Id { get; set; }

    public string Attribute { get; set; }
}

public class HierarchicalMechindise
{
    public int BusinessCode { get; set; }

    public string BusinessName { get; set; }

    public int DivisionCode { get; set; }

    public string DivName { get; set; }

    public int GroupCode { get; set; }

    public string GroupName { get; set; }

    public int DeptCode { get; set; }

    public string DepartName { get; set; }

    public int ClassCode { get; set; }

    public string ClassName { get; set; }

    public int SubclassCode { get; set; }

    public string SubName { get; set; }
}

public class MerchandiseHierarchy
{
    public long Business { get; set; }

    public string Business_Name { get; set; }

    public long Division { get; set; }

    public string Div_Name { get; set; }

    public long Group_No { get; set; }

    public string Group_Name { get; set; }

    public long Dept { get; set; }

    public string Dept_Name { get; set; }

    public long Class { get; set; }

    public string Class_Name { get; set; }

    public long Subclass { get; set; }

    public string Sub_Name { get; set; }

    public bool Active_Ind { get; set; }
}

public class HierarchicalMaster : MasterEntity
{
    public string BusinessCode { get; set; }

    public string BusinessName { get; set; }

    public string DivisionCode { get; set; }

    public string DivisionName { get; set; }

    public string GroupCode { get; set; }

    public string GroupName { get; set; }

    public string DepartmentCode { get; set; }

    public string DepartmentName { get; set; }

    public string ClassCode { get; set; }

    public string ClassName { get; set; }

    public string SubclassCode { get; set; }

    public string SubclassName { get; set; }

    public string DepartmentShortCode { get; set; }
}

public class ProductdeptMaster : MasterEntity
{
    public string Code { get; set; }

    public long Division { get; set; }
}

public class HierarchicalMasterData
{
    public string BusinessCode { get; set; }

    public string BusinessName { get; set; }

    public string DivisionCode { get; set; }

    public string DivisionName { get; set; }

    public string GroupCode { get; set; }

    public string GroupName { get; set; }

    public string DepartmentCode { get; set; }

    public string DepartmentName { get; set; }

    public string ClassCode { get; set; }

    public string ClassName { get; set; }

    public string SubclassCode { get; set; }

    public string SubclassName { get; set; }

    public bool IsActive { get; set; }
}

public class TemplateData
{
    public string TemplateName { get; set; }

    public long DivisionId { get; set; }

    public bool Isactive { get; set; }
}

public class ClassNameMasterData
{
    public string Classname { get; set; }

    public string ClassCode { get; set; }

    public string DepartmentCode { get; set; }

    public bool IsActive { get; set; }
}

public class DepartmentMasterData
{
    public string DepartmentName { get; set; }

    public string DepartmentCode { get; set; }

    public string GroupCode { get; set; }

    public bool IsActive { get; set; }
}

public class DivisionMasterData
{
    public string DivisionName { get; set; }

    public string DivisionCode { get; set; }

    public bool IsActive { get; set; }
}

public class GroupData
{
    public string GroupName { get; set; }

    public string GroupCode { get; set; }

    public string DivisionCode { get; set; }

    public bool IsActive { get; set; }
}

public class SubClassData
{
    public string SubClassName { get; set; }

    public string SubClassCode { get; set; }

    public string DepartmentCode { get; set; }

    public string ClassCode { get; set; }

    public bool IsActive { get; set; }
}

public class AttributeMasterData
{
    public string AttributeName { get; set; }

    public string DataType { get; set; }

    public string IsRequired { get; set; }

    public string Division { get; set; }
}

public class BrandMasterData
{
    public string BrandName { get; set; }

    public string BrandCode { get; set; }

    public bool IsActive { get; set; }
}

public class BusinessMasterData
{
    public string BusinessName { get; set; }

    public string BusinessCode { get; set; }

    public bool IsActive { get; set; }
}

public class ProductColourData
{
    public string ProductColourname { get; set; }

    public long Code { get; set; }

    public bool IsActive { get; set; }

    public string Divisionid { get; set; }
}

public class SetpcsData
{
    public string SetpcsName { get; set; }

    public long Code { get; set; }

    public bool IsActive { get; set; }
}

public class LeadTimeData
{
    public string LeadTimeName { get; set; }

    public bool IsActive { get; set; }
}

public class ItemShapeData
{
    public string ItemShapeName { get; set; }

    public bool IsActive { get; set; }
}

public class PackagingTypeData
{
    public string PackagingTypeName { get; set; }

    public string PackagingTypeCode { get; set; }

    public string DivisionId { get; set; }

    public bool IsActive { get; set; }
}

public class PackagingTypeDataModel
{
    public string PackagingTypeName { get; set; }

    public string Code { get; set; }

    public string Division { get; set; }

    public bool IsActive { get; set; }
}

public class FinishData
{
    public string FinishName { get; set; }

    public bool IsActive { get; set; }
}

public class PrimaryFurnitureMaterialData
{
    public string PrimaryFurnitureMaterialName { get; set; }

    public bool IsActive { get; set; }
}

public class SecondaryFurnitureMaterialData
{
    public string SecondaryFurnitureMaterialName { get; set; }

    public bool IsActive { get; set; }
}

public class Producttype1Data
{
    public string Producttype1Name { get; set; }

    public bool IsActive { get; set; }
}

public class Producttype2Data
{
    public string Producttype2Name { get; set; }

    public bool IsActive { get; set; }
}

public class ProductSubType1Data
{
    public string ProductSubType1Name { get; set; }

    public bool IsActive { get; set; }
}

public class ProductSubType2Data
{
    public string ProductSubType2Name { get; set; }

    public bool IsActive { get; set; }
}

public class ProductPurposeData
{
    public string ProductPurposeName { get; set; }

    public string Code { get; set; }

    public bool IsActive { get; set; }

    public string Divisionid { get; set; }
}

public class PurchaseTypeData
{
    public string PurchaseTypeName { get; set; }

    public long Code { get; set; }

    public bool IsActive { get; set; }

    public string Divisionid { get; set; }
}

public class PriceGroupingData
{
    public string PriceGroupingName { get; set; }

    public string Code { get; set; }

    public bool IsActive { get; set; }

    public string Divisionid { get; set; }
}

public class SeasonData
{
    public string SeasonName { get; set; }

    public string SeasonCode { get; set; }

    public bool IsActive { get; set; }

    public string Divisionid { get; set; }
}

public class UidTemplateAttrvalidation
{
    public string Table { get; set; }

    public string Value { get; set; }
}

public class ItemCodeCreation
{
    public string Attribute { get; set; }

    public string Color { get; set; }

    public string Value { get; set; }

    public string AttributeName { get; set; }
}

public class ItemTypeCodeData
{
    public string ItemTypeCodeName { get; set; }

    public long Code { get; set; }

    public bool Isactive { get; set; }

    public string Divisionid { get; set; }
}

public class ProductClassificationCodeData
{
    public string ProductClassificationCodeName { get; set; }

    public long Code { get; set; }

    public bool Isactive { get; set; }

    public string DivisionId { get; set; }
}

public class ProductClassificationCodeDataModel
{
    public string ProductClassificationCodeName { get; set; }

    public long Code { get; set; }

    public bool Isactive { get; set; }

    public string Division { get; set; }
}

public class PLCCodeData
{
    public string PLCCodeName { get; set; }

    public long Code { get; set; }

    public bool IsActive { get; set; }

    public string DivisionId { get; set; }
}

public class PLCCodeDataModel
{
    public string PLCCodeName { get; set; }

    public long Code { get; set; }

    public bool IsActive { get; set; }

    public string Division { get; set; }
}

public class UniqueIdStatusData
{
    public string UniqueIdStatusName { get; set; }

    public long Code { get; set; }

    public bool IsActive { get; set; }
}

public class ProductGroupingData
{
    public string ProductGroupingName { get; set; }

    public long Code { get; set; }

    public bool IsActive { get; set; }

    public string DivisionId { get; set; }
}

public class ProductGroupingDataModel
{
    public string ProductGroupingName { get; set; }

    public long Code { get; set; }

    public bool IsActive { get; set; }

    public string Division { get; set; }
}

public class PrimarySecondarySKUData
{
    public string PrimarySecondaryName { get; set; }

    public long Code { get; set; }

    public bool IsActive { get; set; }
}

public class CountryOfOriginData
{
    public string Country_Of_Origin_Code { get; set; }

    public string Country_Of_Origin_Name { get; set; }

    public bool IsActive { get; set; }
}

public class CurrencyData
{
    public string Currency_Code { get; set; }

    public string Currency_Name { get; set; }

    public bool IsActive { get; set; }
}

public class TaxCategoryData
{
    public string TaxCategoryName { get; set; }

    public long Code { get; set; }

    public bool IsActive { get; set; }
}

public class InnerCartonCodeData
{
    public string InnerCartonCodeName { get; set; }

    public long Code { get; set; }

    public bool IsActive { get; set; }

    public string DivisionId { get; set; }
}

public class InnerCartonCodeDataModel
{
    public string InnerCartonCodeName { get; set; }

    public long Code { get; set; }

    public bool IsActive { get; set; }

    public string Division { get; set; }
}

public class MasterCartonCodeData
{
    public string MasterCartonCodeName { get; set; }

    public long Code { get; set; }

    public bool IsActive { get; set; }

    public string DivisionId { get; set; }
}

public class MasterCartonCodeDataModel
{
    public string MasterCartonCodeName { get; set; }

    public long Code { get; set; }

    public bool IsActive { get; set; }

    public string Division { get; set; }
}

public class DiffGroupData
{
    public string DiffGroupName { get; set; }

    public long Code { get; set; }

    public bool IsActive { get; set; }
}

public class MadeOfValueData
{
    public string MadeOfValueName { get; set; }

    public long Code { get; set; }

    public bool IsActive { get; set; }
}

public class SupplierCodeData
{
    public string SupplierCodeName { get; set; }

    public long Code { get; set; }

    public bool IsActive { get; set; }

    public string DivisionId { get; set; }
}

public class SupplierCodeDataModel
{
    public string SupplierCodeName { get; set; }

    public long Code { get; set; }

    public bool IsActive { get; set; }

    public string Division { get; set; }
}

public class HsnCodeData
{
    public long Code { get; set; }

    public string HsnName { get; set; }

    public bool IsActive { get; set; }
}

public class BrandTypeData
{
    public long Code { get; set; }

    public string BrandTypeName { get; set; }

    public bool IsActive { get; set; }
}

public class ProductTypeHierarchy
{
    public long Div_Code { get; set; }

    public string Div_Name { get; set; }

    public long Dept_Code { get; set; }

    public string Dept_Name { get; set; }

    public long Class_Code { get; set; }

    public string Class_Name { get; set; }

    public long Subclass_Code { get; set; }

    public string Subclass_Name { get; set; }

    public string Product_Type_1 { get; set; }

    public string Product_Subtype_1 { get; set; }

    public string Product_Type_2 { get; set; }

    public string Product_Subtype_2 { get; set; }
}

public class RMSSeason
{
    public long Code { get; set; }

    public string Name { get; set; }

    public bool IsActive { get; set; }
}

public class WareHouse
{
    public string Name { get; set; }

    public long Code { get; set; }

    public bool IsActive { get; set; }
}