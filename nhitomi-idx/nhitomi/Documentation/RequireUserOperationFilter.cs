using System.Linq;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using nhitomi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace nhitomi.Documentation
{
    public class RequireUserOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var attr = context.ApiDescription.CustomAttributes().OfType<RequireUserAttribute>().LastOrDefault();

            if (attr == null)
                return;

            if (attr.Permissions != UserPermissions.None && attr.AllowSelf == null)
            {
                var array = new OpenApiArray();
                array.AddRange(attr.Permissions.ToFlags().Select(p => new OpenApiString(p.ToString())));

                operation.Extensions["x-permissions"] = array;
            }

            if (attr.Unrestricted)
                operation.Extensions["x-unrestricted"] = new OpenApiBoolean(attr.Unrestricted);

            if (attr.Reputation > 0)
                operation.Extensions["x-reputation-min"] = new OpenApiInteger(attr.Reputation);
        }
    }
}