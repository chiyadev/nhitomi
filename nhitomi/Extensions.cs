using System;
using System.Threading;
using System.Threading.Tasks;

namespace nhitomi
{
    public static class Extensions
    {
        sealed class DisposableSemaphoreReleaseContext : IDisposable
        {
            readonly SemaphoreSlim _semaphore;

            public DisposableSemaphoreReleaseContext(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
            }

            public void Dispose() => _semaphore.ReleaseSafe();
        }

        /// <summary>
        /// Waits on this semaphore and returns a value that can release on disposal.
        /// </summary>
        public static async Task<IDisposable> EnterAsync(this SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
        {
            await semaphore.WaitAsync(cancellationToken);
            return new DisposableSemaphoreReleaseContext(semaphore);
        }

        /// <summary>
        /// Safely releases a semaphore handling <see cref="ObjectDisposedException"/>.
        /// </summary>
        public static void ReleaseSafe(this SemaphoreSlim semaphore)
        {
            try
            {
                semaphore.Release();
            }
            catch (ObjectDisposedException) { }
        }
    }
}