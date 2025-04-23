using System.Data;
using Dapper;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;

namespace HomeCentre.PIM.Common;

public class JObjectTypeHandler : SqlMapper.TypeHandler<JObject>
{
    public JObjectTypeHandler()
    {
    }

    public static JObjectTypeHandler Instance { get; } = new JObjectTypeHandler();

    public override JObject Parse(object value)
    {
        var json = value.ToString();
        return json == null ? null : JObject.Parse(json);
    }

    public override void SetValue(IDbDataParameter parameter, JObject value)
    {
        parameter.Value = value?.ToString(Newtonsoft.Json.Formatting.None);
        ((NpgsqlParameter)parameter).NpgsqlDbType = NpgsqlDbType.Jsonb;
    }
}