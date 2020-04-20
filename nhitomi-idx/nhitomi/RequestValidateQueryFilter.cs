using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;

namespace nhitomi
{
    /// <summary>
    /// For POST and PUT requests, handles the "validate" query parameter and short-circuits.
    /// </summary>
    public class RequestValidateQueryFilter : IAsyncActionFilter
    {
        public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            switch (context.HttpContext.Request.Method)
            {
                case "POST":
                case "PUT":
                    var query = ModelSanitizer.Sanitize(context.HttpContext.Request.Query["validate"].ToString());

                    if (query != null && (!bool.TryParse(query, out var x) || x)) // specified and not "false"
                    {
                        // at this point in an action filter, model is already validated, so we will simply short-circuit.
                        // we always with 422 even if there are no validation problems.
                        var problems = Array.Empty<ValidationProblem>();

                        context.Result = ResultUtilities.UnprocessableEntity(problems);

                        return Task.CompletedTask;
                    }

                    break;
            }

            return next();
        }
    }
}