using System.Linq;
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
        public async Task Run()
        {
            var init = ActivatorUtilities.CreateInstance<StartupInitializer>(Services);

            await init.RunAsync();

            var users = Services.GetService<IUserService>();

            Assert.That(await users.CountAsync(), Is.EqualTo(1));

            var user = (await users.SearchAsync(new UserQuery { Limit = 1 })).Items.FirstOrDefault();

            Assert.That(user, Is.Not.Null);
        }
    }
}