using System.Security.Claims;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;

namespace HomeCentre.PIM.CommonWeb
{
    public class HttpUserContext : IUserContext
    {
        private const string AuthHeader = "token";
        private readonly IDbCache _cache;
        private readonly IConfiguration _configuration;
        private readonly HttpContext _httpContext;

        public HttpUserContext(IHttpContextAccessor httpContextAccessor, IConfiguration configuration, IDbCache cache)
        {
            _httpContext = httpContextAccessor.HttpContext;
            _configuration = configuration;
            _cache = cache;
        }

        public async Task<long> GetDepartmentId()
        {
            var departmentId = GetPrincipal().GetClaim("DepartmentId");
            long dept = 0;
            foreach (var item in departmentId.Split(","))
            {
                if (long.Parse(item) != 0)
                {
                   dept = long.Parse(item);
                }
                else
                {
                    dept = 0;
                }

                return dept;
            }

            return 0;
        }

        public async Task<string> GetDepartmentIds()
        {
            return await Task.FromResult(GetPrincipal().GetClaim("DepartmentId"));
        }

        public string GetIpAddress()
        {
            // return _httpContext.Connection.RemoteIpAddress.ToString();
            return _httpContext.Features.Get<IHttpConnectionFeature>()?.RemoteIpAddress.ToString();
        }

        public async Task<string> GetName()
        {
            return await Task.FromResult(GetPrincipal().GetClaim("FullName"));
        }

        public async Task<long> GetPersonaId()
        {
            long.TryParse(GetPrincipal().GetClaim("PersonaId"), out var personaId);
            return await Task.FromResult(personaId);
        }

        public async Task<string> GetRoleId()
        {
            return await Task.FromResult(GetPrincipal().GetClaim("RoleId"));
        }

        public async Task<string> GetRoleLevel()
        {
            return await Task.FromResult(GetPrincipal().GetClaim("RoleLevel"));
        }

        public async Task<string> GetUserEmail()
        {
            return await Task.FromResult(GetPrincipal().GetClaim(ClaimTypes.Email));
        }

        public async Task<long> GetUserId()
        {
            if (!this.IsValid())
            {
                return 1;
            }
            
            long.TryParse(GetPrincipal().GetClaim("UserId"), out var userId);
            return await Task.FromResult(userId);
        }

        public async Task<bool> IsAuthorized()
        {
            try
            {
                await this.GetUserId();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool IsValid()
        {
            return _httpContext?.Request != null;
        }

        private string GetKeyValue(string key, bool throwError = true)
        {
            if (_httpContext.Request.Headers.TryGetValue(key, out var headerValues))
            {
                return headerValues.FirstOrDefault();
            }

            if (_httpContext.Request.Cookies.TryGetValue(key, out var cookieValues))
            {
                return cookieValues;
            }

            if (_httpContext.Request.Query.TryGetValue(key, out var queryStringValues))
            {
                return queryStringValues.FirstOrDefault();
            }

            if (throwError)
            {
                throw new KeyNotFoundException("Auth Header is missing");
            }

            return null;
        }

        private ClaimsPrincipal GetPrincipal()
        {
            var token = GetToken();
            return token.ValidateToken(_configuration["Jwt-Secret"],
                _configuration["Jwt-Issuer"], _configuration["Jwt-Audience"]);   
        }

        private string GetToken()
        {
            return GetKeyValue(AuthHeader);
        }
    }
}