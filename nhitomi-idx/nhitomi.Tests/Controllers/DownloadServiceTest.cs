using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace nhitomi.Controllers
{
    public class DownloadServiceTest : TestBaseServices
    {
        [Test]
        public async Task SessionLimit()
        {
            var user = await MakeUserAsync();

            var options = Services.GetRequiredService<IOptionsMonitor<DownloadServiceOptions>>().CurrentValue;
            var service = Services.GetRequiredService<IDownloadService>();

            var sessionId = null as string;

            for (var i = 0; i < options.MaxSessions; i++)
            {
                var createSuccess = await service.CreateSessionAsync(user.Id);

                Assert.That(createSuccess.IsT0, Is.True);

                if (i == 0)
                    sessionId = createSuccess.AsT0.Id;
            }

            var createFail = await service.CreateSessionAsync(user.Id);

            Assert.That(createFail.IsT1, Is.True);

            var deleteSuccess = await service.DeleteSessionAsync(sessionId);

            Assert.That(deleteSuccess.IsT0, Is.True);

            var deleteFail = await service.DeleteSessionAsync("nonexistent");

            Assert.That(deleteFail.IsT1, Is.True);
        }

        [Test]
        public async Task SessionConcurrencyLimit()
        {
            var user = await MakeUserAsync();

            var service = Services.GetRequiredService<IDownloadService>();
            var session = (await service.CreateSessionAsync(user.Id)).AsT0;

            Assert.That(session.Concurrency, Is.Not.Zero);

            for (var i = 0; i < session.Concurrency; i++)
            {
                var enterSuccess = await service.AcquireResourceAsync(session.Id);

                Assert.That(enterSuccess.IsT0, Is.True);
            }

            var enterFailNonexistent = await service.AcquireResourceAsync("nonexistent");

            Assert.That(enterFailNonexistent.IsT1, Is.True);

            var enterFailConcurrencyLimit = await service.AcquireResourceAsync(session.Id);

            Assert.That(enterFailConcurrencyLimit.IsT2, Is.True);

            await service.ReleaseResourceAsync(session.Id);

            var enterSuccess2 = await service.AcquireResourceAsync(session.Id);

            Assert.That(enterSuccess2.IsT0, Is.True);
        }
    }
}