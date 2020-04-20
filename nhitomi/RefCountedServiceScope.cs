using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace nhitomi
{
    /// <summary>
    /// <see cref="IServiceScope"/> that disposes itself when all references to it are lost.
    /// </summary>
    public class RefCountedServiceScope : IServiceScope
    {
        int _count;

        readonly IServiceScope _scope;

        public IServiceProvider ServiceProvider => _scope.ServiceProvider;

        public RefCountedServiceScope(IServiceScope scope)
        {
            _scope = scope;
            _count = 1; // ctor caller holds the initial reference
        }

        /// <summary>
        /// Increments the reference count once and returns a <see cref="IServiceScope"/> that will decrement once when disposed.
        /// </summary>
        public IServiceScope CreateReference()
        {
            Interlocked.Increment(ref _count);
            return this;
        }

        public void Dispose()
        {
            if (Interlocked.Decrement(ref _count) == 0)
                _scope.Dispose();
        }
    }
}