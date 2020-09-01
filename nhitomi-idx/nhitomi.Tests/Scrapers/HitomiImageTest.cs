using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace nhitomi.Scrapers
{
    /// <summary>
    /// Hitomi tends to break easily, so it deserves a separate test to download images.
    /// </summary>
    public class HitomiImageTest : TestBaseServices
    {
        [Test]
        public async Task Images()
        {
            var scraper    = Services.GetService<HitomiScraper>();
            var hitomiBook = await scraper.GetAsync(1597977);

            Assert.That(hitomiBook, Is.Not.Null);

            var book    = new HitomiBookAdaptor(hitomiBook).Convert(scraper, Services);
            var content = book.Contents[0];

            for (var i = 0; i < 5; i++)
            {
                await using var file = await scraper.GetImageAsync(book, content, i);
            }
        }
    }
}