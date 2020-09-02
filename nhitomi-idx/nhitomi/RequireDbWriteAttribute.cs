using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using nhitomi.Database;

namespace nhitomi
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireDbWriteAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var control     = context.HttpContext.RequestServices.GetService<IWriteControl>();
            var environment = context.HttpContext.RequestServices.GetService<IHostEnvironment>();

            try
            {
                await using (await control.EnterAsync())
                    await next();
            }
            catch (WriteControlException e)
            {
                context.Result = ResultUtilities.Status(HttpStatusCode.ServiceUnavailable, e.ToStringWithTrace("Could not complete this request at the moment due to maintenance.", environment.IsProduction()));
            }
        }
    }
}