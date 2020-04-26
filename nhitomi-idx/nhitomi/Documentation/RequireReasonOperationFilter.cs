using System;
using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace nhitomi.Documentation
{
    public class RequireReasonOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var attr = context.ApiDescription.CustomAttributes().OfType<RequireReasonAttribute>().Any();

            if (!attr)
                return;

            var param = operation.Parameters.FirstOrDefault(p => p.In == ParameterLocation.Query && p.Name.Equals("reason", StringComparison.OrdinalIgnoreCase));

            if (param != null)
            {
                param.Required         = true;
                param.AllowEmptyValue  = false;
                param.Schema.MinLength = RequireReasonAttribute.MinReasonLength;
            }
            else
            {
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name            = "reason",
                    In              = ParameterLocation.Query,
                    Required        = true,
                    AllowEmptyValue = false,
                    Description     = "Reason for this action.",
                    Schema = new OpenApiSchema
                    {
                        Type      = "string",
                        MinLength = RequireReasonAttribute.MinReasonLength
                    }
                });
            }
        }
    }
}