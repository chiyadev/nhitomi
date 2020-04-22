using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace nhitomi
{
    /// <summary>
    /// Ensures all responses are consistently wrapped in JSON result object.
    /// </summary>
    public class PrimitiveResponseWrapperFilter : IResultFilter
    {
        public void OnResultExecuting(ResultExecutingContext context)
        {
            switch (context.Result)
            {
                case StatusCodeResult result:
                {
                    var status = (HttpStatusCode) result.StatusCode;

                    context.Result = ResultUtilities.Status(status);
                    break;
                }

                case ObjectResult result:
                {
                    var status = (HttpStatusCode) (result.StatusCode ?? 200);

                    var type = result.Value?.GetType();

                    // ensure something is returned, instead of an empty response
                    if (type == null)
                        context.Result = ResultUtilities.Status(status);

                    // wrap string responses
                    else if (type == typeof(string))
                        context.Result = ResultUtilities.Status(status, (string) result.Value);

                    break;
                }
            }
        }

        public void OnResultExecuted(ResultExecutedContext context) { }
    }
}