using System.Collections.Generic;
using System.Linq;

namespace nhitomi.Scrapers
{
    /// <summary>
    /// Thin wrapper around <see cref="IEnumerable{IScraper}"/>.
    /// </summary>
    public interface IScraperService
    {
        bool GetScraper(ScraperType type, out IScraper scraper);

        bool GetBookScraper(ScraperType type, out IBookScraper scraper)
        {
            if (GetScraper(type, out var s) && s is IBookScraper bs)
            {
                scraper = bs;
                return true;
            }

            scraper = null;
            return false;
        }
    }

    public class ScraperService : IScraperService
    {
        readonly Dictionary<ScraperType, IScraper> _scrapers;

        public ScraperService(IEnumerable<IScraper> scrapers)
        {
            _scrapers = scrapers.ToDictionary(s => s.Type);
        }

        public bool GetScraper(ScraperType type, out IScraper scraper) => _scrapers.TryGetValue(type, out scraper);
    }
}