using System.Security.Claims;
using System.Threading.Tasks;
using HomeCentre.PIM.BAL.Model.Account;
using HomeCentre.PIM.DAL;

namespace HomeCentre.PIM.BAL.Contracts
{
    public interface IAuthenticationProvider
    {
        Task<AppUser> GenerateToken(LoginViewModel request);

        ClaimsPrincipal ValidateToken(string token);

        Task RegisterUsers(AppUser[] users);
    }
}