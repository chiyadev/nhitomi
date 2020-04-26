using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace nhitomi.Discord
{
    public class InteractiveRenderer : IDisposable
    {
        readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        readonly TaskSignalSource<object> _signaller = new TaskSignalSource<object>();
        readonly CancellationTokenSource _cts = new CancellationTokenSource();

        readonly InteractiveMessage _message;
        readonly IReplyRenderer _renderer;
        readonly ILogger<InteractiveRenderer> _logger;
        readonly IOptionsMonitor<DiscordOptions> _options;

        public InteractiveRenderer(InteractiveMessage message, IReplyRenderer renderer, ILogger<InteractiveRenderer> logger, IOptionsMonitor<DiscordOptions> options)
        {
            _message  = message;
            _renderer = renderer;
            _logger   = logger;
            _options  = options;
        }

        public async Task<IUserMessage> RenderInitialAsync(CancellationToken cancellationToken = default)
        {
            using (await _message.Semaphore.EnterAsync(cancellationToken))
                return await _renderer.SendAsync(_message.Command, _message, cancellationToken);
        }

        public Task RenderAsync(CancellationToken cancellationToken = default)
        {
            var _ = cancellationToken;

            BeginRerender(_cts.Token);

            return _signaller.Task;
        }

        volatile bool _enqueued;

        void BeginRerender(CancellationToken cancellationToken = default)
        {
            // enqueue rerender
            _enqueued = true;

            // try entering rerender semaphore and if we succeeded, begin rerender loop
            if (!_semaphore.Wait(TimeSpan.Zero))
                return;

            Task.Run(async () =>
            {
                _logger.LogDebug($"Entering interactive rendering loop for message {_message}.");

                try
                {
                    while (_enqueued)
                    {
                        _enqueued = false;

                        using (await _message.Semaphore.EnterAsync(cancellationToken))
                        {
                            try
                            {
                                // rerender interactive
                                var result = await _renderer.ModifyAsync(_message.Reply, _message, cancellationToken);

                                // if false, expire interactive
                                if (!result)
                                {
                                    _message.Dispose();
                                    return;
                                }

                                // signal success
                                _signaller.TrySetResult(null);
                            }
                            catch (Exception e)
                            {
                                // signal fail
                                _signaller.TrySetException(e);
                            }
                        }

                        // sleep interval
                        await Task.Delay(_options.CurrentValue.Interactive.RenderInterval, cancellationToken);
                    }
                }
                finally
                {
                    _logger.LogDebug($"Exiting interactive rendering loop for message {_message}.");

                    _semaphore.ReleaseSafe();
                }
            }, cancellationToken);
        }

        public void Dispose()
        {
            _semaphore.Dispose();
            _signaller.TrySetCanceled();

            _cts.Cancel();
            _cts.Dispose();
        }
    }
}