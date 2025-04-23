namespace HomeCentre.PIM.DAL.Master;

public class ProductDepartment: MasterEntity
{
    public string Code { get; set; }

    public long DivisionId { get; set; }

    public long GroupCode { get; set; }

    public long Departmentcode { get; set; }
}