using System.Reflection;
using Dapper;

namespace HomeCentre.PIM.Common.PostgresTypes
{
    public class PostgresLowerCaseColumnNameResolver : SimpleCRUD.IColumnNameResolver
    {
        public string ResolveColumnName(PropertyInfo propertyInfo)
        {
            return propertyInfo.Name.ToLower();
        }
    }
}