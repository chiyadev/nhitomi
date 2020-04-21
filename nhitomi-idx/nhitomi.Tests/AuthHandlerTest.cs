using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using nhitomi.Controllers;
using nhitomi.Database;
using nhitomi.Models;
using NUnit.Framework;

namespace nhitomi
{
    [ApiController, Route(nameof(AuthHandlerTestController))]
    public sealed class AuthHandlerTestController : nhitomiControllerBase
    {
        [HttpGet("anon"), AllowAnonymous]
        public string Anon() => "hello";

        [HttpGet("auth"), RequireUser]
        public string Auth() => User?.Username;

        [HttpGet("perm"), RequireUser(Permissions = UserPermissions.ManageUsers)]
        public string Permission() => "pass";

        [HttpGet("users/{id}"), RequireUser(Permissions = UserPermissions.ManageUsers, AllowSelf = "id")]
        public string PermissionsWithAllowSelf(string id) => id;
    }

    /// <summary>
    /// <see cref="AuthHandler"/>
    /// <see cref="RequireUserAttribute"/>
    /// </summary>
    public class AuthHandlerTest : TestBaseHttpClient<AuthHandlerTestController>
    {
        async Task<DbUser> MakeAndAuthUserAsync(string name = null, UserPermissions permissions = UserPermissions.None)
        {
            var user = await MakeUserAsync(name, u => u.Permissions = permissions.ToFlags());

            var token = await Services.GetService<IAuthService>().GenerateTokenAsync(user);

            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return user;
        }

        [Test]
        public async Task NoAuth() => Assert.That(await GetAsync<string>("anon"), Is.EqualTo("hello"));

        [Test]
        public async Task Auth()
        {
            var user = await MakeAndAuthUserAsync("my user");

            Assert.That(await GetAsync<string>("auth"), Is.EqualTo(user.Username));
        }

        [Test]
        public Task AuthRequired() => ThrowsStatusAsync(HttpStatusCode.Unauthorized, () => GetAsync<string>("auth"));

        [Test]
        public async Task MalformedToken()
        {
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "malformed.token");

            await ThrowsStatusAsync(HttpStatusCode.Unauthorized, () => GetAsync<string>("auth"));
        }

        [Test]
        public async Task Permissions()
        {
            await MakeAndAuthUserAsync(null, UserPermissions.ManageUsers);

            Assert.That(await GetAsync<string>("perm"), Is.EqualTo("pass"));
        }

        [Test]
        public async Task PermissionsRequired()
        {
            await MakeAndAuthUserAsync();

            await ThrowsStatusAsync(HttpStatusCode.Forbidden, () => GetAsync<string>("perm"));
        }

        [Test]
        public async Task PermissionsAdminOverride()
        {
            await MakeAndAuthUserAsync(null, UserPermissions.Administrator);

            Assert.That(await GetAsync<string>("perm"), Is.EqualTo("pass"));
        }

        [Test]
        public async Task AllowSelfUsingSelf()
        {
            var user = await MakeAndAuthUserAsync("self user", UserPermissions.ManageUsers);

            Assert.That(await GetAsync<string>($"users/{user.Id}"), Is.EqualTo(user.Id));
        }

        [Test]
        public async Task AllowSelfUsingPerm()
        {
            var user = await MakeUserAsync("other user");

            await MakeAndAuthUserAsync(null, UserPermissions.ManageUsers);

            Assert.That(await GetAsync<string>($"users/{user.Id}"), Is.EqualTo(user.Id));
        }

        [Test]
        public async Task AllowSelfRequired()
        {
            var user = await MakeUserAsync("other user");

            await MakeAndAuthUserAsync();

            await ThrowsStatusAsync(HttpStatusCode.Forbidden, () => GetAsync<string>($"users/{user.Id}"));
        }
    }
}