using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace nhitomi
{
    /// <summary>
    /// Implements a thread-safe asynchronous producer-consumer queue with mostly identical interface of <see cref="Queue{T}"/>.
    /// </summary>
    public class AwaitableQueue<T> : IDisposable
    {
        readonly object _lock = new object();
        bool _disposed;

        readonly Queue<TaskCompletionSource<T>> _waiters;
        readonly Queue<T> _items;

        /// <inheritdoc cref="Queue{T}()"/>
        public AwaitableQueue()
        {
            _waiters = new Queue<TaskCompletionSource<T>>();
            _items   = new Queue<T>();
        }

        public AwaitableQueue(int capacity)
        {
            _waiters = new Queue<TaskCompletionSource<T>>(capacity);
            _items   = new Queue<T>(capacity);
        }

        public AwaitableQueue(IEnumerable<T> collection)
        {
            _waiters = new Queue<TaskCompletionSource<T>>();
            _items   = new Queue<T>(collection);
        }

        /// <inheritdoc cref="Queue{T}.Count"/>
        public int Count
        {
            get
            {
                lock (_lock)
                    return _items.Count;
            }
        }

        /// <inheritdoc cref="Queue{T}.Enqueue"/>
        public void Enqueue(T item)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AwaitableQueue<object>));

            lock (_lock)
            {
                // complete any waiters
                while (_waiters.TryDequeue(out var source))
                {
                    if (source.TrySetResult(item))
                        return;
                }

                // no waiters, so remember item
                _items.Enqueue(item);
            }
        }

        /// <summary>
        /// Removes and returns the object at the beginning of the <see cref="AwaitableQueue{T}"/> asynchronously, waiting for new items if the queue is empty.
        /// </summary>
        /// <returns>A task that returns the object that is removed from the beginning of the <see cref="AwaitableQueue{T}"/>.</returns>
        public async Task<T> DequeueAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_disposed)
                throw new ObjectDisposedException(nameof(AwaitableQueue<object>));

            TaskCompletionSource<T> source;

            lock (_lock)
            {
                // check for remembered items
                if (_items.TryDequeue(out var item))
                    return item;

                // create and add waiter
                source = new TaskCompletionSource<T>();

                _waiters.Enqueue(source);
            }

            await using (cancellationToken.Register(() => source.TrySetCanceled(cancellationToken)))
                return await source.Task;
        }

        /// <inheritdoc cref="Queue{T}.TryDequeue"/>
        public bool TryDequeue(out T item)
        {
            lock (_lock)
                return _items.TryDequeue(out item);
        }

        /// <inheritdoc cref="Queue{T}.TryPeek"/>
        public bool TryPeek(out T item)
        {
            lock (_lock)
                return _items.TryPeek(out item);
        }

        /// <inheritdoc cref="Queue{T}.Contains"/>
        public bool Contains(T item)
        {
            lock (_lock)
                return _items.Contains(item);
        }

        /// <inheritdoc cref="Queue{T}.Clear"/>
        public void Clear()
        {
            lock (_lock)
                _items.Clear();
        }

        /// <inheritdoc cref="Queue{T}.CopyTo"/>
        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (_lock)
                _items.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc cref="Queue{T}.ToArray"/>
        public T[] ToArray()
        {
            lock (_lock)
                return _items.ToArray();
        }

        /// <inheritdoc cref="Queue{T}.TrimExcess"/>
        public void TrimExcess()
        {
            lock (_lock)
            {
                _waiters.TrimExcess();
                _items.TrimExcess();
            }
        }

        public void Dispose()
        {
            _disposed = true;

            lock (_lock)
            {
                while (_waiters.TryDequeue(out var source))
                    source.TrySetCanceled();
            }
        }
    }
}