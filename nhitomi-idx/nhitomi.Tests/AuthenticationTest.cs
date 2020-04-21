using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using nhitomi.Database;
using nhitomi.Models;
using nhitomi.Models.Requests;
using NUnit.Framework;

namespace nhitomi
{
    public class AuthenticationTest : TestBaseHttpClient
    {
        [Test]
        public async Task NoAuth()
        {
            var info = await GetAsync<GetInfoResponse>("info");

            Assert.That(info, Is.Not.Null);
            Assert.That(info.Version.Hash, Is.EqualTo(VersionInfo.Commit.Hash));
        }

        async Task<DbUser> MakeAndAuthUserAsync(string name = null, UserPermissions permissions = UserPermissions.None)
        {
            var user = await MakeUserAsync(name, u => u.Permissions = permissions.ToFlags());

            var token = await Services.GetService<IAuthService>().GenerateTokenAsync(user);

            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return user;
        }

        [Test]
        public async Task Auth()
        {
            var user = await MakeAndAuthUserAsync();
            var info = await GetAsync<GetInfoAuthenticatedResponse>("info/current");

            Assert.That(info, Is.Not.Null);
            Assert.That(info.Version.Hash, Is.EqualTo(VersionInfo.Commit.Hash));
            Assert.That(info.User.Id, Is.EqualTo(user.Id));
            Assert.That(info.User.Username, Is.EqualTo(user.Username));
        }

        [Test]
        public async Task MalformedToken()
        {
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "malformed.token");

            await ThrowsStatusAsync(HttpStatusCode.Unauthorized, () => GetAsync<GetInfoAuthenticatedResponse>("info/current"));
        }

        [Test]
        public async Task Permissions()
        {
            var user = await MakeAndAuthUserAsync();

            // try to use restriction endpoint which should not be allowed for a normal user
            await ThrowsStatusAsync(HttpStatusCode.Forbidden, () => PostAsync<User>($"users/{user.Id}/restrictions", new RestrictUserRequest()));
        }

        [Test]
        public async Task AllowSelf()
        {
            var user = await MakeAndAuthUserAsync();

            // user update endpoint is allowed for moderators OR the authenticated user themselves
            var updatedUserInfo = await PutAsync<User>($"users/{user.Id}", new UserBase());

            Assert.That(user.Id, Is.EqualTo(updatedUserInfo.Id));
            Assert.That(user.Username, Is.EqualTo(updatedUserInfo.Username));
            Assert.That(user.CreatedTime, Is.EqualTo(updatedUserInfo.CreatedTime));

            await MakeAndAuthUserAsync();

            // normal user should not be able to update other users
            await ThrowsStatusAsync(HttpStatusCode.Forbidden, () => PutAsync<User>($"users/{user.Id}", new UserBase()));
        }

        [Test]
        public async Task PermissionsMod()
        {
            var user = await MakeAndAuthUserAsync();

            await MakeAndAuthUserAsync(default, UserPermissions.ManageUsers);

            var updatedUserInfo = await PutAsync<User>($"users/{user.Id}", new UserBase());

            Assert.That(user.Id, Is.EqualTo(updatedUserInfo.Id));
        }
    }
}