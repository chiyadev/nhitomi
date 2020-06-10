using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace nhitomi.Scrapers.Tests
{
    public class Hitomi1657375 : ScraperTest<HitomiBook>
    {
        readonly HitomiScraper _scraper;

        public Hitomi1657375(HitomiScraper scraper, ILogger<ScraperTest<HitomiBook>> logger) : base(logger)
        {
            _scraper = scraper;
        }

        protected override Task<HitomiBook> GetAsync(CancellationToken cancellationToken = default) => _scraper.GetAsync(1657375, cancellationToken);

        protected override void Match(HitomiBook other)
        {
            Match("id", 1657375, other.GalleryInfo.Id);
            Match("title", "Kiritan To Kosshori Situation!!", other.Title);
            Match("artist", "Jovejun.", other.Artist);
            Match("group", "Jajujo", other.Group);
            Match("type", "doujinshi", other.GalleryInfo.Type);
            Match("language", "chinese", other.GalleryInfo.Language);
            Match("language_localname", "中文", other.GalleryInfo.LanguageLocalName);
            Match("series", "Voiceroid", other.Series);

            Match("characters", new[]
            {
                "Kiritan Tohoku"
            }, other.Characters);

            Match("tags", new[]
            {
                "Bikini ♀",
                "Exhibitionism ♀",
                "Kemonomimi ♀",
                "Loli ♀",
                "Randoseru ♀",
                "Sole Female ♀",
                "Swimsuit ♀",
                "Sole Male ♂"
            }, other.Tags);

            Match("pages", 25, other.GalleryInfo.Files.Length);
        }
    }
}