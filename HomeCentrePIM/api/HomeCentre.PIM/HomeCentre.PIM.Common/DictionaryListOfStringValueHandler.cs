using System.Collections.Generic;
using System.Data;
using Dapper;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;

namespace HomeCentre.PIM.Common;

public class DictionaryListOfStringValueHandler: SqlMapper.TypeHandler<Dictionary<string, List<string>>>
{
    public override void SetValue(IDbDataParameter parameter, Dictionary<string,  List<string>> value)
    {
        parameter.Value = JsonConvert.SerializeObject(value);
        ((NpgsqlParameter)parameter).NpgsqlDbType = NpgsqlDbType.Jsonb;
    }

    public override Dictionary<string,  List<string>> Parse(object value)
    {
        var json = value.ToString();
        return json == null ? null : JsonConvert.DeserializeObject<Dictionary<string,  List<string>>>(json);
    }
}