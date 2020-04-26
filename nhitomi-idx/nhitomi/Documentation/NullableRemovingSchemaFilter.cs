using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace nhitomi.Documentation
{
    // this filter still seems to be necessary in Swashbuckle 5.3.1
    // https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/1346
    public class NullableRemovingSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            // remove all nullability
            schema.Nullable = false;

            foreach (var property in schema.Properties.Values)
                property.Nullable = false;
        }
    }
}