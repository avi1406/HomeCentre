using Dapper;
using HomeCentre.PIM.Common.MetaAttributes;

namespace HomeCentre.PIM.DAL;

[SearchField(new[] {"Name"})]
public class Template: Entity
{
    public string Name { get; set; }

    public long? DivisionId { get; set; }

    public bool IsUidTemplate { get; set; }

    public long TemplateStatusId { get; set; }
    
    [Editable(false)]
    [IgnoreInsert]
    [IgnoreSelect]
    [IgnoreUpdate]
    public long[] RoleIds { get; set; }
    
    [Editable(false)]
    [IgnoreInsert]
    [IgnoreSelect]
    [IgnoreUpdate]
    public long? AppUserId { get; set; }
}