namespace HomeCentre.PIM.DAL.Master;

public class AppUserRole : MasterEntity
{
    public long AppUserId { get; set; }

    public long RoleId { get; set; }

    public long DepartmentId { get; set; }
}