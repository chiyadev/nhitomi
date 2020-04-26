using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace nhitomi.Documentation
{
    /// <summary>
    /// Adds generic type parameters to the schema repository.
    /// </summary>
    /// <remarks>
    /// This is useful in scenarios like:
    /// Dictionary key is an enum that is only used in the constructed dictionary definition and the client needs to list the enum's values.
    /// </remarks>
    public class GenericTypeParameterAdditionalSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (!context.Type.IsGenericType)
                return;

            foreach (var type in context.Type.GenericTypeArguments)
                context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);
        }
    }
}