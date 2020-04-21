using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using nhitomi.Models;
using NUnit.Framework;

namespace nhitomi
{
    public class StartupInitializerTest : TestBaseServices
    {
        [Test]
        public async Task Run()
        {
            GetOptions<UserServiceOptions>().InitializeFirstUser = true;

            var init = ActivatorUtilities.CreateInstance<StartupInitializer>(Services);

            await init.RunAsync();

            var users = Services.GetService<IUserService>();

            Assert.That(await users.CountAsync(), Is.EqualTo(1));

            var user = (await users.SearchAsync(new UserQuery { Limit = 1 })).Items.FirstOrDefault();

            Assert.That(user, Is.Not.Null);

            var snapshots = Services.GetService<ISnapshotService>();

            var snapshot = (await snapshots.SearchAsync(SnapshotTarget.User, new SnapshotQuery
            {
                TargetId = user.Id,
                Limit    = 1
            })).Items?.FirstOrDefault();

            Assert.That(snapshot, Is.Not.Null);
            Assert.That(snapshot.Type, Is.EqualTo(SnapshotType.Creation));
        }
    }
}