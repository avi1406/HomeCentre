using Dapper;

namespace HomeCentre.PIM.DAL.Master;

[Table("appconfig")]
public class AppSettings : MasterEntity
{
    public string Value { get; set; }

    public string JsonValue { get; set; }
}