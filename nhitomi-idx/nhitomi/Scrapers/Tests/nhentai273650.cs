using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace nhitomi.Scrapers.Tests
{
    public class nhentai273650 : ScraperTest<nhentaiBook>
    {
        readonly nhentaiScraper _scraper;

        public nhentai273650(nhentaiScraper scraper, ILogger<nhentai273650> logger) : base(logger)
        {
            _scraper = scraper;
        }

        protected override Task<nhentaiBook> GetAsync(CancellationToken cancellationToken = default) => _scraper.GetAsync(273650, cancellationToken);

        protected override void Match(nhentaiBook other)
        {
            Match("id", 273650, other.Id);
            Match("mediaId", 1421931, other.MediaId);
            Match("uploadDate", 1559024244, other.UploadDate);

            Match("title.english", "[ASTRONOMY (SeN)] Imouto wa Onii-chan to Shouraiteki ni Flag o Tatetai (Fate/kaleid liner Prisma Illya) [Digital]", other.Title.English);
            Match("title.japanese", "[ASTRONOMY (SeN)] いもうとはお兄ちゃんと将来的にフラグをたてたい (Fate/kaleid liner プリズマ☆イリヤ) [DL版]", other.Title.Japanese);

            Match("pages", 33, other.Images.Pages.Length);
            Match("pages.ext", new string('j', 33), string.Concat(other.Images.Pages.Select(p => p.Type)));

            Match("tags", new[]
            {
                "astronomy",
                "japanese",
                "illyasviel von einzbern",
                "shirou emiya",
                "lolicon",
                "defloration",
                "sen",
                "fate kaleid liner prisma illya",
                "doujinshi",
                "sole female",
                "sole male"
            }, other.Tags.Select(t => t.Name));
        }
    }
}