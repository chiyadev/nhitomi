using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using nhitomi.Database;
using nhitomi.Models;
using nhitomi.Models.Queries;
using OneOf;
using OneOf.Types;

namespace nhitomi.Controllers
{
    public class UserServiceOptions
    {
        /// <summary>
        /// If true, assign the first created user administrator permissions on startup.
        /// </summary>
        public bool FirstUserAdmin { get; set; } = true;

        /// <summary>
        /// User permissions to grant to newly registered users.
        /// This will not apply to existing users.
        /// </summary>
        public UserPermissions DefaultPermissions { get; set; } = UserPermissions.None;

        /// <summary>
        /// Lifetime of the access token in user sessions before expiry.
        /// This can be long because access tokens can be invalidated on-demand.
        /// </summary>
        public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromDays(90);
    }

    public interface IUserService
    {
        /// <summary>
        /// Creates an user info object without adding it to the database.
        /// </summary>
        DbUser MakeUserObject();

        Task<OneOf<DbUser, NotFound>> GetAsync(string id, CancellationToken cancellationToken = default);
        Task<SearchResult<DbUser>> SearchAsync(UserQuery query, CancellationToken cancellationToken = default);

        Task<int> CountAsync(CancellationToken cancellationToken = default);

        Task<OneOf<DbUser, NotFound>> UpdateAsync(string id, UserBase user, SnapshotArgs snapshot, CancellationToken cancellationToken = default);

        Task<OneOf<DbUser, NotFound>> RestrictAsync(string id, string moderatorId, TimeSpan? duration, SnapshotArgs snapshot, CancellationToken cancellationToken = default);
        Task<OneOf<DbUser, NotFound>> UnrestrictAsync(string id, SnapshotArgs snapshot, CancellationToken cancellationToken = default);
    }

    public class UserService : IUserService
    {
        readonly IOptionsMonitor<UserServiceOptions> _options;
        readonly IElasticClient _client;
        readonly ISnapshotService _snapshots;

        public UserService(IOptionsMonitor<UserServiceOptions> options, IElasticClient client, ISnapshotService snapshots)
        {
            _options   = options;
            _client    = client;
            _snapshots = snapshots;
        }

        public DbUser MakeUserObject() => new DbUser
        {
            Restrictions = Array.Empty<DbUserRestriction>(),
            Permissions  = _options.CurrentValue.DefaultPermissions.ToFlags()
        };

        public async Task<OneOf<DbUser, NotFound>> GetAsync(string id, CancellationToken cancellationToken = default)
        {
            var user = await _client.GetAsync<DbUser>(id, cancellationToken);

            if (user == null)
                return new NotFound();

            return user;
        }

        public Task<SearchResult<DbUser>> SearchAsync(UserQuery query, CancellationToken cancellationToken = default)
            => _client.SearchAsync(new DbUserQueryProcessor(query), cancellationToken);

        public Task<int> CountAsync(CancellationToken cancellationToken = default)
            => _client.CountAsync<DbUser>(cancellationToken);

        public async Task<OneOf<DbUser, NotFound>> UpdateAsync(string id, UserBase user, SnapshotArgs snapshot, CancellationToken cancellationToken = default)
        {
            var entry = await _client.GetEntryAsync<DbUser>(id, cancellationToken);

            do
            {
                if (entry.Value == null)
                    return new NotFound();

                if (!entry.Value.TryApplyBase(user))
                    break;
            }
            while (!await entry.TryUpdateAsync(cancellationToken));

            if (snapshot != null)
                await _snapshots.CreateAsync(entry.Value, snapshot, cancellationToken);

            return entry.Value;
        }

        public async Task<OneOf<DbUser, NotFound>> RestrictAsync(string id, string moderatorId, TimeSpan? duration, SnapshotArgs snapshot, CancellationToken cancellationToken = default)
        {
            var entry = await _client.GetEntryAsync<DbUser>(id, cancellationToken);

            do
            {
                if (entry.Value == null)
                    return new NotFound();

                var list = new List<DbUserRestriction>();

                if (entry.Value.Restrictions != null)
                    list.AddRange(entry.Value.Restrictions);

                var start = DateTime.UtcNow;

                // if the user already has an active restriction, add to the duration
                var last = list.LastOrDefault();

                if (last != null && start < last.EndTime)
                    start = last.EndTime ?? start;

                var end = start as DateTime?;

                if (duration == null)
                    end = null;

                // append to the list of restrictions
                list.Add(new DbUserRestriction
                {
                    StartTime   = start,
                    EndTime     = end,
                    ModeratorId = moderatorId,
                    Reason      = snapshot?.Reason
                });

                entry.Value.Restrictions = list.ToArray();
            }
            while (!await entry.TryUpdateAsync(cancellationToken));

            if (snapshot != null)
                await _snapshots.CreateAsync(entry.Value, snapshot, cancellationToken);

            return entry.Value;
        }

        public async Task<OneOf<DbUser, NotFound>> UnrestrictAsync(string id, SnapshotArgs snapshot, CancellationToken cancellationToken = default)
        {
            var entry = await _client.GetEntryAsync<DbUser>(id, cancellationToken);

            do
            {
                if (entry.Value == null)
                    return new NotFound();

                if (entry.Value.Restrictions == null)
                    break;

                var now = DateTime.UtcNow;

                var changed = false;

                foreach (var restriction in entry.Value.Restrictions)
                {
                    // currently active restrictions
                    if (restriction.StartTime <= now && now < restriction.EndTime)
                    {
                        // move restriction end time to current time, ending it immediately
                        restriction.EndTime = now;

                        changed = true;
                    }

                    // future restrictions
                    else if (now < restriction.StartTime)
                    {
                        // move restriction end time to start time, which invalidates it
                        restriction.EndTime = restriction.StartTime;

                        changed = true;
                    }
                }

                if (!changed)
                    break;
            }
            while (!await entry.TryUpdateAsync(cancellationToken));

            if (snapshot != null)
                await _snapshots.CreateAsync(entry.Value, snapshot, cancellationToken);

            return entry.Value;
        }
    }
}