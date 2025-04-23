using System.Threading.Tasks;

namespace HomeCentre.PIM.Common.Interfaces
{
    public interface IAppConfiguration
    {
        Task<string> GetValue(string key);

        Task Reload();
    }
}