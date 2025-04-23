using HomeCentre.PIM.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HomeCentre.PIM.CommonWeb
{
    public class HandledExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<HandledException> _logger;
        private readonly IHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public HandledExceptionFilter(ILogger<HandledException> logger, IHostEnvironment environment,
            IConfiguration configuration)
        {
            _logger = logger;
            _environment = environment;
            _configuration = configuration;
        }

        public void OnException(ExceptionContext context)
        {
            if (!context.HttpContext.Request.Path.StartsWithSegments(new PathString("/api")))
            {
                return;
            }

            if (context.Exception is HandledException)
            {
                _logger.LogWarning(context.Exception, $"Handled Error in {context.HttpContext.Request.Path}");
            }
            else
            {
                _logger.LogError(context.Exception, $"Error in {context.HttpContext.Request.Path}");
            }

            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            switch (context.Exception)
            {
                case HandledException exception:
                    _logger.LogWarning($"An '{exception.Data["message"]}' occured in " + env);
                    context.Result = new ObjectResult(new ApiResponse
                    {
                        Code = (int)exception.Data["code"],
                        Message = (string)exception.Data["message"],
                        RequestKey = Guid.NewGuid().ToString(),
                        HasError = true
                    });

                    break;
                case null:
                    context.Result = new ObjectResult(new ApiResponse
                    {
                        Result = context.Result,
                        HasError = false
                    });
                    break;
                default:
                    _logger.LogWarning("An unknown error occured in " + env);
                    context.Result = new ObjectResult(new ApiResponse
                    {
                        Message = env.IsDevOrQa()
                            ? context.Exception.Message
                            : _configuration["Messages:ServerError"] ?? "Something went wrong",
                        RequestKey = Guid.NewGuid().ToString(),
                        HasError = true,
                        Detail = env.IsDevOrQa() ? context.Exception.StackTrace : null
                    });
                    break;
            }
        }
    }
}