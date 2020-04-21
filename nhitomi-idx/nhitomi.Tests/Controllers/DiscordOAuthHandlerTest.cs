using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using RichardSzalay.MockHttp;

namespace nhitomi.Controllers
{
    public class DiscordOAuthHandlerTest : TestBaseServices
    {
        [Test]
        public async Task CreateUserAsync()
        {
            var client = TestUtils.HttpClient(
                x => x.When("https://discordapp.com/api/oauth2/token")
                      .With(m => m.Content is FormUrlEncodedContent)
                      .RespondJson(new
                       {
                           access_token = "access token"
                       }),
                x => x.When("https://discordapp.com/api/users/@me")
                      .With(m => m.Headers.Authorization.Scheme == "Bearer" && m.Headers.Authorization.Parameter == "access token")
                      .RespondJson(new
                       {
                           id            = "12345",
                           username      = "phosphene47",
                           discriminator = "1234",
                           avatar        = "hash",
                           locale        = "jp",
                           verified      = true,
                           email         = "phosphene47@chiya.dev"
                       }));

            var handler = ActivatorUtilities.CreateInstance<DiscordOAuthHandler>(Services, client);

            var user = await handler.GetOrCreateUserAsync("test code");

            Assert.That(user.Username, Is.EqualTo("phosphene47"));
            Assert.That(user.Email, Is.EqualTo("phosphene47@chiya.dev"));
            Assert.That(user.DiscordConnection.Id, Is.EqualTo(12345));
            Assert.That(user.DiscordConnection.Discriminator, Is.EqualTo(1234));
            Assert.That(user.DiscordConnection.Email, Is.EqualTo("phosphene47@chiya.dev"));
            Assert.That(user.DiscordConnection.Verified, Is.EqualTo(true));
        }
    }
}