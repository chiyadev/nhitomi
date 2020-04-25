using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace nhitomi.Scrapers.Tests
{
    public interface IScraperTestManager
    {
        /// <summary>
        /// Number of tests to pick per run, or null to use all tests.
        /// </summary>
        int? RunTestCount { get; set; }

        /// <summary>
        /// If true, always retry failed tests before other tests.
        /// </summary>
        bool RetryFailed { get; set; }

        Task RunAsync(CancellationToken cancellationToken = default);
    }

    public class ScraperTestManager<T> : IScraperTestManager
    {
        public IReadOnlyCollection<ScraperTest<T>> Tests { get; }

        public ScraperTestManager(IScraper scraper)
        {
            Tests = GetType()
                   .Assembly.GetTypes()
                   .Where(t => !t.IsAbstract && typeof(ScraperTest<T>).IsAssignableFrom(t))
                   .Select(t => (ScraperTest<T>) ActivatorUtilities.CreateInstance(scraper.Services, t, scraper))
                   .ToArray();
        }

        readonly Random _rand = new Random();
        readonly ConcurrentQueue<ScraperTest<T>> _failed = new ConcurrentQueue<ScraperTest<T>>();

        public int? RunTestCount { get; set; } = 1;
        public bool RetryFailed { get; set; } = true;

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            var tests = new HashSet<ScraperTest<T>>(RunTestCount ?? Tests.Count);

            // add previously failed tests first
            while (tests.Count < RunTestCount && _failed.TryDequeue(out var test))
                tests.Add(test);

            // add other tests
            lock (_rand)
            {
                foreach (var test in Tests.OrderBy(x => _rand.Next()))
                {
                    if (tests.Count < RunTestCount)
                        tests.Add(test);
                }
            }

            await Task.WhenAll(tests.Select(async test =>
            {
                try
                {
                    await test.RunAsync(cancellationToken);
                }
                catch
                {
                    if (RetryFailed && !_failed.Contains(test))
                        _failed.Enqueue(test);

                    throw;
                }
            }));
        }
    }
}