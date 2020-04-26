using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using nhitomi.Controllers;
using nhitomi.Models;
using NUnit.Framework;

namespace nhitomi
{
    /// <summary>
    /// <see cref="StartupInitializer"/>
    /// </summary>
    public class StartupInitializerTest : TestBaseServices
    {
        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.PostConfigure<UserServiceOptions>(o => o.FirstUserAdmin = true);
        }

        [Test]
        public async Task ConfigureUsersAsync()
        {
            var firstUser  = await MakeUserAsync("first");
            var secondUser = await MakeUserAsync("second");
            var thirdUser  = await MakeUserAsync("third");

            var init = ActivatorUtilities.CreateInstance<StartupInitializer>(Services);

            await init.ConfigureUsersAsync();

            var users = Services.GetService<IUserService>();

            Assert.That(await users.CountAsync(), Is.EqualTo(3));

            firstUser  = (await users.GetAsync(firstUser.Id)).AsT0;
            secondUser = (await users.GetAsync(secondUser.Id)).AsT0;
            thirdUser  = (await users.GetAsync(thirdUser.Id)).AsT0;

            Assert.That(firstUser.HasPermissions(UserPermissions.Administrator), Is.True);
            Assert.That(secondUser.HasPermissions(UserPermissions.Administrator), Is.False);
            Assert.That(thirdUser.HasPermissions(UserPermissions.Administrator), Is.False);
        }
    }
}