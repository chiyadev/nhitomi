using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using nhitomi.Database;
using NUnit.Framework;

namespace nhitomi.Tests
{
    [TestFixture(typeof(SemaphoreResourceLocker))]
    [TestFixture(typeof(RedisResourceLocker))]
    public class ResourceLockerTest<T> : TestBaseServices where T : IResourceLocker
    {
        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.RemoveLogging();
        }

        [Test]
        public async Task Test()
        {
            using var locker = ActivatorUtilities.CreateInstance<T>(Services);

            var entered = false;

            async Task enter()
            {
                // ReSharper disable once AccessToDisposedClosure
                await using (await locker.EnterAsync("key"))
                {
                    Assert.That(entered, Is.False);

                    entered = true;

                    await Task.Yield();

                    Assert.That(entered, Is.True);

                    entered = false;
                }
            }

            await Task.WhenAll(Enumerable.Range(0, 100).Select(_ => enter()));
        }
    }
}