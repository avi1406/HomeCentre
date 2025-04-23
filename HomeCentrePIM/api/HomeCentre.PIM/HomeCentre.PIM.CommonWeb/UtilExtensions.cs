using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using FluentEmail.Core.Interfaces;
using FluentEmail.Smtp;
using HomeCentre.PIM.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace HomeCentre.PIM.CommonWeb
{
    public static class UtilExtensions
    {
        private static readonly Dictionary<string, Type> _socketControllerTypes = new Dictionary<string, Type>();

        public static string GetHeaderValue(this AuthorizationHandlerContext context, string header)
        {
            var filterContext = (AuthorizationFilterContext)context.Resource;
            filterContext.HttpContext.Request.Headers.TryGetValue(header, out var headerValues);
            return headerValues.FirstOrDefault();
        }

        public static string GetHeaderValue(this HttpRequest request, string header)
        {
            request.Headers.TryGetValue(header, out var headerValues);
            return headerValues.FirstOrDefault();
        }

        public static string GetCookieValue(this AuthorizationHandlerContext context, string header)
        {
            var filterContext = (AuthorizationFilterContext)context.Resource;
            filterContext.HttpContext.Request.Cookies.TryGetValue(header, out var cookieValue);
            return cookieValue;
        }

        public static string GetCookieValue(this HttpRequest request, string header)
        {
            request.Cookies.TryGetValue(header, out var cookieValue);
            return cookieValue;
        }

        public static string GetQueryStringValue(this AuthorizationHandlerContext context, string queryString)
        {
            var filterContext = (AuthorizationFilterContext)context.Resource;
            filterContext.HttpContext.Request.Query.TryGetValue(queryString, out var queryStringValue);
            return queryStringValue;
        }

        public static string GetQueryStringValue(this HttpRequest request, string header)
        {
            request.Query.TryGetValue(header, out var cookieValue);
            return cookieValue;
        }

        public static string GetPath(this AuthorizationHandlerContext context)
        {
            var filterContext = (AuthorizationFilterContext)context.Resource;
            return filterContext.HttpContext.Request.Path;
        }

        public static void RunConcurrently(this Task task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task), "task is null.");
            }

            if (task.Status == TaskStatus.Created)
            {
                task.Start();
            }
        }

        public static string GetClaim(this ClaimsPrincipal principal, string claimName)
        {
            return principal.Claims.FirstOrDefault(x => x.Type == claimName)?.Value;
        }

        public static ClaimsPrincipal ValidateToken(this string token, string secret, string issuer, string audience)
        {
            var symmetricKey = Convert.FromBase64String(secret);
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateLifetime = true,
                ValidateAudience = true,
                IssuerSigningKey = new SymmetricSecurityKey(symmetricKey),
                ValidAudience = audience
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
        }

        public static void AddAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy(Constants.Policies.User,
                    policy =>
                    {
                        policy.Requirements.Add(
                            new HasRoleRequirement(new[] { Constants.Roles.User.Name }));
                    });
                options.AddPolicy(Constants.Policies.Admin,
                    policy =>
                    {
                        policy.Requirements.Add(
                            new HasRoleRequirement(new[] { Constants.Roles.Admin.Name }));
                    });
                options.AddPolicy(Constants.Policies.SuperAdmin,
                    policy =>
                    {
                        policy.Requirements.Add(
                            new HasRoleRequirement(new[] { Constants.Roles.SuperAdmin.Name }));
                    });
            });
        }

        public static void AddMailService(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            var client = new SmtpClient(configuration["AppSettingsSmtpServer"], int.Parse(configuration["AppSettingsSmtpPort"]))
            {
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(
                    configuration["AppSettingsSmtpUserName"],
                    configuration["AppSettingsSmtpPassword"]),
                EnableSsl = true,
                Timeout = 100000
            };

            serviceCollection
                .AddTransient<ISender>(x => new SmtpSender(client))
                .AddFluentEmail(configuration["AppSettingsSmtpFromEmail"])
                .AddRazorRenderer();
        }
    }
}