using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace nhitomi
{
    /// <summary>
    /// Legacy resource locker implementation using <see cref="SemaphoreSlim"/>.
    /// </summary>
    public class SemaphoreResourceLocker : IResourceLocker
    {
        readonly object _lock = new object();
        readonly ILogger<SemaphoreResourceLocker> _logger;

        // use plain Dictionary, not ConcurrentDictionary
        readonly Dictionary<string, Lock> _locks = new Dictionary<string, Lock>();

        // contains reusable semaphores to avoid recreating every request
        readonly Stack<SemaphoreSlim> _pool = new Stack<SemaphoreSlim>();

        // the maximum capacity of the semaphore pool
        readonly int _poolCapacity;

        volatile bool _isDisposed;

        public SemaphoreResourceLocker(ILogger<SemaphoreResourceLocker> logger)
        {
            _logger = logger;

            // preallocate semaphores
            /*for (var i = 0; i < _poolCapacity; i++)
                _pool.Push(new SemaphoreSlim(1));*/

            // default pool capacity
            _poolCapacity = 100;
        }

        public Task<bool> IsConsumed(string key, CancellationToken cancellationToken = default)
        {
            Lock l;

            lock (_lock)
            {
                if (!_locks.TryGetValue(key, out l))
                    return Task.FromResult(false);
            }

            return Task.FromResult(l.Semaphore.Wait(TimeSpan.Zero));
        }

        public async Task<IAsyncDisposable> EnterAsync(string key, CancellationToken cancellationToken = default)
        {
            Lock l;

            lock (_lock)
            {
                // get or create a new lock
                if (!_locks.TryGetValue(key, out l))
                    _locks[key] = l = new Lock(this, key);

                // increment reference count
                ++l.References;
            }

            using (var measure = new MeasureContext())
            {
                // wait for this semaphore
                await l.Semaphore.WaitAsync(cancellationToken);

                if (measure.Seconds >= 1)
                    _logger.LogWarning($"Took {measure} to obtain lock for resource {key}.");
            }

            // we own this semaphore; assume caller calls Lock.Dispose
            return l;
        }

        sealed class Lock : IAsyncDisposable
        {
            readonly SemaphoreResourceLocker _manager;
            readonly string _id;

            public readonly SemaphoreSlim Semaphore;

            public Lock(SemaphoreResourceLocker manager, string id)
            {
                _manager = manager;
                _id      = id;

                // try reusing semaphores
                if (!_manager._pool.TryPop(out Semaphore))
                    Semaphore = new SemaphoreSlim(1);
            }

            // reference counter always modified within lock
            public int References;

            // called Lock.Dispose
            public ValueTask DisposeAsync()
            {
                lock (_manager._lock)
                {
                    // decrement reference count
                    if (--References == 0)
                    {
                        // we are the last reference to this lock
                        // return this semaphore to the pool if capacity not reached (to be reused later),
                        // if the manager is not disposed yet
                        if (_manager._pool.Count != _manager._poolCapacity && !_manager._isDisposed)
                        {
                            Semaphore.Release();

                            _manager._pool.Push(Semaphore);
                        }

                        // pool is full, so dispose semaphore and forget it
                        else
                        {
                            Semaphore.Dispose();
                        }

                        Trace.Assert(_manager._locks.Remove(_id), "someone hacked our semaphore dictionary");
                    }
                    else
                    {
                        // someone is still holding a reference to this lock; let them in
                        Semaphore.Release();
                    }
                }

                return default;
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_isDisposed)
                    return;

                while (_pool.TryPop(out var semaphore))
                    semaphore.Dispose();

                _isDisposed = true;
            }
        }
    }
}