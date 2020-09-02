using System;
using System.Threading;
using System.Threading.Tasks;

namespace nhitomi
{
    public interface IResourceLocker : IDisposable
    {
        /// <summary>
        /// Returns true if a lock is already being consumed.
        /// </summary>
        Task<bool> IsConsumed(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Consumes a lock by the specified name and returns a disposable that will release it.
        /// </summary>
        Task<IAsyncDisposable> EnterAsync(string key, CancellationToken cancellationToken = default);
    }
}