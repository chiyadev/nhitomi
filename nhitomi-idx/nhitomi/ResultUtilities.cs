using System.Net;
using Microsoft.AspNetCore.Mvc;
using nhitomi.Models;

namespace nhitomi
{
    public static class ResultUtilities
    {
        public static ActionResult Status(HttpStatusCode status, string message = null)
            => Value<object>(status, null, message ?? status.ToString());

        public static ActionResult Value<T>(HttpStatusCode status, T value, string message = null) where T : class
        {
            var result = new Result<T>(status, message, value);

            // some test code depends on action result type equality
            return status switch
            {
                HttpStatusCode.NotFound            => new NotFoundObjectResult(result),
                HttpStatusCode.BadRequest          => new BadRequestObjectResult(result),
                HttpStatusCode.UnprocessableEntity => new UnprocessableEntityObjectResult(result),

                _ => new ObjectResult(result) { StatusCode = (int) status }
            };
        }

        public static ActionResult NotFound(params object[] ids) => Status(HttpStatusCode.NotFound, $"'{string.Join('/', ids)}' not found.");
        public static ActionResult Forbidden(string message) => Status(HttpStatusCode.Forbidden, message);
        public static ActionResult BadRequest(string message) => Status(HttpStatusCode.BadRequest, message);
        public static ActionResult Unauthorized(string message) => Status(HttpStatusCode.Unauthorized, message);

        public static ActionResult UnprocessableEntity(params ValidationProblem[] problems)
        {
            var message = problems.Length == 0
                ? "Validation was successful."
                : "There was a problem during validation.";

            return Value(HttpStatusCode.UnprocessableEntity, problems, message);
        }

        public static ActionResult<T> NotFound<T>(params object[] ids) => NotFound(ids);
        public static ActionResult<T> Forbidden<T>(string message) => Forbidden(message);
        public static ActionResult<T> BadRequest<T>(string message) => BadRequest(message);
        public static ActionResult<T> Unauthorized<T>(string message) => Unauthorized(message);
        public static ActionResult<T> UnprocessableEntity<T>(params ValidationProblem[] problems) => UnprocessableEntity(problems);
    }
}