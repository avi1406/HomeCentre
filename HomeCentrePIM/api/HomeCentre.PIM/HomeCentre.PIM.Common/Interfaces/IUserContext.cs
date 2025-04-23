using System.Threading.Tasks;

namespace HomeCentre.PIM.Common.Interfaces
{
    public interface IUserContext
    {
        Task<long> GetUserId();

        Task<long> GetDepartmentId();

        Task<string> GetDepartmentIds();

        Task<string> GetRoleId();

        Task<string> GetRoleLevel();

        Task<bool> IsAuthorized();

        Task<string> GetUserEmail();

        Task<string> GetName();

        string GetIpAddress();
    }
}