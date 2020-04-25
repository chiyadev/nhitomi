using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fastenshtein;
using Microsoft.Extensions.Logging;

namespace nhitomi.Scrapers.Tests
{
    public abstract class ScraperTest<T>
    {
        readonly ILogger<ScraperTest<T>> _logger; // logger may be null

        protected ScraperTest(ILogger<ScraperTest<T>> logger)
        {
            _logger = logger.IsEnabled(LogLevel.Debug) ? logger : null;
        }

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            var other = await GetAsync(cancellationToken);

            NotNull(nameof(other), other);
            Match(other);
        }

        protected abstract Task<T> GetAsync(CancellationToken cancellationToken = default);
        protected abstract void Match(T other);

        protected void NotNull(string name, object value)
        {
            if (value is null)
                throw new TestCaseException(this, $"{name}: expected not null but was null.");

            _logger?.LogDebug($"{name}: not null");
        }

        protected virtual int StringMatchMaxDifferences => 0;
        protected virtual int StringCollectionMatchMaxDifferences => 0;
        protected virtual double NumberMatchPrecision => 0;

        protected int Match(string name, string a, string b, bool ignoreCase = true, int? maxDifferences = null)
        {
            NotNull(name, a);
            NotNull(name, b);

            maxDifferences ??= StringMatchMaxDifferences;

            if (ignoreCase)
            {
                a = a.ToLowerInvariant();
                b = b.ToLowerInvariant();
            }

            var diff = Levenshtein.Distance(a, b);

            if (diff > maxDifferences)
                throw new TestCaseException(this, $"{name}: expected '{a}' but was '{b}'.");

            _logger?.LogDebug($"{name}: ~{diff} '{a}' == '{b}'");

            return diff;
        }

        protected int Match(string name, IEnumerable<string> a, IEnumerable<string> b, bool ignoreCase = true, int? maxDifferences = null)
        {
            NotNull(name, a);
            NotNull(name, b);

            maxDifferences ??= StringCollectionMatchMaxDifferences;

            if (ignoreCase)
            {
                a = a.Select(x => x?.ToLowerInvariant());
                b = b.Select(x => x?.ToLowerInvariant());
            }

            var xa = a.ToArray();
            var xb = b.ToArray();

            var set = xa.ToHashSet();

            var missing = xb.Distinct().Where(x => !set.Remove(x)).ToArray();

            if (missing.Length > maxDifferences)
                throw new TestCaseException(this, $"{name}: expected items '{string.Join("', '", missing)}' but was missing.");

            maxDifferences -= missing.Length;

            if (set.Count > maxDifferences)
                throw new TestCaseException(this, $"{name}: did not expect excess items '{string.Join("', '", set)}'.");

            maxDifferences -= set.Count;

            _logger?.LogDebug($"{name}: ~{maxDifferences} '{string.Join("', '", xa)}' == '{string.Join("', '", xb)}'");

            return maxDifferences.Value;
        }

        protected double Match(string name, double a, double b, double? precision = null)
        {
            precision ??= NumberMatchPrecision;

            var diff = Math.Abs(a - b);

            if (diff > precision)
                throw new TestCaseException(this, $"{name}: expected {a} but was {b}.");

            _logger?.LogDebug($"{name}: ~{diff} {a} == {b}");

            return diff;
        }
    }

    [Serializable]
    public class TestCaseException : Exception
    {
        public TestCaseException(object test, string message) : base($"Error while running test case {test?.GetType().Name}. {message}") { }
    }
}