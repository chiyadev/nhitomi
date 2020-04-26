using System;
using System.Threading;
using System.Threading.Tasks;

namespace nhitomi
{
    public interface IResourceLocker : IDisposable
    {
        Task<IAsyncDisposable> EnterAsync(string key, CancellationToken cancellationToken = default);
    }
}