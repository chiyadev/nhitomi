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
        int? PickCount { get; set; }

        /// <summary>
        /// True to run picked tests in parallel.
        /// </summary>
        bool Parallel { get; set; }

        /// <summary>
        /// True to always retry failed tests before other tests.
        /// </summary>
        bool PrioritizeFailed { get; set; }

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

        public int? PickCount { get; set; } = 1;
        public bool Parallel { get; set; } = true;
        public bool PrioritizeFailed { get; set; } = true;

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            var total  = PickCount ?? Tests.Count;
            var picked = new HashSet<ScraperTest<T>>(total);

            // add previously failed tests first
            while (picked.Count < total && _failed.TryDequeue(out var test))
                picked.Add(test);

            // add other tests
            lock (_rand)
            {
                foreach (var test in Tests.OrderBy(x => _rand.Next()))
                {
                    if (picked.Count < total)
                        picked.Add(test);
                }
            }

            // ReSharper disable AccessToDisposedClosure
            using var semaphore = new SemaphoreSlim(Parallel ? total : 1);

            await Task.WhenAll(picked.Select(async test =>
            {
                await semaphore.WaitAsync(cancellationToken);

                try
                {
                    await test.RunAsync(cancellationToken);
                }
                catch
                {
                    if (PrioritizeFailed && !_failed.Contains(test))
                        _failed.Enqueue(test);

                    throw;
                }
                finally
                {
                    semaphore.ReleaseSafe();
                }
            }));
            // ReSharper enable AccessToDisposedClosure
        }
    }
}