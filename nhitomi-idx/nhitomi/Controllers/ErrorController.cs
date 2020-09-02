using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using nhitomi.Models;

namespace nhitomi.Controllers
{
    [ApiController, ApiExplorerSettings(IgnoreApi = true)]
    public class ErrorController : ControllerBase
    {
        readonly IHostEnvironment _environment;

        public ErrorController(IHostEnvironment environment)
        {
            _environment = environment;
        }

        [Route("error")]
        public Result<object> Handle()
        {
            var exception = HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error;

            var message = exception?.ToStringWithTrace("Could not complete this request due to an internal server error.", _environment.IsProduction());

            return new Result<object>(HttpStatusCode.InternalServerError, message, null);
        }

        [Route("error/{status}")]
        public Result<object> Handle(HttpStatusCode status)
            => new Result<object>(status, status.ToString(), null);
    }
}