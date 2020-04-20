using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace nhitomi.Documentation
{
    public class JsonRequestContentTypeOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // remove other json request types
            operation.RequestBody?.Content.Remove("application/json-patch+json");
            operation.RequestBody?.Content.Remove("text/json");
            operation.RequestBody?.Content.Remove("application/*+json");
        }
    }
}