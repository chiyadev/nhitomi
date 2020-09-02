using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using nhitomi.Database;
using nhitomi.Models;
using nhitomi.Scrapers.Tests;
using nhitomi.Storage;
using Prometheus;

namespace nhitomi.Scrapers
{
    public enum ScraperType
    {
        Unknown = 0,

        /// <summary>
        /// <see cref="nhentaiScraper"/>
        /// </summary>
        nhentai = 1,

        /// <summary>
        /// <see cref="HitomiScraper"/>
        /// </summary>
        Hitomi = 2
    }

    public abstract class ScraperOptions
    {
        /// <summary>
        /// True to enable this scraper. False by default.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Interval of scrapes.
        /// </summary>
        public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(10);
    }

    /// <summary>
    /// Scrapers are responsible for scraping data from another website and indexing it into the database.
    /// </summary>
    public interface IScraper : IHostedService, IDisposable
    {
        IServiceProvider Services { get; }
        IScraperTestManager TestManager { get; }

        string Name { get; }
        ScraperType Type { get; }
        ScraperCategory Category { get; }
        bool Enabled { get; }

        string Url { get; }
        ScraperUrlRegex UrlRegex { get; }
    }

    /// <summary>
    /// <see cref="ScraperBase{TState}"/> is generic.
    /// </summary>
    static class ScraperMetrics
    {
        public static readonly Histogram ScrapingTime = Metrics.CreateHistogram("scraper_scraping_time", "Time spent on scraping a source.", new HistogramConfiguration
        {
            Buckets    = HistogramEx.ExponentialBuckets(1, 30, 10),
            LabelNames = new[] { "type" }
        });

        public static readonly Counter ScrapeErrors = Metrics.CreateCounter("scraper_errors", "Number of errors occurred that terminated a scrape.", new CounterConfiguration
        {
            LabelNames = new[] { "type" }
        });

        public static readonly Histogram TestingTime = Metrics.CreateHistogram("scraper_testing_time", "Time spent on testing a source.", new HistogramConfiguration
        {
            Buckets    = HistogramEx.ExponentialBuckets(1, 30, 10),
            LabelNames = new[] { "type" }
        });
    }

    public abstract class ScraperBase<TState> : BackgroundService, IScraper
    {
        readonly IResourceLocker _locker;
        readonly IStorage _storage;
        readonly IWriteControl _writeControl;
        readonly IOptionsMonitor<ScraperOptions> _options;
        readonly ILogger<ScraperBase<TState>> _logger;

        public IServiceProvider Services { get; }
        public virtual IScraperTestManager TestManager => null;

        public abstract string Name { get; }
        public abstract ScraperType Type { get; }
        public abstract ScraperCategory Category { get; }
        public bool Enabled => _options.CurrentValue.Enabled;

        public abstract string Url { get; }
        public virtual ScraperUrlRegex UrlRegex => null;

        protected ScraperBase(IServiceProvider services, IOptionsMonitor<ScraperOptions> options, ILogger<ScraperBase<TState>> logger)
        {
            Services = services;

            _locker       = services.GetService<IResourceLocker>();
            _storage      = services.GetService<IStorage>();
            _writeControl = services.GetService<IWriteControl>();
            _options      = options;
            _logger       = logger;
        }

        protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!Enabled)
                        goto sleep;

                    await using (await _writeControl.EnterAsync(stoppingToken))
                    await using (await _locker.EnterAsync($"scrape:{Type}", stoppingToken))
                    {
                        _logger.LogDebug($"Begin {Type} scrape.");

                        // run tests before scrape
                        await TestAsync(stoppingToken);

                        // load last state from storage
                        var state = await _storage.ReadObjectAsync<TState>($"scrapers/{Type}/state", stoppingToken);

                        // execute scraper
                        using (ScraperMetrics.ScrapingTime.Labels(Type.ToString()).Measure())
                            await RunAsync(state, stoppingToken);

                        // save state
                        await _storage.WriteObjectAsync($"scrapers/{Type}/state", state, stoppingToken);

                        if (_logger.IsEnabled(LogLevel.Debug))
                            _logger.LogDebug($"Saved scraper state {Type}: {JsonConvert.SerializeObject(state)}");

                        _logger.LogDebug($"End {Type} scrape.");
                    }
                }
                catch (Exception e)
                {
                    ScraperMetrics.ScrapeErrors.Labels(Type.ToString()).Inc();

                    _logger.LogWarning(e, $"Exception while scraping {Type}.");
                }

                sleep:
                await Task.Delay(_options.CurrentValue.Interval, stoppingToken);
            }
        }

        /// <summary>
        /// Tests this scraper and throws an exception if this scraper is broken.
        /// This is called before <see cref="RunAsync"/> to ensure invalid data does not get indexed
        /// when this scraper breaks due to the reasons such as the website's layout changing.
        /// </summary>
        protected async Task TestAsync(CancellationToken cancellationToken = default)
        {
            var manager = TestManager;

            if (manager == null)
                return;

            using (ScraperMetrics.TestingTime.Labels(Type.ToString()).Measure())
                await manager.RunAsync(cancellationToken);
        }

        /// <summary>
        /// Scrapes and indexes data into the database.
        /// </summary>
        protected abstract Task RunAsync(TState state, CancellationToken cancellationToken = default);

        /// <summary>
        /// Forcefully calls <see cref="RunAsync"/> immediately. This is for unit testing purposes only.
        /// </summary>
        public Task ForceRunAsync(TState state, CancellationToken cancellationToken = default) => RunAsync(state, cancellationToken);
    }

    public class ScraperUrlRegex
    {
        public readonly string Pattern;

        public readonly Regex Strict;
        public readonly Regex Lax;

        public ScraperUrlRegex(string pattern)
        {
            Pattern = pattern;

            Strict = new Regex($@"^{pattern}$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
            Lax    = new Regex($@"\b{pattern}\b", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        }

        public string Group { get; set; } = "id";

        public IEnumerable<string> MatchIds(string input, bool strict)
        {
            var regex = strict ? Strict : Lax;

            foreach (Match match in regex.Matches(input))
            {
                var group = match.Groups[Group];

                if (group.Success)
                    yield return group.Value;
            }
        }

        public bool IsMatch(string input) => Strict.IsMatch(input);
    }
}