using System;
using System.Threading;
using System.Threading.Tasks;

namespace nhitomi
{
    /// <summary>
    /// Represents a flexible task-based timeout that can be awaited and restarted.
    /// </summary>
    public class Timeout : IDisposable
    {
        readonly TimeSpan _timeout;
        readonly TaskCompletionSource<object> _completion = new TaskCompletionSource<object>();

        /// <summary>
        /// Task that completes when this timeout completes.
        /// </summary>
        public Task Task => _completion.Task;

        /// <summary>
        /// True to artificially complete this timeout on disposal; otherwise, task will be marked as canceled. Defaults to false.
        /// </summary>
        public bool CompleteOnDisposal { get; set; }

        public Timeout(TimeSpan timeout)
        {
            _timeout = timeout;

            Restart();
        }

        CancellationTokenSource _cts;

        /// <summary>
        /// Resets this timeout with a new timeout time.
        /// </summary>
        public void Reset(TimeSpan timeout)
        {
            lock (_completion)
            {
                // cancel last task
                _cts?.Cancel();
                _cts?.Dispose();

                // begin new task
                var cts = new CancellationTokenSource();

                Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(timeout, cts.Token);

                        _completion.TrySetResult(null);
                    }
                    catch (OperationCanceledException) { }
                }, cts.Token);

                _cts = cts;
            }
        }

        /// <summary>
        /// Restarts this timeout from the start.
        /// </summary>
        public void Restart() => Reset(_timeout);

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();

            if (CompleteOnDisposal)
                _completion.TrySetResult(null);
            else
                _completion.TrySetCanceled();
        }
    }
}