using System.Data;

namespace HomeCentre.PIM.Common.Interfaces
{
    public interface IDbConnectionFactory
    {
        IDbConnection GetAppDbConnection(IDbTransaction transaction);

        IDbConnection GetTenantDbHostConnection(IDbTransaction transaction);

        IDbConnection GetDbConnection(string connectionString);
    }
}