using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using nhitomi.Models;

namespace nhitomi
{
    /// <summary>
    /// Wraps <see cref="StatusCodeResult"/> in a <see cref="Result{T}"/> object.
    /// </summary>
    public class StatusCodeResultWrapperFilter : IResultFilter
    {
        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Result is StatusCodeResult result)
            {
                var status = (HttpStatusCode) result.StatusCode;

                context.Result = ResultUtilities.Status(status);
            }
        }

        public void OnResultExecuted(ResultExecutedContext context) { }
    }
}