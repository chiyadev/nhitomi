using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using nhitomi.Scrapers.Tests;
using nhitomi.Storage;

namespace nhitomi.Scrapers
{
    public enum ScraperType
    {
        Unknown = 0,

        /// <summary>
        /// <see cref="nhentaiScraper"/>
        /// </summary>
        nhentai = 1
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

        /// <summary>
        /// Type of this scraper.
        /// </summary>
        ScraperType Type { get; }

        IScraperTestManager TestManager { get; }
        ScraperUrlRegex UrlRegex { get; }
    }

    public abstract class ScraperBase : BackgroundService, IScraper
    {
        readonly IResourceLocker _locker;
        readonly IStorage _storage;
        readonly IOptionsMonitor<ScraperOptions> _options;
        readonly ILogger<ScraperBase> _logger;

        public IServiceProvider Services { get; }
        public abstract ScraperType Type { get; }
        public virtual IScraperTestManager TestManager => null;
        public virtual ScraperUrlRegex UrlRegex => null;

        protected ScraperBase(IServiceProvider services, IOptionsMonitor<ScraperOptions> options, ILogger<ScraperBase> logger)
        {
            Services = services;

            _locker  = services.GetService<IResourceLocker>();
            _storage = services.GetService<IStorage>();
            _options = options;
            _logger  = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var options = _options.CurrentValue;

                try
                {
                    if (options.Enabled)
                        await using (await _locker.EnterAsync($"scrape:{Type}", stoppingToken))
                        {
                            _logger.LogDebug($"Begin {Type} scrape.");

                            await TestAsync(stoppingToken);
                            await RunAsync(stoppingToken);

                            _logger.LogDebug($"End {Type} scrape.");
                        }
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, $"Exception while scraping {Type}.");
                }

                await Task.Delay(options.Interval, stoppingToken);
            }
        }

        /// <summary>
        /// Tests this scraper and throws an exception if this scraper is broken.
        /// This is called before <see cref="RunAsync"/> to ensure invalid data does not get indexed
        /// when this scraper breaks due to the reasons such as the website's layout changing.
        /// </summary>
        protected virtual async Task TestAsync(CancellationToken cancellationToken = default)
        {
            var manager = TestManager;

            if (manager != null)
                await manager.RunAsync(cancellationToken);
        }

        /// <summary>
        /// Scrapes and indexes data into the database.
        /// </summary>
        protected abstract Task RunAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the last saved state of this scraper.
        /// </summary>
        protected Task<T> GetStateAsync<T>(CancellationToken cancellationToken = default)
            => _storage.ReadObjectAsync<T>($"scrapers/{Type}/state", cancellationToken);

        /// <summary>
        /// Updates the saved state of this scraper.
        /// </summary>
        protected async Task SetStateAsync<T>(T value, CancellationToken cancellationToken = default)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug($"Saving scraper state {Type}: {JsonConvert.SerializeObject(value)}");

            await _storage.WriteObjectAsync($"scrapers/{Type}/state", value, cancellationToken);
        }

        /// <summary>
        /// Forcefully calls <see cref="RunAsync"/> immediately. This is for unit testing only.
        /// </summary>
        public Task ForceRunAsync(CancellationToken cancellationToken = default) => RunAsync(cancellationToken);
    }
}