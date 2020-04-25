using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace nhitomi.Scrapers
{
    /// <summary>
    /// Thin wrapper around <see cref="IEnumerable{IScraper}"/>.
    /// </summary>
    public interface IScraperService : IEnumerable<IScraper>
    {
        IEnumerable<IBookScraper> Books => this.OfType<IBookScraper>();

        bool Get(ScraperType type, out IScraper scraper);

        bool GetBook(ScraperType type, out IBookScraper scraper)
        {
            if (Get(type, out var s) && s is IBookScraper bs)
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

        public bool Get(ScraperType type, out IScraper scraper) => _scrapers.TryGetValue(type, out scraper);

        public IEnumerator<IScraper> GetEnumerator() => _scrapers.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}