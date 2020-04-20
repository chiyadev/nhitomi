using Microsoft.OpenApi.Models;
using nhitomi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace nhitomi.Documentation
{
    /// <summary>
    /// Adds validation query parameter to POST and PUT methods. <see cref="RequestValidateQueryFilter"/>
    /// </summary>
    public class ValidateQueryOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            switch (context.ApiDescription.HttpMethod)
            {
                case "POST":
                case "PUT":
                    break;

                default:
                    return;
            }

            operation.Parameters.Add(new OpenApiParameter
            {
                Name        = "validate",
                In          = ParameterLocation.Query,
                Required    = false,
                Description = "True to validate this request only. 422 Unprocessable Entity will be returned regardless of success, with a list of validation problems.",
                Schema = new OpenApiSchema
                {
                    Type = "boolean"
                }
            });

            operation.Responses["422"] = new OpenApiResponse
            {
                Description = "Validation failure",
                Content =
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = context.SchemaGenerator.GenerateSchema(typeof(Result<ValidationProblem[]>), context.SchemaRepository)
                    }
                }
            };
        }
    }
}