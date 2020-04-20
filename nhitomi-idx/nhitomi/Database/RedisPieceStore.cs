using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nhitomi.Models;
using StackExchange.Redis;

namespace nhitomi.Database
{
    public class RedisPieceStore : IPieceStore
    {
        readonly IOptionsMonitor<PieceStoreOptions> _options;
        readonly IRedisClient _client;
        readonly IResourceLocker _locker;
        readonly IServiceProvider _services;
        readonly ILogger<RedisPieceStore> _logger;

        readonly CancellationTokenSource _providerCheckCts = new CancellationTokenSource();
        readonly CancellationTokenSource _objectRefreshCts = new CancellationTokenSource();

        public RedisPieceStore(IOptionsMonitor<PieceStoreOptions> options, IRedisClient client, IResourceLocker locker, IServiceProvider services, ILogger<RedisPieceStore> logger)
        {
            _options  = options;
            _client   = client;
            _locker   = locker;
            _services = services;
            _logger   = logger;

            Task.Run(() => CheckProvidersAsync(_providerCheckCts.Token));
            Task.Run(() => CheckObjectsAsync(_objectRefreshCts.Token));
        }

        public TimeSpan ProviderRefreshInterval => _options.CurrentValue.InactiveProviderExpirationTime / 2; // refresh every half of expiration time

        IAvailabilityService GetAvailabilityService(nhitomiObject obj) => obj.Type switch
        {
            SnapshotTarget.Book => _services.GetService<IBookService>(),

            _ => throw new ArgumentException($"Unsupported object type {obj.Type}.")
        };

        /// <summary>
        /// Sorted set containing client IDs of all providers that have refreshed.
        /// ID is the value and latest refresh time is the score.
        /// Providers that don't refresh on time will expire and have all announced objects renounced.
        /// </summary>
        IRedisSortedSetHandler RefreshedProviders => _client.SortedSet("pi:refreshed");

        /// <summary>
        /// Sorted set containing objects that are pending refresh.
        /// Serialized object reference (<see cref="nhitomiObject.Serialize"/>) is the value and next scheduled refresh time + rescheduling information is the score.
        /// </summary>
        IRedisSortedSetHandler ScheduledObjects => _client.SortedSet("pi:scheduled");

        static readonly RedisKey _providerObjectsPrefix = "po:";

        /// <summary>
        /// Set containing objects that a specific client is providing.
        /// Serialized object reference is the value.
        /// This set must be locked using <see cref="GetProviderObjectsLockAsync"/> during modification.
        /// </summary>
        IRedisSetHandler ProviderObjects(long clientId)
        {
            var bytes = BitConverter.GetBytes(clientId);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            return _client.Set(_providerObjectsPrefix.Append(bytes));
        }

        /// <summary>
        /// Returns the disposable lock for set handler returned by <see cref="ProviderObjects"/>.
        /// </summary>
        Task<IAsyncDisposable> GetProviderObjectsLockAsync(long clientId, CancellationToken cancellationToken = default)
            => _locker.EnterAsync($"po:{clientId}", cancellationToken);

        static readonly RedisKey _pieceProvidersPrefix = "p:";

        /// <summary>
        /// Set containing IDs of providers that are providing a piece, given by its hash.
        /// Client ID is the value.
        /// </summary>
        IRedisSetHandler PieceProviders(byte[] hash) => _client.Set(_pieceProvidersPrefix.Append(hash));

        static readonly RedisKey _objectPiecesPrefix = "px:";

        /// <summary>
        /// Set containing all pieces of an object.
        /// Piece hash is the value.
        /// </summary>
        IRedisSetHandler ObjectPieces(nhitomiObject obj) => _client.Set(_objectPiecesPrefix.Append(obj.Serialize()));

        public async Task<bool> ProviderExists(long clientId, CancellationToken cancellationToken = default)
            => await RefreshedProviders.ContainsAsync((RedisValue) clientId, cancellationToken);

        public async Task<int> CountProvidersAsync(CancellationToken cancellationToken = default)
            => (int) await RefreshedProviders.CountAsync(cancellationToken);

        public async Task AddProviderAsync(long clientId, nhitomiObject obj, byte[][] hashes, CancellationToken cancellationToken = default)
        {
            await using (await GetProviderObjectsLockAsync(clientId, cancellationToken))
            {
                // automatically refresh
                await RefreshProviderAsync(clientId, cancellationToken);

                // add object to announced object list
                await ProviderObjects(clientId).AddAsync(obj.Serialize(), cancellationToken);

                // add client to piece provider lists
                await Task.WhenAll(hashes.Select(h => PieceProviders(h).AddAsync(clientId, cancellationToken)));

                // update object availability
                await RefreshObjectAsync(obj, cancellationToken);
            }
        }

