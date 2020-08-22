using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace nhitomi.Scrapers
{
    [TestFixture(typeof(nhitomiDummyBookScraper))]
    [TestFixture(typeof(nhentaiScraper))]
    [TestFixture(typeof(HitomiScraper))]
    public class ScraperTest<T> : TestBaseServices where T : IScraper
    {
        [Test]
        public async Task AllTests()
        {
            var tests = ActivatorUtilities.CreateInstance<T>(Services).TestManager;

            if (tests == null)
                return;

            tests.PickCount = null;
            tests.Parallel  = false;

            await tests.RunAsync();
        }
    }
}