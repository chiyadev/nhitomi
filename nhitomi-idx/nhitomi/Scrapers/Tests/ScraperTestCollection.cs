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
        Task RunAsync(CancellationToken cancellationToken = default);
    }

    public class ScraperTestCollection<T> : IScraperTestManager
    {
        public IReadOnlyCollection<ScraperTest<T>> Tests { get; }

        public ScraperTestCollection(IScraper scraper)
        {
            Tests = GetType()
                   .Assembly.GetTypes()
                   .Where(t => !t.IsAbstract && typeof(ScraperTest<T>).IsAssignableFrom(t))
                   .Select(t => (ScraperTest<T>) ActivatorUtilities.CreateInstance(scraper.Services, t, scraper))
                   .ToArray();
        }

        readonly Random _rand = new Random();

        public IEnumerable<ScraperTest<T>> Pick(int count)
        {
            lock (_rand)
                return Tests.OrderBy(x => _rand.Next()).Take(count).ToArray();
        }

        /// <summary>
        /// Number of tests to pick per run.
        /// </summary>
        public int RunTestCount { get; set; } = 1;

        /// <summary>
        /// If true, always retry failed tests before other tests.
        /// </summary>
        public bool RetryFailed { get; set; } = true;

        readonly ConcurrentQueue<ScraperTest<T>> _failed = new ConcurrentQueue<ScraperTest<T>>();

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            var tests = new HashSet<ScraperTest<T>>(RunTestCount);

            while (tests.Count < RunTestCount && _failed.TryDequeue(out var test))
                tests.Add(test);

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