        public async Task<long[]> GetProvidersAsync(byte[] hash, CancellationToken cancellationToken = default)
        {
            // no lock needed for read
            var set = PieceProviders(hash);

            var values = (await set.GetAsync(cancellationToken)).ToList(v => (long) v);

            // remove nonexistent providers (this *can* happen in some edge cases)
            var remove = new List<long>(0);

            foreach (var id in values)
            {
                if (!await RefreshedProviders.ContainsAsync((RedisValue) id, cancellationToken))
                    remove.Add(id);
            }

            if (remove.Count != 0)
            {
                await Task.WhenAll(remove.Select(id => set.RemoveAsync(id, cancellationToken)));

                foreach (var id in remove)
                    values.Remove(id);
            }

            return values.ToArray();
        }

        public async Task<int> CountProvidersAsync(byte[] hash, CancellationToken cancellationToken = default)
            => (int) await PieceProviders(hash).CountAsync(cancellationToken);

        public Task RefreshProviderAsync(long clientId, CancellationToken cancellationToken = default)
        {
            // move to expiration time relative from now
            var time = (DateTime.UtcNow + _options.CurrentValue.InactiveProviderExpirationTime).Ticks;

            return RefreshedProviders.AddOrUpdateAsync(clientId, time, cancellationToken);
        }

        public async Task RemoveProviderAsync(long clientId, nhitomiObject obj, CancellationToken cancellationToken = default)
        {
            await using (await GetProviderObjectsLockAsync(clientId, cancellationToken))
                await RemoveProviderAsyncInternal(clientId, obj, cancellationToken);
        }

        public async Task RemoveProviderAsync(long clientId, CancellationToken cancellationToken = default)
        {
            await using (await GetProviderObjectsLockAsync(clientId, cancellationToken))
            {
                // get all objects announced by this provider
                var objects = await GetObjectsAsync(clientId, cancellationToken);

                // remove as provider of these objects
                await Task.WhenAll(objects.Select(o => RemoveProviderAsyncInternal(clientId, o, cancellationToken)));

                // remove from refresh list
                await RefreshedProviders.RemoveAsync((RedisValue) clientId, cancellationToken);
            }
        }

        async Task RemoveProviderAsyncInternal(long clientId, nhitomiObject obj, CancellationToken cancellationToken = default)
        {
            // get all pieces of the object
            var pieces = await GetObjectPiecesAsync(obj, cancellationToken);

            // remove client from piece provider lists
            await Task.WhenAll(pieces.Select(p => PieceProviders(p).RemoveAsync(clientId, cancellationToken)));

            // remove object from announced object list
            await ProviderObjects(clientId).RemoveAsync(obj.Serialize(), cancellationToken);

            // update object availability
            await RefreshObjectAsync(obj, cancellationToken);
        }

