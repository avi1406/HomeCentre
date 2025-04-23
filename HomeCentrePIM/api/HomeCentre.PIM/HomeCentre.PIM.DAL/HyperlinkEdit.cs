using Dapper;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.Common.MetaAttributes;

namespace HomeCentre.PIM.DAL;
public class HyperlinkEdit : Entity
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Email { get; set; }

    public string? Mobile { get; set; }

    public long Id { get; set; }

    public string? ProfilePicture { get; set; }

    public long? DepartmentId { get; set; }

    public long? RoleId { get; set; }

    public string? Rolelevel { get; set; }

    public bool? IsActive { get; set; }
}