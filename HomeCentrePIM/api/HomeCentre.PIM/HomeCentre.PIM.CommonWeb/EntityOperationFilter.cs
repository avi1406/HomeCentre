using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace HomeCentre.PIM.CommonWeb
{
    public class EntityOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // var excludeProperties = new[] { "dateCreated", "dateModified", "createdBy", "updatedBy" }.ToList();
            var excludeProperties = new string[] {}.ToList();

            if (operation.Parameters == null)
            {
                return;
            }

            if (context.ApiDescription.SupportedResponseTypes.Any(x => x.Type == typeof(FileContentResult)))
            {
                operation.Responses["200"].Content = new Dictionary<string, OpenApiMediaType>
                {
                    {
                        "application/octet-stream", new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "file"
                            }
                        }
                    }
                };
            }
            else
            {
                UpdateResponseTypes(context.ApiDescription.SupportedResponseTypes);
                foreach (var responseType in context.ApiDescription.SupportedResponseTypes)
                {
                    var schema = context.SchemaGenerator.GenerateSchema(responseType.Type, context.SchemaRepository);
                    operation.Responses["200"].Content = new Dictionary<string, OpenApiMediaType>
                    {
                        {
                            "text/plain", new OpenApiMediaType
                            {
                                Schema = schema
                            }
                        },
                        {
                            "application/json", new OpenApiMediaType
                            {
                                Schema = schema
                            }
                        },
                        {
                            "text/json", new OpenApiMediaType
                            {
                                Schema = schema
                            }
                        }
                    };
                }
            }

            for (var index = 0; index < operation.Parameters.Count; index++)
            {
                var parameter = operation.Parameters[index];
                if (!excludeProperties.Contains(parameter?.Name))
                {
                    continue;
                }

                operation.Parameters.RemoveAt(index);
                index--;
            }
        }

        private void UpdateResponseTypes(IList<ApiResponseType> responseTypes)
        {
            foreach (var supportedResponseType in responseTypes)
            {
                var isGenericApiResponseType = supportedResponseType.Type.IsGenericType &&
                                               supportedResponseType.Type.GetGenericTypeDefinition() ==
                                               typeof(ApiResponse<>);
                if (isGenericApiResponseType ||
                    supportedResponseType.Type == typeof(ApiResponse))
                {
                    continue;
                }

                var listType = typeof(ApiResponse<>);
                if (supportedResponseType.Type == typeof(void))
                {
                    supportedResponseType.Type = typeof(ApiResponse);
                    continue;
                }

                var genericType = listType.MakeGenericType(supportedResponseType.Type);
                supportedResponseType.Type = genericType;
            }
        }
    }
}