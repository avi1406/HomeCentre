using System.Collections.Generic;
using NpgsqlTypes;

namespace HomeCentre.PIM.Common.PostgresTypes
{
    public class DbTypeMapping
    {
        public static readonly IDictionary<string, NpgsqlDbType> NpgsqlDbTypesMapping =
            new Dictionary<string, NpgsqlDbType>
            {
                {"bool", NpgsqlDbType.Boolean},
                {"int2", NpgsqlDbType.Smallint},
                {"int4", NpgsqlDbType.Integer},
                {"integer", NpgsqlDbType.Integer},
                {"int8", NpgsqlDbType.Bigint},
                {"bigint", NpgsqlDbType.Bigint},
                {"float4", NpgsqlDbType.Real},
                {"float8", NpgsqlDbType.Double},
                {"numeric", NpgsqlDbType.Numeric},
                {"money", NpgsqlDbType.Money},
                {"text", NpgsqlDbType.Text},
                {"varchar", NpgsqlDbType.Varchar},
                {"char", NpgsqlDbType.Char},
                {"citext", NpgsqlDbType.Citext},
                {"json", NpgsqlDbType.Json},
                {"jsonb", NpgsqlDbType.Jsonb},
                {"xml", NpgsqlDbType.Xml},
                {"point", NpgsqlDbType.Point},
                {"lseg", NpgsqlDbType.LSeg},
                {"path", NpgsqlDbType.Path},
                {"polygon", NpgsqlDbType.Polygon},
                {"line", NpgsqlDbType.Line},
                {"circle", NpgsqlDbType.Circle},
                {"box", NpgsqlDbType.Box},
                {"bit", NpgsqlDbType.Bit},
                {"varbit", NpgsqlDbType.Varbit},
                {"hstore", NpgsqlDbType.Hstore},
                {"uuid", NpgsqlDbType.Uuid},
                {"cidr", NpgsqlDbType.Cidr},
                {"inet", NpgsqlDbType.Inet},
                {"macaddr", NpgsqlDbType.MacAddr},
                {"tsquery", NpgsqlDbType.TsQuery},
                {"tsvector", NpgsqlDbType.TsVector},
                {"date", NpgsqlDbType.Date},
                {"interval", NpgsqlDbType.Interval},
                {"timestamp", NpgsqlDbType.Timestamp},
                {"timestamptz", NpgsqlDbType.TimestampTz},
                {"timestamp with time zone", NpgsqlDbType.TimestampTz},
                {"timestamp without time zone", NpgsqlDbType.Timestamp},
                {"time", NpgsqlDbType.Time},
                {"timetz", NpgsqlDbType.TimeTz},
                {"bytea", NpgsqlDbType.Bytea},
                {"oid", NpgsqlDbType.Oid},
                {"xid", NpgsqlDbType.Xid},
                {"cid", NpgsqlDbType.Cid},
                {"oidvector", NpgsqlDbType.Oidvector},
                {"name", NpgsqlDbType.Name},
                {"bpchar", NpgsqlDbType.Char},
                {"boolean", NpgsqlDbType.Boolean}
            };
    }
}