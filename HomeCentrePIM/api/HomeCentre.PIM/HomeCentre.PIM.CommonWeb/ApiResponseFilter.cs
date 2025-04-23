using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HomeCentre.PIM.CommonWeb
{
    public class ApiResponseFilter : IResultFilter
    {
        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Result.GetType() == typeof(FileStreamResult) ||
                context.Result.GetType() == typeof(FileContentResult) ||
                context.Result.GetType() == typeof(RedirectResult))
            {
                return;
            }

            if (!context.HttpContext.Request.Path.Value.StartsWith("/api/",
                StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            var result = context.Result as ObjectResult;
            if (result?.Value == null)
            {
                context.HttpContext.Response.Headers.TryGetValue("code", out var header);
                int.TryParse(header, out var code);
                context.Result = new ObjectResult(new ApiResponse
                {
                    Result = null,
                    HasError = false,
                    Code = code
                });
            }
            else
            {
                var type = result.Value.GetType();
                var isGenericApiResponseType =
                    type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ApiResponse<>);
                if (!isGenericApiResponseType)
                {
                    if (type == typeof(ApiResponse))
                    {
                        return;
                    }

                    context.HttpContext.Response.Headers.TryGetValue("code", out var header);
                    int.TryParse(header, out var code);
                    context.Result = new ObjectResult(new ApiResponse
                    {
                        Result = result.Value,
                        HasError = false,
                        Code = code
                    });
                }
            }
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {            
        }
    }
}