using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace nhitomi.Documentation
{
    public class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // must have at least one IFormFile param
            var parameters = context.ApiDescription.ActionDescriptor.Parameters.Where(p => p.ParameterType == typeof(IFormFile)).ToArray();

            if (parameters.Length == 0)
                return;

            // find Consumes
            var consumes = context.ApiDescription
                                  .CustomAttributes()
                                  .OfType<ConsumesAttribute>()
                                  .FirstOrDefault()
                                 ?.ContentTypes
                        ?? new MediaTypeCollection { "multipart/form-data" };

            // rewrite RequestBody
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = consumes.ToDictionary(
                    x => x,
                    x => new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Properties = parameters.ToDictionary(
                                p => p.Name,
                                p => new OpenApiSchema
                                {
                                    Type   = "string",
                                    Format = "binary"
                                }),
                            Required = new HashSet<string>(parameters.Select(p => p.Name))
                        }
                    }),
                Required = true
            };
        }
    }
}