using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace nhitomi
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireHumanAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var validator = context.HttpContext.RequestServices.GetService<IRecaptchaValidator>();

            var value = ModelSanitizer.Sanitize(context.HttpContext.Request.Query["recaptcha"].ToString());

            if (await validator.TryValidateAsync(value, context.HttpContext.RequestAborted))
                await next();

            else
                context.Result = ResultUtilities.BadRequest("Could not verify reCAPTCHA token.");
        }
    }
}