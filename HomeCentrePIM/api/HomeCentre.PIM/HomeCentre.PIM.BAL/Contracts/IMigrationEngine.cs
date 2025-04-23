using System.Threading.Tasks;

namespace HomeCentre.PIM.BAL.Contracts
{
    public interface IMigrationEngine
    {
        Task StartMigration();
    }
}