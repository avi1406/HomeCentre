using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HomeCentre.PIM.CommonWeb
{
    /// <summary>
    /// Authorization filter for multiple policies
    /// </summary>
    public class MultiplePoliciesAuthorizeFilter : IAsyncAuthorizationFilter
    {
        private readonly IAuthorizationService _authorization;

        /// <summary>
        /// Constructor for multiple policy authorization
        /// </summary>
        /// <param name="policies">Policy list  separated by comma</param>
        /// <param name="isAnd">should all policies match or any one, by default it is any</param>
        /// <param name="authorization"></param>
        public MultiplePoliciesAuthorizeFilter(string policies, bool isAnd, IAuthorizationService authorization)
        {
            Policies = policies;
            IsAnd = isAnd;
            _authorization = authorization;
        }

        /// <summary>
        /// Property to store condition type
        /// </summary>
        public bool IsAnd { get; private set; }

        /// <summary>
        /// List of Policies as comma separated string
        /// </summary>
        public string Policies { get; private set; }
        
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var policies = Policies.Split(",").ToList();
            if (IsAnd)
            {
                foreach (var policy in policies)
                {
                    var authorized = await _authorization.AuthorizeAsync(context.HttpContext.User, policy);

                    if (authorized is null || authorized.Succeeded)
                    {
                        continue;
                    }

                    context.Result = new ForbidResult();
                    return;
                }
            }
            else
            {
                foreach (var policy in policies)
                {
                    var authorized = await _authorization.AuthorizeAsync(context.HttpContext.User, policy);
                    if (authorized.Succeeded)
                    {
                        return;
                    }
                }

                context.Result = new UnauthorizedResult();
            }
        }
    }
}