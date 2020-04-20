using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using nhitomi.Models;
using NUnit.Framework;

namespace nhitomi.Tests
{
    [ApiController, Route(nameof(PrimitiveResponseWrapperFilterTestController))]
    public sealed class PrimitiveResponseWrapperFilterTestController : ControllerBase
    {
        [HttpGet("string")]
        public string PlainString() => "plain string test";

        [HttpGet("bro")]
        public ActionResult BadRequestObj() => new BadRequestObjectResult("bad request");

        [HttpGet("status")]
        public ActionResult Status() => new StatusCodeResult(404);
    }

    public class PrimitiveResponseWrapperFilterTest : TestBaseHttpClient
    {
        protected override string RequestPathPrefix => base.RequestPathPrefix + nameof(PrimitiveResponseWrapperFilterTestController);

        [Test]
        public async Task PlainString()
        {
            var result = await GetAsync<Result<string>>("string");

            Assert.That(result.Error, Is.False);
            Assert.That(result.Status, Is.EqualTo((int) HttpStatusCode.OK));
            Assert.That(result.Message, Is.EqualTo("plain string test"));
        }

        [Test]
        public async Task BadRequestObject()
        {
            try
            {
                await GetAsync<Result<string>>("bro");

                Assert.Fail("exception expected");
            }
            catch (RequestException e)
            {
                Assert.That(e.Response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

                var result = JsonConvert.DeserializeObject<Result<string>>(await e.Response.Content.ReadAsStringAsync());

                Assert.That(result.Error, Is.True);
                Assert.That(result.Status, Is.EqualTo((int) HttpStatusCode.BadRequest));
                Assert.That(result.Message, Is.EqualTo("bad request"));
            }
        }

        [Test]
        public async Task Status()
        {
            try
            {
                await GetAsync<Result<string>>("status");

                Assert.Fail("exception expected");
            }
            catch (RequestException e)
            {
                Assert.That(e.Response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

                var result = JsonConvert.DeserializeObject<Result<string>>(await e.Response.Content.ReadAsStringAsync());

                Assert.That(result.Error, Is.True);
                Assert.That(result.Status, Is.EqualTo((int) HttpStatusCode.NotFound));
            }
        }
    }
}