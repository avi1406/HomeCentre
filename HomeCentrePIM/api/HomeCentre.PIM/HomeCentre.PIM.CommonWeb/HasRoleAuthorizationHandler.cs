using HomeCentre.PIM.BAL.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace HomeCentre.PIM.CommonWeb
{
    public class HasRoleAuthorizationHandler : AuthorizationHandler<HasRoleRequirement>
    {
        private readonly IDbCache _cache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        public HasRoleAuthorizationHandler(IDbCache cache,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration)
        {
            _cache = cache;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
            HasRoleRequirement requirement)
        {
            try
            {
                var request = _httpContextAccessor.HttpContext?.Request;
                var token = request.GetHeaderValue("token") ??
                            request.GetCookieValue("token") ?? request.GetQueryStringValue("token");
                if (token == null)
                {
                    context.Fail();
                }

                var allowAnonymous = false;
                var endPointMeta = _httpContextAccessor.HttpContext?.GetEndpoint();
                allowAnonymous = endPointMeta?.Metadata.Any(x => x is AllowAnonymousAttribute) ?? false;

                if (allowAnonymous)
                {
                    context.Succeed(requirement);
                    return;
                }

                var principal = token.ValidateToken(_configuration.GetSection("Jwt-Secret").Value,
                    _configuration.GetSection("Jwt-Issuer").Value,
                    _configuration.GetSection("Jwt-Audience").Value);

                var userId = principal.GetClaim("UserId");

                if (userId != null)
                {
                    var appUser =
                        await _cache.GetUser(
                            long.Parse(
                                userId));

                    if (appUser == null)
                    {
                        context.Fail();
                        return;
                    }
                }
                else
                {
                    context.Fail();
                }

                context.Succeed(requirement);
                return;
            }
            catch (Exception ex)
            {
                context.Fail();
            }
        }

        private static int? GetClientId(HttpRequest request)
        {
            var clientId = 0;
            if (request.Headers.ContainsKey("clientid"))
            {
                var clientIdHeader = request.Headers["clientid"];
                int.TryParse(clientIdHeader, out clientId);
            }

            if (clientId != 0)
            {
                return clientId;
            }

            if (request.Cookies.ContainsKey("clientid"))
            {
                var clientIdCookie = request.Cookies["clientid"];
                if (!int.TryParse(clientIdCookie, out clientId))
                {
                    return null;
                }
            }
            else
            {
                return null;
            }

            return clientId;
        }
    }
}