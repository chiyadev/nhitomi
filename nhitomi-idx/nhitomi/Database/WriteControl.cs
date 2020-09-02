using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace nhitomi.Database
{
    /// <summary>
    /// Controls when things can write to the database, and blocks writers when they shouldn't.
    /// This is similar to an rwlock but with reader and writer roles swapped.
    /// </summary>
    public interface IWriteControl
    {
        /// <summary>
        /// Begins blocking writers, and returns when all existing writers have finished. Blocks cannot be nested.
        /// </summary>
        Task BlockAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops blocking writers.
        /// </summary>
        Task UnblockAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Attempts to write to the database and throws if it is blocked.
        /// </summary>
        Task<IAsyncDisposable> EnterAsync(CancellationToken cancellationToken = default);
    }

    public class WriteControl : IWriteControl
    {
        readonly IOptionsMonitor<ServerOptions> _options;
        readonly IDynamicOptions _optionsSetter;
        readonly IRedisClient _redis;
        readonly ILogger<WriteControl> _logger;

        public WriteControl(IOptionsMonitor<ServerOptions> options, IDynamicOptions optionsSetter, IRedisClient redis, ILogger<WriteControl> logger)
        {
            _options       = options;
            _optionsSetter = optionsSetter;
            _redis         = redis;
            _logger        = logger;
        }

        const string _writerCountKey = "rctl";

        public async Task BlockAsync(CancellationToken cancellationToken = default)
        {
            await _optionsSetter.SetAsync($"Server:{nameof(ServerOptions.BlockDatabaseWrites)}", "true", cancellationToken);

            _logger.LogDebug("Blocking on write control.");

            // wait until all writers release
            while (true)
            {
                var count = await _redis.GetIntegerAsync(_writerCountKey, cancellationToken);

                if (count == 0)
                    break;

                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            }
        }

        public Task UnblockAsync(CancellationToken cancellationToken = default)
            => _optionsSetter.SetAsync($"Server:{nameof(ServerOptions.BlockDatabaseWrites)}", null, cancellationToken);

        public async Task<IAsyncDisposable> EnterAsync(CancellationToken cancellationToken = default)
        {
            var options = _options.CurrentValue;

            if (options.BlockDatabaseWrites)
                throw new WriteControlException("Database writes are currently blocked.");

            await _redis.IncrementIntegerAsync(_writerCountKey, 1, cancellationToken);

            return new WriterRelease(_redis);
        }

        sealed class WriterRelease : IAsyncDisposable
        {
            readonly IRedisClient _redis;

            public WriterRelease(IRedisClient redis)
            {
                _redis = redis;
            }

            public async ValueTask DisposeAsync()
                => await _redis.IncrementIntegerAsync(_writerCountKey, -1);
        }
    }

    [Serializable]
    public class WriteControlException : Exception
    {
        public WriteControlException() { }
        public WriteControlException(string message) : base(message) { }
        public WriteControlException(string message, Exception inner) : base(message, inner) { }

        protected WriteControlException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}