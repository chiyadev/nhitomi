using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using nhitomi.Models;
using NUnit.Framework;

namespace nhitomi.Tests
{
    public class RequestValidateOnlyQueryTest : TestBaseHttpClient
    {
        [Test]
        public async Task Valid()
        {
            try
            {
                await PostAsync<object>("users/auth?validate=true", new AuthenticateRequest
                {
                    Username = "admin",
                    Password = "admin"
                });
            }
            catch (RequestException e)
            {
                // should always return 422 regardless of validation success or failure
                Assert.That(e.Response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));

                var result = JsonConvert.DeserializeObject<Result<ValidationProblem[]>>(await e.Response.Content.ReadAsStringAsync());

                Assert.That(result.Value, Has.Exactly(0).Items);
            }
        }

        [Test]
        public async Task Invalid()
        {
            try
            {
                await PostAsync<object>("users/auth?validate=true", new AuthenticateRequest
                {
                    Username = "admin",
                    Password = "  "
                });
            }
            catch (RequestException e)
            {
                Assert.That(e.Response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));

                var result = JsonConvert.DeserializeObject<Result<ValidationProblem[]>>(await e.Response.Content.ReadAsStringAsync());

                Assert.That(result.Value, Has.Exactly(1).Items);
            }
        }
    }
}