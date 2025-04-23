using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace HomeCentre.PIM.CommonWeb
{
    public class EntitySchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema.Properties != null)
            {
                // var excludeProperties = new[] { "dateCreated", "dateModified", "createdBy", "updatedBy" }.ToList();
                var excludeProperties = new string[] {}.ToList();
                foreach (var prop in excludeProperties)
                {
                    if (schema.Properties.ContainsKey(prop))
                    {
                        schema.Properties.Remove(prop);
                    }
                }
            }
        }
    }
}