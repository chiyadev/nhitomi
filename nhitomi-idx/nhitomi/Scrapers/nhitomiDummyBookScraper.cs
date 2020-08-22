using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
        readonly IElasticClient _client;
        readonly ILinkGenerator _link;

        public override string Name => "nhitomi (dummy)";
        public override ScraperType Type => ScraperType.Unknown;
        public override string Url => _link.GetWebLink("/");
        public override ScraperUrlRegex UrlRegex { get; }

        sealed class Options : ScraperOptions { }

        public nhitomiDummyBookScraper(IServiceProvider services, ILogger<nhitomiDummyBookScraper> logger, IElasticClient client, ILinkGenerator link) : base(services, Extensions.GetOptionsMonitor(new Options()), logger)
        {
            _client = client;
            _link   = link;

            var hostname = new Uri(_link.GetWebLink("/")).Authority;

            UrlRegex = new ScraperUrlRegex($@"(nhitomi(\/|\s+)|(https?:\/\/)?{Regex.Escape(hostname)}\/books\/)(?<id>\w{{1,{Snowflake.MaxLength}}})((\/contents)?\/(?<contentId>\w{{1,{Snowflake.MaxLength}}}))?\/?");
        }

        public override string GetExternalUrl(DbBookContent content) => null;

        protected override IAsyncEnumerable<BookAdaptor> ScrapeAsync(CancellationToken cancellationToken = default) => AsyncEnumerable.Empty<BookAdaptor>();

        public override async IAsyncEnumerable<(IDbEntry<DbBook>, DbBookContent)> FindByUrlAsync(string url, bool strict, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var ids = UrlRegex.MatchIds(url, strict).ToArray();

            if (ids.Length == 0)
                yield break;

            var entries = await _client.GetEntryManyAsync<DbBook>(ids, cancellationToken);

            foreach (var entry in entries)
            {
                var content = entry.Value.Contents?.OrderByDescending(c => c.Id).FirstOrDefault();

                if (content == null)
                    continue;

                yield return (entry, content);
            }
        }
    }
}