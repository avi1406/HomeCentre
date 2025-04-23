using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Implementations.B2C.Graph;
using HomeCentre.PIM.BAL.Implementations.B2C.Graph.Models;
using HomeCentre.PIM.BAL.Implementations.B2C.Graph.Services;
using HomeCentre.PIM.BAL.Model.Account;
using HomeCentre.PIM.BAL.Model.Email;
using HomeCentre.PIM.Common;
using HomeCentre.PIM.DAL;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace HomeCentre.PIM.BAL.Implementations.B2C
{
    public class B2CAuthenticationProvider : Contracts.IAuthenticationProvider
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<B2CAuthenticationProvider> _logger;
        private readonly IMailEngine _mailEngine;
        private readonly IDbCache _cache;

        public B2CAuthenticationProvider(IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IMailEngine mailEngine,
            IDbCache cache,
            ILogger<B2CAuthenticationProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _mailEngine = mailEngine;
            _logger = logger;
            _cache = cache;
        }

        public async Task<AppUser> GenerateToken(LoginViewModel request)
        {
            var b2CToken = request.Token;
            ClaimsPrincipal principal = null;
            var email = string.Empty;
            var config = await _cache.FindAppSettings("IdentityProvider");

            if (config.Value.ToLower() == "b2c")
            {
                principal = await GetClaimsPrincipal(b2CToken);
                email = principal.Claims.FirstOrDefault(x => x.Type == "emails")?.Value;
            }
            else
            {
                principal = await GetPricipalForAd(b2CToken);
                email = principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
            }

            var b2Cobjectid = principal.Claims
                .FirstOrDefault(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
            if (email == null)
            {
                throw new MissingPrimaryKeyException("Email is missing in claims.");
            }

            var appUser = new AppUser
            {
                Email = email.ToLower(),
                Domain = email.GetEmailHost(),
                FirstName = principal.GetClaim(ClaimTypes.GivenName),
                Address =
                    $"{principal.GetClaim("streetAddress")} {principal.GetClaim("city")} {principal.GetClaim("postalCode")} {principal.GetClaim("state")}",
                LastName = principal.GetClaim(ClaimTypes.Surname),
                Mobile = principal.GetClaim("extension_MobileNo"),
                B2CObjectId = b2Cobjectid
            };
            return appUser;
        }

        public async Task RegisterUsers(AppUser[] users)
        {
            var graphLib = new B2CGraphLibrary(_configuration);
            var templateModels = new List<UserInviteModel>();
            await graphLib.GraphClient.BulkCreate(
                new UsersModel
                {
                    Users = users.Select(x =>
                    {
                        // var pwd = UtilExtensions.GenerateRandomPassword();
                        templateModels.Add(new UserInviteModel
                        {
                            Email = x.Email,
                            Password = x.Password
                        });
                        return new UserModel
                        {
                            GivenName = x.FirstName,
                            Surname = x.LastName,
                            DisplayName = x.Name ?? x.Email ?? $"{x.FirstName} {x.LastName}",
                            Identities = new List<ObjectIdentity>
                            {
                            new ObjectIdentity()
                            {
                                SignInType = "emailAddress",
                                Issuer = _configuration["B2C-Tenant"],
                                IssuerAssignedId = x.Email,
                            },
                            },
                            PasswordProfile = new PasswordProfile()
                            {
                                Password = x.Password,
                                ForceChangePasswordNextSignIn = true
                            },
                            Password = x.Password,

                            // PasswordPolicies = "DisablePasswordExpiration"
                        };
                    }).ToArray(),
                }, _configuration["B2C-Tenant"]);

            foreach (var templateModel in templateModels)
            {
                try
                {
                    await _mailEngine.SendEmail("UserInviteTemplate", new UserInviteModel
                    {
                        Email = templateModel.Email,
                        Password = templateModel.Password,
                        LoginUrl = _configuration["AppUrl"]
                    }, "Welcome to HC.PIM", null, new[] { templateModel.Email });
                }
                catch (Exception e)
                {
                    this._logger.LogError(e, "Error while sending email");
                }
            }
        }

        public ClaimsPrincipal ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var jwtToken = tokenHandler.ReadToken(token) as JwtSecurityToken;

            if (jwtToken == null)
            {
                return null;
            }

            var secret = _configuration["Jwt-Secret"];
            var symmetricKey = Convert.FromBase64String(secret);

            var validationParameters = new TokenValidationParameters
            {
                RequireExpirationTime = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidAudiences = new List<string> { _configuration["Jwt-Audience"] },
                ValidIssuer = _configuration["Jwt-Issuer"],
                IssuerSigningKey = new SymmetricSecurityKey(symmetricKey)
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);

            return principal;
        }

        private static byte[] FromBase64Url(string base64Url)
        {
            var padded = base64Url.Length % 4 == 0
                ? base64Url
                : base64Url + "====".Substring(base64Url.Length % 4);
            var base64 = padded.Replace("_", "/")
                .Replace("-", "+");
            return Convert.FromBase64String(base64);
        }

        private async Task<ClaimsPrincipal> GetClaimsPrincipal(string b2CToken)
        {
            var jwtToken = new JwtSecurityToken(b2CToken);
            var httpClient = this._httpClientFactory.CreateClient();
            var b2CKeysBaseAddress = _configuration["B2C-BaseAddress"];
            var issuer = _configuration["B2C-Issuer"];
            var b2CTenant = _configuration["B2C-Tenant"];
            httpClient.BaseAddress = new Uri(b2CKeysBaseAddress);
            var policy = _configuration["B2C-LoginPolicy"];
            var response = await httpClient.GetAsync($"/{b2CTenant}/discovery/v2.0/keys?p={policy}");
            var data = await response.Content.ReadAsStringAsync();
            var keys = JsonConvert.DeserializeObject<B2CTokenKeys>(data);
            var keyOfToken = keys.Keys.FirstOrDefault(x => x.Kid == jwtToken.Header["kid"].ToString());
            var rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(
                new RSAParameters
                {
                    Modulus = FromBase64Url(keyOfToken.N),
                    Exponent = FromBase64Url(keyOfToken.E)
                });
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateLifetime = true,
                ValidateAudience = false,
                IssuerSigningKey = new RsaSecurityKey(rsa)
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.ValidateToken(b2CToken, validationParameters, out var validatedToken);
        }

        private async Task<ClaimsPrincipal> GetPricipalForAd(string request)
        {
            IdentityModelEventSource.ShowPII = true; // enable for debugging errors
            var tenantId = _configuration["ApplicationtenantID"];
            var myAudience = _configuration["ApplicationclientID"];
            var myIssuer = string.Format(CultureInfo.InvariantCulture, "https://sts.windows.net/{0}/",
                tenantId);
            var stsDiscoveryEndpoint = string.Format(CultureInfo.InvariantCulture,
                "https://login.microsoftonline.com/{0}/.well-known/openid-configuration", tenantId);
            var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(stsDiscoveryEndpoint,
                new OpenIdConnectConfigurationRetriever());
            var config = await configManager.GetConfigurationAsync();
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidAudience = myAudience,
                ValidIssuer = myIssuer,
                IssuerSigningKeys = config.SigningKeys,
                ValidateLifetime = false,
                ValidateIssuer = true,
                ValidateAudience = true
            };

            var principal = tokenHandler.ValidateToken(request, validationParameters, out _);
            return principal;
        }
    }
}