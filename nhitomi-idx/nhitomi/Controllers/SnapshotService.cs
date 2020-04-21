using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;
using nhitomi.Database;
using nhitomi.Models;
using nhitomi.Models.Queries;
using nhitomi.Storage;
using OneOf;
using OneOf.Types;

namespace nhitomi.Controllers
{
    public class SnapshotServiceOptions { }

    public interface ISnapshotService
    {
        Task<OneOf<DbSnapshot, NotFound>> GetAsync(ObjectType type, string id, CancellationToken cancellationToken = default); // not using nhitomiObject for semantic correctness
        Task<OneOf<T, NotFound>> GetValueAsync<T>(DbSnapshot snapshot, CancellationToken cancellationToken = default) where T : class, IDbObject;

        Task<SearchResult<DbSnapshot>> SearchAsync(ObjectType target, SnapshotQuery query, CancellationToken cancellationToken = default);

        Task<OneOf<T>> RollbackAsync<T>(DbSnapshot snapshot, CancellationToken cancellationToken = default) where T : class, IDbObject;

        Task<DbSnapshot> OnCreatedAsync<T>(T obj, SnapshotSource source, string committerId, string reason, CancellationToken cancellationToken = default) where T : class, IDbObject;
        Task<DbSnapshot> OnModifiedAsync<T>(T obj, SnapshotSource source, string committerId, string reason, CancellationToken cancellationToken = default) where T : class, IDbObject;
        Task<DbSnapshot> OnDeletedAsync(nhitomiObject obj, SnapshotSource source, string committerId, string reason, CancellationToken cancellationToken = default);
        Task<DbSnapshot> OnRolledBackAsync(DbSnapshot previous, SnapshotSource source, string committerId, string reason, CancellationToken cancellationToken = default);
    }

    public class SnapshotService : ISnapshotService
    {
        static readonly MessagePackSerializerOptions _serializerOptions =
            MessagePackSerializerOptions
               .Standard
               .WithCompression(MessagePackCompression.Lz4Block);

        readonly IElasticClient _client;
        readonly IStorage _storage;

        public SnapshotService(IElasticClient client, IStorage storage)
        {
            _client  = client;
            _storage = storage;
        }

        public async Task<OneOf<DbSnapshot, NotFound>> GetAsync(ObjectType type, string id, CancellationToken cancellationToken = default)
        {
            var snapshot = await _client.GetAsync<DbSnapshot>(id, cancellationToken);

            return snapshot?.Target == type ? snapshot : null;
        }

        public async Task<OneOf<T, NotFound>> GetValueAsync<T>(DbSnapshot snapshot, CancellationToken cancellationToken = default) where T : class, IDbObject
        {
            switch (snapshot.Type)
            {
                // creation and modification snapshots
                case SnapshotType.Creation:
                case SnapshotType.Modification:

                    // read serialized snapshot value
                    using (var file = await _storage.ReadAsync($"snapshots/{snapshot.Id}", cancellationToken))
                    {
                        if (file != null)
                        {
                            var value = MessagePackSerializer.Deserialize<T>(await file.Stream.ToArrayAsync(cancellationToken), _serializerOptions);

                            if (value != null)
                            {
                                // ensure value matches information in snapshot
                                if (value.Id != snapshot.TargetId || value.SnapshotTarget != snapshot.Target)
                                    throw new SnapshotMismatchException($"Snapshot {snapshot.Id} of {snapshot.Target} {snapshot.TargetId} does not match with value {value.SnapshotTarget}:{value.Id}");

                                return value;
                            }
                        }

                        throw new SnapshotMismatchException($"Serialized value of snapshot {snapshot.Id} could not be loaded.");
                    }

                // deletion snapshots
                case SnapshotType.Deletion:

                    // value is not recorded at deletion
                    return default;

                // rollback snapshots
                case SnapshotType.Rollback:

                    // value is not recorded at rollback, but rollback snapshot will contain reference to the target snapshot
                    var rollback = snapshot.RollbackId == null ? null : await GetAsync(snapshot.Target, snapshot.RollbackId, cancellationToken);

                    if (rollback == null || rollback.TargetId != snapshot.TargetId)
                        throw new SnapshotMismatchException($"Rollback snapshot {snapshot.Id} references invalid target snapshot: {rollback?.Id ?? "<null>"}");

                    // ReSharper disable once TailRecursiveCall
                    return await GetValueAsync<T>(rollback, cancellationToken);

                default:
                    throw new ArgumentOutOfRangeException(nameof(snapshot.Type), snapshot.Type, $"Unknown snapshot type: {snapshot.Type}");
            }
        }

