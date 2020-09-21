using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChiyaFlake;
using MessagePack;
using Microsoft.Extensions.Options;
using nhitomi.Database;
using nhitomi.Models;
using OneOf;
using OneOf.Types;
using StackExchange.Redis;

namespace nhitomi.Controllers
{
    public class DownloadServiceOptions
    {
        /// <summary>
        /// Maximum number of download sessions per user.
        /// </summary>
        public int MaxSessions { get; set; } = 2;

        /// <summary>
        /// <see cref="MaxSessions"/> for supporter users.
        /// </summary>
        public int MaxSessionsForSupporters { get; set; } = 5;

        /// <summary>
        /// Maximum concurrency of download requests.
        /// </summary>
        public int MaxConcurrency { get; set; } = 3;

        /// <summary>
        /// <see cref="MaxConcurrency"/> for supporter users.
        /// </summary>
        public int MaxConcurrencyForSupporters { get; set; } = 6;

        /// <summary>
        /// Delay until automatic session expiry.
        /// </summary>
        public TimeSpan ExpiryDelay { get; set; } = TimeSpan.FromSeconds(30);
    }

    [MessagePackObject]
    public class InternalDownloadSession
    {
        [Key("id")]
        public string Id { get; set; }

        [Key("Tr")]
        public DateTime CreatedTime { get; set; }

        [Key("co")]
        public int Concurrency { get; set; }

        public DownloadSession Convert() => new DownloadSession
        {
            Id          = Id,
            CreatedTime = CreatedTime,
            Concurrency = Concurrency
        };
    }

    public readonly struct MaxDownloadSessionsExceededError { }

    public readonly struct DownloadSessionConcurrencyError { }

    public interface IDownloadService
    {
        Task<OneOf<InternalDownloadSession, NotFound>> GetSessionAsync(string id, CancellationToken cancellationToken = default);
        Task<OneOf<InternalDownloadSession, MaxDownloadSessionsExceededError>> CreateSessionAsync(string userId, CancellationToken cancellationToken = default);
        Task<OneOf<Success, NotFound>> DeleteSessionAsync(string id, CancellationToken cancellationToken = default);

        Task<OneOf<Success, NotFound, DownloadSessionConcurrencyError>> AcquireResourceAsync(string id, CancellationToken cancellationToken = default);
        Task ReleaseResourceAsync(string id, CancellationToken cancellationToken = default);

        async Task<OneOf<IAsyncDisposable, NotFound, DownloadSessionConcurrencyError>> GetResourceContextAsync(string id, CancellationToken cancellationToken = default)
        {
            var result = await AcquireResourceAsync(id, cancellationToken);

            if (!result.TryPickT0(out _, out var error))
            {
                if (error.TryPickT0(out var a, out var b))
                    return a;

                return b;
            }

            return new ResourceContext(this, id);
        }

        sealed class ResourceContext : IAsyncDisposable
        {
            readonly IDownloadService _service;
            readonly string _id;

            public ResourceContext(IDownloadService service, string id)
            {
                _service = service;
                _id      = id;
            }

            int _disposed;

            public async ValueTask DisposeAsync()
            {
                if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
                    await _service.ReleaseResourceAsync(_id);
            }
        }
    }

    public class DownloadService : IDownloadService
    {
        readonly IOptionsMonitor<DownloadServiceOptions> _options;
        readonly IResourceLocker _locker;
        readonly IElasticClient _client;
        readonly IRedisClient _redis;

        public DownloadService(IOptionsMonitor<DownloadServiceOptions> options, IResourceLocker locker, IElasticClient client, IRedisClient redis)
        {
            _options = options;
            _locker  = locker;
            _client  = client;
            _redis   = redis;
        }

        public async Task<OneOf<InternalDownloadSession, NotFound>> GetSessionAsync(string id, CancellationToken cancellationToken = default)
        {
            // update expiry
            if (!await _redis.ExpireAsync($"dl:{id}", _options.CurrentValue.ExpiryDelay, cancellationToken))
                return new NotFound();

            var session = await _redis.GetObjectAsync<InternalDownloadSession>($"dl:{id}", cancellationToken);

            if (session == null)
                return new NotFound();

            return session;
        }

        public async Task<OneOf<InternalDownloadSession, MaxDownloadSessionsExceededError>> CreateSessionAsync(string userId, CancellationToken cancellationToken = default)
        {
            await using (await _locker.EnterAsync($"dl:user:{userId}", cancellationToken))
            {
                var now     = DateTime.UtcNow;
                var options = _options.CurrentValue;

                var user       = await _client.GetAsync<DbUser>(userId, cancellationToken);
                var sessionIds = new List<string>(await _redis.GetObjectAsync<string[]>($"dl:user:{userId}", cancellationToken) ?? Array.Empty<string>());

                if (sessionIds.Count != 0)
                {
                    var sessions = await _redis.GetObjectManyAsync<InternalDownloadSession>(sessionIds.ToArray(s => (RedisKey) $"dl:{s}"), cancellationToken);

                    // only consider active sessions
                    sessionIds.RemoveAll(id => Array.FindIndex(sessions, s => s?.Id == id) == -1);
                }

                var maxSessions    = options.MaxSessions;
                var maxConcurrency = options.MaxConcurrency;

                if (now < user?.SupporterInfo?.EndTime)
                {
                    maxSessions    = options.MaxSessionsForSupporters;
                    maxConcurrency = options.MaxConcurrencyForSupporters;
                }

                if (sessionIds.Count >= maxSessions)
                    return new MaxDownloadSessionsExceededError();

                var session = new InternalDownloadSession
                {
                    Id          = Snowflake.New,
                    CreatedTime = now,
                    Concurrency = maxConcurrency
                };

                sessionIds.Add(session.Id);

                if (await _redis.SetObjectAsync($"dl:{session.Id}", session, options.ExpiryDelay, cancellationToken: cancellationToken) &&
                    await _redis.SetObjectAsync($"dl:user:{userId}", sessionIds, cancellationToken: cancellationToken))
                    return session;

                return new MaxDownloadSessionsExceededError();
            }
        }

        public async Task<OneOf<Success, NotFound>> DeleteSessionAsync(string id, CancellationToken cancellationToken = default)
        {
            var result = await _redis.DeleteAsync($"dl:{id}", cancellationToken);

            if (!result)
                return new NotFound();

            return new Success();
        }

        public async Task<OneOf<Success, NotFound, DownloadSessionConcurrencyError>> AcquireResourceAsync(string id, CancellationToken cancellationToken = default)
        {
            var result = await GetSessionAsync(id, cancellationToken);

            if (!result.TryPickT0(out var session, out _))
                return new NotFound();

            var concurrency = await _redis.IncrementIntegerAsync($"dl:conc:{id}", 1, cancellationToken);

            if (concurrency > session.Concurrency)
            {
                await _redis.IncrementIntegerAsync($"dl:conc:{id}", -1, cancellationToken);

                return new DownloadSessionConcurrencyError();
            }

            return new Success();
        }

        public async Task ReleaseResourceAsync(string id, CancellationToken cancellationToken = default)
        {
            var concurrency = await _redis.IncrementIntegerAsync($"dl:conc:{id}", -1, cancellationToken);

            // this should never happen, but just in case it does, reset to default
            if (concurrency < 0)
                await _redis.DeleteAsync($"dl:conc:{id}", cancellationToken);
        }
    }
}