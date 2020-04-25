using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fastenshtein;

namespace nhitomi.Scrapers.Tests
{
    public abstract class ScraperTest<T>
    {
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

            var dist = Levenshtein.Distance(a, b);

            if (dist > maxDifferences)
                throw new TestCaseException(this, $"{name}: mismatching '{a}' and '{b}'.");

            return dist;
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

            var set = a.ToHashSet();

            var missing = b.Distinct().Where(x => !set.Remove(x)).ToArray();

            if (missing.Length > maxDifferences)
                throw new TestCaseException(this, $"{name}: missing from collection a '{string.Join("', '", missing)}'.");

            maxDifferences -= missing.Length;

            if (set.Count > maxDifferences)
                throw new TestCaseException(this, $"{name}: missing from collection b '{string.Join("', '", set)}'.");

            maxDifferences -= set.Count;

            return maxDifferences.Value;
        }

        protected double Match(string name, double a, double b, double? precision = null)
        {
            precision ??= NumberMatchPrecision;

            var dist = Math.Abs(a - b);

            if (dist > precision)
                throw new TestCaseException(this, $"{name}: mismatching {a} and {b}.");

            return dist;
        }
    }

    [Serializable]
    public class TestCaseException : Exception
    {
        public TestCaseException(object test, string message) : base($"Error while running test case {test?.GetType().Name}. {message}") { }
    }
}