        public Task<SearchResult<DbSnapshot>> SearchAsync(ObjectType target, SnapshotQuery query, CancellationToken cancellationToken = default)
            => _client.SearchAsync(new DbSnapshotQueryProcessor(target, query), cancellationToken);

        public async Task<OneOf<T>> RollbackAsync<T>(DbSnapshot snapshot, CancellationToken cancellationToken = default) where T : class, IDbObject
        {
            // get snapshot value
            var value = await GetValueAsync<T>(snapshot, cancellationToken);

            // get entry
            var entry = await _client.GetEntryAsync<T>(snapshot.TargetId, cancellationToken);

            do
            {
                if (entry.Value == null)
                {
                    // rolling deleted entry to deletion snapshot
                    if (value == null)
                        return (false, null);

                    entry.Value = value;

                    // restoring deleted entry
                    if (await entry.TryCreateAsync(cancellationToken))
                        return (true, value);
                }

                else if (value == null)
                {
                    // rolling entry to deletion snapshot
                    if (await entry.TryDeleteAsync(cancellationToken))
                        return (true, null);
                }

                else
                {
                    // avoid updating if nothing changes
                    if (entry.Value.DeepEqualTo(value))
                        return (false, value);

                    entry.Value = value;

                    // rolling entry to creation or modification snapshot
                    if (await entry.TryUpdateAsync(cancellationToken))
                        return (true, value);
                }
            }
            while (true);
        }

        public Task<DbSnapshot> OnCreatedAsync<T>(T obj, SnapshotSource source, string committerId, string reason, CancellationToken cancellationToken = default) where T : class, IDbObject
            => AddAsync(obj, () => new DbSnapshot
            {
                Type        = SnapshotType.Creation,
                Source      = source,
                CommitterId = committerId,
                Target      = obj.SnapshotTarget,
                TargetId    = obj.Id,
                Reason      = reason
            }, cancellationToken);

        public Task<DbSnapshot> OnModifiedAsync<T>(T obj, SnapshotSource source, string committerId, string reason, CancellationToken cancellationToken = default) where T : class, IDbObject
            => AddAsync(obj, () => new DbSnapshot
            {
                Type        = SnapshotType.Modification,
                Source      = source,
                CommitterId = committerId,
                Target      = obj.SnapshotTarget,
                TargetId    = obj.Id,
                Reason      = reason
            }, cancellationToken);

        public Task<DbSnapshot> OnDeletedAsync(nhitomiObject obj, SnapshotSource source, string committerId, string reason, CancellationToken cancellationToken = default)
            => AddAsync(() => new DbSnapshot
            {
                Type        = SnapshotType.Deletion,
                Source      = source,
                CommitterId = committerId,
                Target      = obj.Type,
                TargetId    = obj.Id,
                Reason      = reason
            }, cancellationToken);

        public Task<DbSnapshot> OnRolledBackAsync(DbSnapshot previous, SnapshotSource source, string committerId, string reason, CancellationToken cancellationToken = default)
            => AddAsync(() => new DbSnapshot
            {
                Type        = SnapshotType.Rollback,
                Source      = source,
                RollbackId  = previous.Id,
                CommitterId = committerId,
                Target      = previous.Target,
                TargetId    = previous.TargetId,
                Reason      = reason
            }, cancellationToken);

        async Task<DbSnapshot> AddAsync<T>(T target, Func<DbSnapshot> create, CancellationToken cancellationToken = default) where T : class, IDbObject, IDbSupportsSnapshot
        {
            if (target.Id == null)
                throw new ArgumentException($"Cannot create snapshot of {typeof(T).Name} with uninitialized ID: {target}");

            var snapshot = await AddAsync(create, cancellationToken);

            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (snapshot.Type)
            {
                case SnapshotType.Creation:
                case SnapshotType.Modification:

                    // upload serialized value
                    var file = new StorageFile
                    {
                        Name   = $"snapshots/{snapshot.Id}",
                        Stream = new MemoryStream(MessagePackSerializer.Serialize(target, _serializerOptions))
                    };

                    await _storage.WriteAsync(file, cancellationToken);
                    break;
            }

            return snapshot;
        }

        Task<DbSnapshot> AddAsync(Func<DbSnapshot> create, CancellationToken cancellationToken = default)
            => _client.Entry(create())
                      .CreateAsync(cancellationToken);
    }
}