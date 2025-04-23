using System;
using Dapper;

namespace HomeCentre.PIM.Common.PostgresTypes
{
    public class PostgresLowerCaseTableNameResolver : SimpleCRUD.ITableNameResolver
    {
        public string ResolveTableName(Type type)
        {
            return type.Name.ToLower();
        }
    }
}