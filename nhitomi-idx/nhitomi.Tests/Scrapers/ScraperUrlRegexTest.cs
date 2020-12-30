using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace nhitomi.Scrapers
{
    public class ScraperUrlRegexTest : TestBaseServices
    {
        [Test]
        public void nhentai()
        {
            var regex = Services.GetRequiredService<nhentaiScraper>().UrlRegex;

            Assert.That(regex.IsMatch("https://nhentai.net/g/123/"), Is.True);
            Assert.That(regex.IsMatch("https://nhentai.net/g/123"), Is.True);
            Assert.That(regex.IsMatch("https://nhentai.net/g/"), Is.False);
            Assert.That(regex.IsMatch("http://nhentai.net/g/123/"), Is.True);
            Assert.That(regex.IsMatch("nhentai.net/g/123"), Is.True);
            Assert.That(regex.IsMatch("nhentai.net/g 123"), Is.False);
            Assert.That(regex.IsMatch("nhentai.net/123"), Is.False);
            Assert.That(regex.IsMatch("nhentai.net 123"), Is.False);
            Assert.That(regex.IsMatch("nhentai.net"), Is.False);
            Assert.That(regex.IsMatch("nhentai/g/123"), Is.False);
            Assert.That(regex.IsMatch("nhentai/123"), Is.True);
            Assert.That(regex.IsMatch("nhentai 123"), Is.True);
            Assert.That(regex.IsMatch("nh/g/123"), Is.False);
            Assert.That(regex.IsMatch("nh/123"), Is.True);
            Assert.That(regex.IsMatch("nh 123"), Is.True);
            Assert.That(regex.IsMatch("123"), Is.False);
        }
    }
}