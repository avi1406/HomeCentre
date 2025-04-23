using Microsoft.AspNetCore.Authorization;

namespace HomeCentre.PIM.CommonWeb
{
    /// <summary>
    /// HasRoleRequirement 
    /// </summary>
    public class HasRoleRequirement : IAuthorizationRequirement
    {
        public HasRoleRequirement(string[] roles)
        {
            Roles = roles;
        }

        public string[] Roles { get; set; }
    }
}