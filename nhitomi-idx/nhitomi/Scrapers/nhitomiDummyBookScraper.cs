using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using ChiyaFlake;
using Microsoft.Extensions.Logging;
using nhitomi.Database;

namespace nhitomi.Scrapers
{
    /// <summary>
    /// This is a dummy scraper, used to provide link scanning for nhitomi's own gallery urls.
    /// </summary>
    public class nhitomiDummyBookScraper : BookScraperBase
    {
        readonly ILinkGenerator _link;

        public override string Name => "nhitomi (dummy)";
        public override ScraperType Type => ScraperType.Unknown;
        public override string Url => _link.GetWebLink("/");
        public override ScraperUrlRegex UrlRegex { get; }

        sealed class Options : ScraperOptions { }

        public nhitomiDummyBookScraper(IServiceProvider services, ILogger<nhitomiDummyBookScraper> logger, ILinkGenerator link) : base(services, Extensions.GetOptionsMonitor(new Options()), logger)
        {
            _link = link;

            var hostname = new Uri(_link.GetWebLink("/")).Authority;

            UrlRegex = new ScraperUrlRegex($@"(nhitomi(\/|\s+)|(https?:\/\/)?{Regex.Escape(hostname)}\/books\/)(?<id>\w{{1,{Snowflake.MaxLength}}})((\/contents)?\/(?<contentId>\w{{1,{Snowflake.MaxLength}}}))?\/?");
        }

        public override string GetExternalUrl(DbBookContent content) => null;

        protected override IAsyncEnumerable<BookAdaptor> ScrapeAsync(CancellationToken cancellationToken = default) => AsyncEnumerable.Empty<BookAdaptor>();
    }
}