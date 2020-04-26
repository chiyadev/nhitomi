using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;

namespace nhitomi
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireReasonAttribute : Attribute, IAsyncActionFilter
    {
        public const int MinReasonLength = 4;

        public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var reason = ModelSanitizer.Sanitize(context.HttpContext.Request.Query["reason"].ToString());

            if (reason != null && reason.Length >= MinReasonLength)
                return next();

            context.Result = ResultUtilities.BadRequest("A valid reason must be provided for this action.");

            return Task.CompletedTask;
        }
    }
}