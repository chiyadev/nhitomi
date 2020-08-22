using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using nhitomi.Database;
using nhitomi.Models;
using NUnit.Framework;
using RichardSzalay.MockHttp;

namespace nhitomi.Controllers.OAuth
{
    /// <summary>
    /// <see cref="DiscordOAuthHandler"/>
    /// </summary>
    public class DiscordOAuthHandlerTest : TestBaseServices
    {
        readonly IHttpClientFactory _clientFactory = TestUtils.HttpClient(
            x => x.When("https://discord.com/api/oauth2/token")
                  .With(m => m.Content is FormUrlEncodedContent)
                  .RespondJson(new
                   {
                       access_token = "access token"
                   }),
            x => x.When("https://discord.com/api/users/@me")
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

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.PostConfigure<DiscordOAuthOptions>(o =>
            {
                o.ClientId     = 1234;
                o.ClientSecret = "secret";
            });
        }

        [Test]
        public async Task RegisterAsync()
        {
            var handler = ActivatorUtilities.CreateInstance<DiscordOAuthHandler>(Services, _clientFactory);

            // create user
            var user = await handler.GetOrCreateUserAsync("test code");

            Assert.That(user.Username, Is.EqualTo("phosphene47"));
            Assert.That(user.Email, Is.EqualTo("phosphene47@chiya.dev"));
            Assert.That(user.Language, Is.EqualTo(LanguageType.Japanese));
            Assert.That(user.DiscordConnection.Id, Is.EqualTo(12345));
            Assert.That(user.DiscordConnection.Discriminator, Is.EqualTo(1234));
            Assert.That(user.DiscordConnection.Email, Is.EqualTo("phosphene47@chiya.dev"));

            // ensure creation snapshot
            var snapshots = Services.GetService<ISnapshotService>();

            var snapshotSearch = await snapshots.SearchAsync(ObjectType.User, new SnapshotQuery
            {
                TargetId = user.Id,
                Limit    = 10
            });

            Assert.That(snapshotSearch.Items, Has.Exactly(1).Items);

            var snapshot = snapshotSearch.Items[0];

            Assert.That(snapshot.Source, Is.EqualTo(SnapshotSource.User));
            Assert.That(snapshot.Event, Is.EqualTo(SnapshotEvent.AfterCreation));
            Assert.That(snapshot.CommitterId, Is.EqualTo(user.Id));
        }

        [Test]
        public async Task UpdateInfoAsync()
        {
            // already registered user with outdated info
            var oldUser = await MakeUserAsync("old username", u =>
            {
                u.Email    = "old@gmail.com";
                u.Language = LanguageType.Chinese;
                u.DiscordConnection = new DbUserDiscordConnection
                {
                    Id            = 12345,
                    Discriminator = 4321,
                    Email         = "old@gmail.com"
                };
            });

            var handler = ActivatorUtilities.CreateInstance<DiscordOAuthHandler>(Services, _clientFactory);

            var user = await handler.GetOrCreateUserAsync("test code");

            // should not create new user
            Assert.That(user.Id, Is.EqualTo(oldUser.Id));

            // should update user with new info
            Assert.That(user.Username, Is.EqualTo("phosphene47"));
            Assert.That(user.Email, Is.EqualTo("phosphene47@chiya.dev"));
            Assert.That(user.Language, Is.EqualTo(LanguageType.Japanese));
            Assert.That(user.DiscordConnection.Id, Is.EqualTo(12345));
            Assert.That(user.DiscordConnection.Discriminator, Is.EqualTo(1234));
            Assert.That(user.DiscordConnection.Email, Is.EqualTo("phosphene47@chiya.dev"));

            // should not create snapshot
            var snapshots = Services.GetService<ISnapshotService>();

            var snapshotSearch = await snapshots.SearchAsync(ObjectType.User, new SnapshotQuery
            {
                TargetId = user.Id,
                Limit    = 10
            });

            Assert.That(snapshotSearch.Items, Has.Exactly(0).Items);
        }
    }
}
