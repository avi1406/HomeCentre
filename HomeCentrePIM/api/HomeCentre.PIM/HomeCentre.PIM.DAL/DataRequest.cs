using Dapper;

namespace HomeCentre.PIM.DAL;

public class DataRequest: Entity
{
    public string Name { get; set; }

    public long TemplateId { get; set; }

    [Editable(false)]
    [IgnoreSelect]
    [IgnoreInsert]
    [IgnoreUpdate]
    [ReadOnly(true)]
    public string[] ItemCodes { get; set; }
}