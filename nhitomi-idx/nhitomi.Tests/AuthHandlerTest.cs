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
    [Route(nameof(AuthHandlerTestController)), ApiExplorerSettings(IgnoreApi = true)]
    public class AuthHandlerTestController : nhitomiControllerBase // not TestControllerBase because it allows anonymous
    {
        [HttpGet("anon"), AllowAnonymous]
        public string Anon() => "hello";

        [HttpGet("auth"), RequireUser]
        public string Auth() => User?.Username;

        [HttpGet("perm"), RequireUser(Permissions = UserPermissions.ManageUsers)]
        public string Permission() => "pass";
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
    }
}