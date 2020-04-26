using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace nhitomi.Documentation
{
    public class RequireHumanOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var attr = context.ApiDescription.CustomAttributes().OfType<RequireHumanAttribute>().Any();

            if (!attr)
                return;

            operation.Parameters.Add(new OpenApiParameter
            {
                Name            = "recaptcha",
                In              = ParameterLocation.Query,
                AllowEmptyValue = false,
                Description     = "reCAPTCHA token.",
                Schema = new OpenApiSchema
                {
                    Type = "string"
                }
            });
        }
    }
}