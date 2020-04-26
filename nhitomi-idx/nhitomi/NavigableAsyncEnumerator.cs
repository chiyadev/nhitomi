using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace nhitomi
{
    /// <summary>
    /// Supports a complex asynchronous iteration over a generic collection.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the collection.</typeparam>
    public interface INavigableAsyncEnumerator<out T> : IAsyncEnumerator<T>
    {
        /// <summary>
        /// Gets the current zero-based enumeration position.
        /// </summary>
        int Position { get; }

        /// <summary>
        /// Retreats the enumerator asynchronously to the previous element of the collection.
        /// </summary>
        /// <returns>A <see cref="ValueTask{TResult}"/> that will complete with a result of true if the enumerator was successfully retreated to the previous element, or false if the enumerator has passed the beginning of the collection.</returns>
        ValueTask<bool> MovePreviousAsync();

        /// <summary>
        /// Moves the enumerator asynchronously to the element at the specified index.
        /// </summary>
        /// <param name="index">Zero-based index of the element to move to.</param>
        /// <returns>A <see cref="ValueTask{TResult}"/> that will complete with a result of true if the enumerator was successfully moved to the element at the specified index, or false if the enumerator has passed the beginning or the end of the collection.</returns>
        ValueTask<bool> MoveToAsync(int index);
    }

    /// <summary>
    /// Implements <see cref="INavigableAsyncEnumerator{T}"/> using an underlying enumerator implementation.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the collection.</typeparam>
    public class NavigableAsyncEnumerator<T> : INavigableAsyncEnumerator<T>
    {
        readonly IAsyncEnumerator<T> _enumerator;
        readonly List<T> _cache = new List<T>();

        /// <summary>
        /// Constructs an <see cref="NavigableAsyncEnumerator{T}"/>.
        /// </summary>
        /// <param name="enumerator">The underlying enumerator implementation to use.</param>
        public NavigableAsyncEnumerator(IAsyncEnumerator<T> enumerator)
        {
            _enumerator = enumerator;
        }

        public T Current => _cache[Position];
        public int Position { get; private set; } = -1;

        /// <summary>
        /// Gets the actual position of the underlying enumerator.
        /// </summary>
        public int EnumerationPosition => _cache.Count - 1;

        public ValueTask<bool> MoveNextAsync() => MoveToAsync(Position + 1);
        public ValueTask<bool> MovePreviousAsync() => MoveToAsync(Position - 1);

        public async ValueTask<bool> MoveToAsync(int index)
        {
            if (index < 0)
                return false;

            while (EnumerationPosition < index)
            {
                if (await _enumerator.MoveNextAsync())
                    _cache.Add(_enumerator.Current);

                else
                    return false;
            }

            Position = index;
            return true;
        }

        public ValueTask DisposeAsync()
        {
            Position = -1;
            _cache.Clear();

            return _enumerator.DisposeAsync();
        }
    }

    public static class NavigableAsyncEnumeratorExtensions
    {
        /// <summary>
        /// Returns a navigable enumerator that iterates asynchronously through the collection.
        /// </summary>
        /// <param name="enumerable">The collection to create a navigable iterator of.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that may be used to cancel the asynchronous navigation.</param>
        /// <typeparam name="T">The type of the elements in the collection.</typeparam>
        /// <returns>A navigable enumerator that can be used to iterate asynchronously through the collection.</returns>
        public static INavigableAsyncEnumerator<T> GetNavigableAsyncEnumerator<T>(this IAsyncEnumerable<T> enumerable, CancellationToken cancellationToken = default)
        {
            var enumerator = enumerable.GetAsyncEnumerator(cancellationToken);

            return new NavigableAsyncEnumerator<T>(enumerator);
        }
    }
}