        /// <summary>Background provider expiration task.</summary>
        async Task CheckProvidersAsync(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await using (await _locker.EnterAsync("pi:refreshed", cancellationToken))
                    {
                        while (true)
                        {
                            // find providers not refreshed on time
                            var providers = await RefreshedProviders.RangeAsync(double.NegativeInfinity, DateTime.UtcNow.Ticks, cancellationToken);

                            if (providers.Length == 0)
                                break;

                            await Task.WhenAll(providers.Select(v => RemoveProviderAsync((long) v, cancellationToken)));
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Exception while removing expired providers.");
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        public async Task<nhitomiObject[]> GetObjectsAsync(long clientId, CancellationToken cancellationToken = default)
        {
            // no lock needed for read
            var values = await ProviderObjects(clientId).GetAsync(cancellationToken);

            return values.ToArray(v => nhitomiObject.Deserialize(v));
        }

        public async Task RefreshObjectAsync(nhitomiObject obj, CancellationToken cancellationToken = default)
        {
            var options = _options.CurrentValue;

            var objSerialized = obj.Serialize();

            // refresh immediately if not deferred
            if (!options.DeferredObjectRefresh)
            {
                await RefreshObjectAsyncInternal(obj, 0, cancellationToken);
                return;
            }

            var score = await ScheduledObjects.GetAsync(objSerialized, cancellationToken);

            if (score == null)
            {
                await RefreshObjectAsyncInternal(obj, 0, cancellationToken);
                return;
            }

            var (scheduledTime, _) = DeserializeScheduledObjectScore(score.Value);

            var next = DateTime.UtcNow + options.RefreshIntervalMinimum;

            // already scheduled to refresh soon
            // don't return as we still need to reset reschedule count
            if (next > scheduledTime)
                next = scheduledTime;

            await ScheduledObjects.AddOrUpdateAsync(objSerialized, SerializeScheduledObjectScore(next, 0), cancellationToken);
        }

        public async Task<byte[][]> GetObjectPiecesAsync(nhitomiObject obj, CancellationToken cancellationToken = default)
        {
            var set = ObjectPieces(obj);

            var values = await set.GetAsync(cancellationToken);

            if (values.Length != 0)
                return values.ToArray(v => (byte[]) v);

            return await LoadObjectPiecesAsync(set, obj, cancellationToken);
        }

        public async Task<bool> ObjectContainsPiecesAsync(nhitomiObject obj, byte[][] hashes, CancellationToken cancellationToken = default)
        {
            var set = ObjectPieces(obj);

            // fill set if empty
            if (await set.CountAsync(cancellationToken) == 0)
                await LoadObjectPiecesAsync(set, obj, cancellationToken);

            var results = await Task.WhenAll(hashes.Select(v => set.ContainsAsync(v, cancellationToken)));

            for (var i = 0; i < hashes.Length; i++)
            {
                if (!results[i])
                    return false;
            }

            return true;
        }

        async Task<byte[][]> LoadObjectPiecesAsync(IRedisSetHandler set, nhitomiObject obj, CancellationToken cancellationToken = default)
        {
            var hashes = (await GetAvailabilityService(obj).EnumeratePiecesAsync(obj.Id, cancellationToken)).ToArray(p => p.Hash);

            await Task.WhenAll(hashes.Select(v => set.AddAsync(v, cancellationToken)));

            return hashes;
        }

        /// <summary>
        /// Serializes object scheduled time and rescheduling information into one double value that is sortable.
        /// </summary>
        static double SerializeScheduledObjectScore(DateTime time, int reschedules)
        {
            // millisecond precision is good enough
            var x = time.Ticks / 10000;

            // clamp and use the last byte for number of reschedules
            x = (x & ~0xff) | ((uint) Math.Clamp(reschedules, 0, 0xff) & 0xff);

            return x;
        }

        static (DateTime time, int reschedules) DeserializeScheduledObjectScore(double score)
        {
            var x = (long) score;

            var ticks       = x * 10000;
            var reschedules = (int) (x & 0xff);

            return (new DateTime(ticks), reschedules);
        }

        async Task RefreshObjectAsyncInternal(nhitomiObject obj, int reschedules, CancellationToken cancellationToken = default)
        {
            var options = _options.CurrentValue;

            // update availability score
            if (!await GetAvailabilityService(obj).UpdateAvailabilityAsync(obj.Id, cancellationToken))
            {
                // if returned false, object was deleted
                await ScheduledObjects.RemoveAsync(obj.Serialize(), cancellationToken);

                return;
            }

            // calculate next refresh time
            var minInterval = options.RefreshIntervalMinimum;
            var maxInterval = options.RefreshIntervalMaximum ?? await GetDynamicMaxObjectRefreshIntervalAsync(options, cancellationToken);

            if (maxInterval < minInterval)
                maxInterval = minInterval;

            var interval = new TimeSpan((long) Math.Clamp(minInterval.Ticks * Math.Pow(options.RefreshIntervalMultiple, ++reschedules), minInterval.Ticks, maxInterval.Ticks));

            var next = DateTime.UtcNow + interval;

            await ScheduledObjects.AddOrUpdateAsync(obj.Serialize(), SerializeScheduledObjectScore(next, reschedules), cancellationToken);
        }

        async Task<TimeSpan> GetDynamicMaxObjectRefreshIntervalAsync(PieceStoreOptions options, CancellationToken cancellationToken = default)
        {
            var total = await ScheduledObjects.CountAsync(cancellationToken);

            // prevent objects from refreshing too infrequently, but allow some interval as the number of objects scales
            return TimeSpan.FromSeconds(total / (options.RefreshIntervalMinimum.TotalSeconds * options.RefreshIntervalMultiple));
        }

        /// <summary>Background object refresh task.</summary>
        async Task CheckObjectsAsync(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await using (await _locker.EnterAsync("pi:scheduled", cancellationToken))
                    {
                        while (true)
                        {
                            // find objects that are pending refresh
                            var entries = await ScheduledObjects.RangeScoresAsync(double.NegativeInfinity, SerializeScheduledObjectScore(DateTime.UtcNow, int.MaxValue), cancellationToken);

                            if (entries.Length == 0)
                                break;

                            await Task.WhenAll(entries.Select(x =>
                            {
                                var obj = nhitomiObject.Deserialize(x.Element);

                                var (_, reschedules) = DeserializeScheduledObjectScore(x.Score);

                                return RefreshObjectAsyncInternal(obj, reschedules, cancellationToken);
                            }));
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Exception while removing expired providers.");
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        public void Dispose()
        {
            _providerCheckCts.Cancel();
            _providerCheckCts.Dispose();

            _objectRefreshCts.Cancel();
            _objectRefreshCts.Dispose();
        }
    }
}