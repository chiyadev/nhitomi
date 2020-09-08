using System;
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
        public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromDays(365 * 100); //TimeSpan.FromDays(90); practically never expiring
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

        Task<OneOf<DbUser, NotFound>> AddSupporterDurationAsync(string id, TimeSpan duration, double spending, CancellationToken cancellationToken = default);
    }

    public class UserService : IUserService
    {
        readonly IServiceProvider _services;
        readonly IOptionsMonitor<UserServiceOptions> _options;
        readonly IElasticClient _client;
        readonly ISnapshotService _snapshots;

        public UserService(IServiceProvider services, IOptionsMonitor<UserServiceOptions> options, IElasticClient client, ISnapshotService snapshots)
        {
            _services  = services;
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

                if (!entry.Value.TryApplyBase(user, _services))
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

                var now = DateTime.UtcNow;

                var restriction = new DbUserRestriction
                {
                    StartTime   = now,
                    EndTime     = now + duration,
                    ModeratorId = moderatorId,
                    Reason      = snapshot?.Reason
                };

                entry.Value.Restrictions = (entry.Value.Restrictions ?? Enumerable.Empty<DbUserRestriction>()).Append(restriction).ToArray();
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
                    if (now <= restriction.EndTime || restriction.EndTime == null)
                    {
                        // move restriction end time to current time, ending it immediately
                        restriction.EndTime = now;

                        changed = true;
                    }

                    // future restrictions
                    else if (now < restriction.StartTime)
                    {
                        // move restriction end time to start time, invalidating it
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

        public async Task<OneOf<DbUser, NotFound>> AddSupporterDurationAsync(string id, TimeSpan duration, double spending, CancellationToken cancellationToken = default)
        {
            var entry = await _client.GetEntryAsync<DbUser>(id, cancellationToken);

            do
            {
                if (entry.Value == null)
                    return new NotFound();

                var now  = DateTime.UtcNow;
                var info = entry.Value.SupporterInfo ??= new DbUserSupporterInfo();

                info.StartTime ??= now;

                if (info.EndTime == null || info.EndTime < now)
                    info.EndTime = now;

                info.EndTime       += duration;
                info.TotalDays     =  duration.TotalDays;
                info.TotalSpending =  spending;
            }
            while (!await entry.TryUpdateAsync(cancellationToken));

            return entry.Value;
        }
    }
}