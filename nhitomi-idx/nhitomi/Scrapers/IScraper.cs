using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    }

    public abstract class ScraperBase : BackgroundService, IScraper
    {
        readonly IResourceLocker _locker;
        readonly IStorage _storage;
        readonly IOptionsMonitor<ScraperOptions> _options;
        readonly ILogger<ScraperBase> _logger;

        public IServiceProvider Services { get; }
        public abstract ScraperType Type { get; }

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

                await Task.Delay(options.Interval, stoppingToken);

                try
                {
                    if (options.Enabled)
                        await using (await _locker.EnterAsync($"scrape:{Type}", stoppingToken))
                            await RunAsyncInternal(stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogInformation(e, $"Exception while scraping {Type}.");
                }
            }
        }

        internal async Task RunAsyncInternal(CancellationToken cancellationToken = default)
        {
            await TestAsync(cancellationToken);
            await RunAsync(cancellationToken);
        }

        protected virtual IScraperTestManager TestManager => null;

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
        /// This method is guaranteed to be synchronized.
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
        protected Task SetStateAsync<T>(T value, CancellationToken cancellationToken = default)
            => _storage.WriteObjectAsync($"scrapers/{Type}/state", value, cancellationToken);
    }
}