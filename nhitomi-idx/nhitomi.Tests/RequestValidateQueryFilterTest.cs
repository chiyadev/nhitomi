using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using nhitomi.Models;
using NUnit.Framework;

namespace nhitomi
{
    [ApiController, Route(nameof(RequestValidateQueryFilterTestController))]
    public class RequestValidateQueryFilterTestController : ControllerBase
    {
        public class Model
        {
            [Required]
            public string Data { get; set; }
        }

        [HttpPost("data")]
        public string Post(Model model) => model.Data;
    }

    /// <summary>
    /// <see cref="RequestValidateQueryFilter"/>
    /// </summary>
    public class RequestValidateQueryFilterTest : TestBaseHttpClient
    {
        [Test]
        public async Task Valid()
        {
            var exception = await ThrowsStatusAsync(HttpStatusCode.UnprocessableEntity, () => PostAsync<string>("data", new RequestValidateQueryFilterTestController.Model
            {
                Data = "  test  "
            }));

            var result = JsonConvert.DeserializeObject<Result<ValidationProblem[]>>(await exception.Response.Content.ReadAsStringAsync());

            Assert.That(result.Value, Has.Exactly(0).Items);
        }

        [Test]
        public async Task Invalid()
        {
            var exception = await ThrowsStatusAsync(HttpStatusCode.UnprocessableEntity, () => PostAsync<string>("data", new RequestValidateQueryFilterTestController.Model
            {
                Data = "    "
            }));

            var result = JsonConvert.DeserializeObject<Result<ValidationProblem[]>>(await exception.Response.Content.ReadAsStringAsync());

            Assert.That(result.Value, Has.Exactly(1).Items);
            Assert.That(result.Value[0].Field, Is.EqualTo("data"));
        }
    }
}