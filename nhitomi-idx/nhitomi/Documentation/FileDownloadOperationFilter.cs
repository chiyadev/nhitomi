using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace nhitomi.Documentation
{
    [AttributeUsage(AttributeTargets.Method)]
    sealed class ProducesFileAttribute : Attribute
    {
        public string MediaType { get; set; } = "application/octet-stream";
    }

    public class FileDownloadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // find ProducesFile
            var mediaType = context.ApiDescription
                                   .CustomAttributes()
                                   .OfType<ProducesFileAttribute>()
                                   .FirstOrDefault()
                                  ?.MediaType;

            // remove other json media types
            if (mediaType == null)
                foreach (var response in operation.Responses.Values)
                {
                    response.Content.Remove("text/json");
                    response.Content.Remove("text/plain");
                }

            // rewrite result media type
            else
                operation.Responses["200"].Content = new Dictionary<string, OpenApiMediaType>
                {
                    [mediaType] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type   = "string",
                            Format = "binary"
                        }
                    }
                };
        }
    }
}