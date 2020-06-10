using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace nhitomi.Scrapers.Tests
{
    public class Hitomi1597977 : ScraperTest<HitomiBook>
    {
        readonly HitomiScraper _scraper;

        public Hitomi1597977(HitomiScraper scraper, ILogger<ScraperTest<HitomiBook>> logger) : base(logger)
        {
            _scraper = scraper;
        }

        protected override Task<HitomiBook> GetAsync(CancellationToken cancellationToken = default) => _scraper.GetAsync(1597977, cancellationToken);

        protected override void Match(HitomiBook other)
        {
            Match("id", 1597977, other.GalleryInfo.Id);
            Match("title", "Succubus-Chan Ikusei Nisshi 2 | Sex Education Diary Succubus-Chan 2", other.Title);
            Match("artist", "Hanamiya Natsuka", other.Artist);
            Match("group", "Unagiyasan", other.Group);
            Match("type", "doujinshi", other.GalleryInfo.Type);
            Match("language", "english", other.GalleryInfo.Language);
            Match("language_localname", "English", other.GalleryInfo.LanguageLocalName);
            Match("series", "Original", other.Series);

            IsNull("characters", other.Characters);

            Match("tags", new[]
            {
                "Blowjob ♀",
                "Demon Girl ♀",
                "Garter Belt ♀",
                "Loli ♀",
                "Nakadashi ♀",
                "School Swimsuit ♀",
                "Sole Female ♀",
                "Stockings ♀",
                "Tail ♀",
                "Twintails ♀",
                "Full Color",
                "Sole Male ♂",
                "Multi-Work Series"
            }, other.Tags);
        }
    }
}