using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace nhitomi
{
    /// <summary>
    /// Reusable <see cref="TaskCompletionSource{TResult}"/>.
    /// </summary>
    public class TaskSignalSource<TResult>
    {
        TaskCompletionSource<TResult> _source = new TaskCompletionSource<TResult>();

        TaskCompletionSource<TResult> Reset() => Interlocked.Exchange(ref _source, new TaskCompletionSource<TResult>());

        /// <inheritdoc cref="TaskCompletionSource{TResult}.Task"/>
        public Task<TResult> Task => _source.Task;

        /// <inheritdoc cref="TaskCompletionSource{TResult}.TrySetException(System.Exception)"/>
        public bool TrySetException(Exception exception) => Reset().TrySetException(exception);

        /// <inheritdoc cref="TaskCompletionSource{TResult}.TrySetException(System.Collections.Generic.IEnumerable{System.Exception})"/>
        public bool TrySetException(IEnumerable<Exception> exceptions) => Reset().TrySetException(exceptions);

        /// <inheritdoc cref="TaskCompletionSource{TResult}.SetException(System.Exception)"/>
        public void SetException(Exception exception) => Reset().SetException(exception);

        /// <inheritdoc cref="TaskCompletionSource{TResult}.SetException(System.Collections.Generic.IEnumerable{System.Exception})"/>
        public void SetException(IEnumerable<Exception> exceptions) => Reset().SetException(exceptions);

        /// <inheritdoc cref="TaskCompletionSource{TResult}.TrySetResult"/>
        public bool TrySetResult(TResult result) => Reset().TrySetResult(result);

        /// <inheritdoc cref="TaskCompletionSource{TResult}.SetResult"/>
        public void SetResult(TResult result) => Reset().SetResult(result);

        /// <inheritdoc cref="TaskCompletionSource{TResult}.TrySetCanceled()"/>
        public bool TrySetCanceled() => Reset().TrySetCanceled();

        /// <inheritdoc cref="TaskCompletionSource{TResult}.TrySetCanceled(CancellationToken)"/>
        public bool TrySetCanceled(CancellationToken cancellationToken) => Reset().TrySetCanceled(cancellationToken);

        /// <inheritdoc cref="TaskCompletionSource{TResult}.SetCanceled"/>
        public void SetCanceled() => Reset().SetCanceled();
    }
}