using System.Collections.Generic;
using System.Data;
using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;

namespace HomeCentre.PIM.Common;

public class DictionaryObjectValueHandler: SqlMapper.TypeHandler<Dictionary<string, object>>
{
    public override void SetValue(IDbDataParameter parameter, Dictionary<string, object> value)
    {
        parameter.Value = JsonConvert.SerializeObject(value);
        ((NpgsqlParameter)parameter).NpgsqlDbType = NpgsqlDbType.Jsonb;
    }

    public override Dictionary<string, object> Parse(object value)
    {
        var json = value.ToString();
        return json == null ? null : JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
    }
}