using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace nhitomi
{
    public class AsyncEnumerableBrowser<T> : IAsyncEnumerator<T>
    {
        readonly IAsyncEnumerator<T> _enumerator;
        readonly Dictionary<int, T> _dict = new Dictionary<int, T>();

        public T Current => _dict[Index];
        public int Index { get; private set; } = -1;

        public AsyncEnumerableBrowser(IAsyncEnumerator<T> enumerator)
        {
            _enumerator = enumerator;
        }

        public async Task<bool> MoveNext(CancellationToken cancellationToken = default)
        {
            if (_dict.ContainsKey(Index + 1))
            {
                ++Index;
                return true;
            }

            if (!await _enumerator.MoveNext(cancellationToken))
                return false;

            _dict[++Index] = _enumerator.Current;
            return true;
        }

        public bool MovePrevious()
        {
            if (!_dict.ContainsKey(Index - 1))
                return false;

            --Index;
            return true;
        }

        public void Reset() => Index = -1;

        public void Dispose() => _enumerator.Dispose();
    }

    public static class EnumerableBrowserExtensions
    {
        public static AsyncEnumerableBrowser<T> CreateAsyncBrowser<T>(this IAsyncEnumerable<T> enumerable) =>
            new AsyncEnumerableBrowser<T>(enumerable.GetEnumerator());

        public static AsyncEnumerableBrowser<T> CreateAsyncBrowser<T>(this IEnumerable<T> enumerable) =>
            enumerable.ToAsyncEnumerable().CreateAsyncBrowser();
    }
}