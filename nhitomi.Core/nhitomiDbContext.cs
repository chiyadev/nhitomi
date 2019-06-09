using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Design;

namespace nhitomi.Core
{
    public class DoujinSearchArgs
    {
        public string Query { get; set; }
        public int ScanOffset { get; set; }
        public int ScanRange { get; set; } = 128;
        public bool QualityFilter { get; set; } = true;
        public string Source { get; set; }
        public bool? Exists { get; set; }
        public int? MaxCount { get; set; }

        public DoujinSearchArgs Next()
        {
            ScanOffset += ScanRange;

            return this;
        }
    }

    public delegate IQueryable<T> QueryFilterDelegate<T>(IQueryable<T> query);

    public interface IDatabase
    {
        EntityEntry<TEntity> Add<TEntity>(TEntity entity) where TEntity : class;
        EntityEntry<TEntity> Remove<TEntity>(TEntity entity) where TEntity : class;

        IQueryable<TEntity> Query<TEntity>() where TEntity : class;

        Task<bool> SaveAsync(CancellationToken cancellationToken = default);

        Task<Doujin> GetDoujinAsync(string source, string id, CancellationToken cancellationToken = default);
        Task<Doujin[]> GetDoujinsAsync((string source, string id)[] ids, CancellationToken cancellationToken = default);

        Task<Doujin[]> GetDoujinsAsync(QueryFilterDelegate<Doujin> query,
            CancellationToken cancellationToken = default);

        Task<Doujin[]> SearchDoujinsAsync(DoujinSearchArgs args, CancellationToken cancellationToken = default);

        Task<Tag[]> GetTagsAsync(string value, CancellationToken cancellationToken = default);

        Task<Collection> GetCollectionAsync(ulong userId, string name, CancellationToken cancellationToken = default);
        Task<Collection[]> GetCollectionsAsync(ulong userId, CancellationToken cancellationToken = default);

        Task<Doujin[]> GetCollectionAsync(ulong userId, string name, QueryFilterDelegate<Doujin> query,
            CancellationToken cancellationToken = default);

        Task<Guild> GetGuildAsync(ulong guildId, CancellationToken cancellationToken = default);
        Task<Guild[]> GetGuildsAsync(ulong[] guildIds, CancellationToken cancellationToken = default);

        Task<FeedChannel> GetFeedChannelAsync(ulong guildId, ulong channelId,
            CancellationToken cancellationToken = default);

        Task<FeedChannel[]> GetFeedChannelsAsync(CancellationToken cancellationToken = default);
    }

    public class nhitomiDbContext : DbContext, IDatabase
    {
        public DbSet<Doujin> Doujins { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Collection> Collections { get; set; }
        public DbSet<Guild> Guilds { get; set; }
        public DbSet<FeedChannel> FeedChannels { get; set; }

        public nhitomiDbContext(DbContextOptions<nhitomiDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            Doujin.Describe(modelBuilder);
            Collection.Describe(modelBuilder);
            Tag.Describe(modelBuilder);
            Guild.Describe(modelBuilder);
            FeedChannel.Describe(modelBuilder);
        }

        public new IQueryable<TEntity> Query<TEntity>() where TEntity : class => Set<TEntity>();

        public async Task<bool> SaveAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await SaveChangesAsync(cancellationToken);

                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                // optimistic concurrency handling
                // forget stale data by detaching all entities
                foreach (var entry in ChangeTracker.Entries())
                {
                    if (entry.Entity != null)
                        entry.State = EntityState.Detached;
                }

                return false;
            }
        }

        public Task<Doujin> GetDoujinAsync(string source, string id,
            CancellationToken cancellationToken = default) =>
            Query<Doujin>()
                .Where(d => d.Source == source &&
                            d.SourceId == id)
                .Include(d => d.Tags).ThenInclude(x => x.Tag)
                .FirstOrDefaultAsync(cancellationToken);

        public Task<Doujin[]> GetDoujinsAsync((string source, string id)[] ids,
            CancellationToken cancellationToken = default)
        {
            switch (ids.Length)
            {
                case 0:
                    return Task.FromResult(new Doujin[0]);
            }

            var source = ids.Select(x => x.source);
            var id = ids.Select(x => x.id);

            return Query<Doujin>()
                .Where(d => source.Contains(d.Source) &&
                            id.Contains(d.SourceId))
                .Include(d => d.Tags).ThenInclude(x => x.Tag)
                .ToArrayAsync(cancellationToken);
        }

        public async Task<Doujin[]> GetDoujinsAsync(QueryFilterDelegate<Doujin> query,
            CancellationToken cancellationToken = default)
        {
            var doujins = await query(Query<Doujin>())
                .ToArrayAsync(cancellationToken);

            await PopulateTags(doujins, cancellationToken);

            return doujins;
        }

        public async Task<Doujin[]> SearchDoujinsAsync(DoujinSearchArgs args,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(args.Query))
                return new Doujin[0];

            Doujin[] doujins;

            switch (Database.ProviderName)
            {
                case "Pomelo.EntityFrameworkCore.MySql":
                    doujins = await MySqlSearchAsync(args, cancellationToken);
                    break;

                //todo:
                case "Microsoft.EntityFrameworkCore.Sqlite":
                    doujins = new Doujin[0];
                    break;

                default:
                    throw new NotSupportedException($"Unsupported database provider {Database.ProviderName}.");
                    break;
            }

            // populate tags
            await PopulateTags(doujins, cancellationToken);

            return doujins;
        }

        static readonly Regex _commonSymbols = new Regex(@"[-!$%^&*#@()_+|~=`{}\[\]:"";'<>?,.\\\/\s]",
            RegexOptions.Singleline | RegexOptions.Compiled);

        static readonly string[] _mySqlStopwords =
        {
            // MySQL 5.7 InnoDB stopwords
            "about", "are", "com", "for", "from", "how", "that", "the", "this", "was", "what", "when", "where", "who",
            "will", "with", "und", "the", "www"
        };

        void FixMySqlQueryParts(HashSet<string> set)
        {
            // remove anything shorter than 3 characters (InnoDB)
            set.RemoveWhere(s => s.Length < 3);

            // stopwords can kill search results because they are not indexed
            set.RemoveWhere(s => Array.IndexOf(_mySqlStopwords, s) != -1);
        }

        async Task<Doujin[]> MySqlSearchAsync(DoujinSearchArgs args, CancellationToken cancellationToken = default)
        {
            // remove symbols
            args.Query = _commonSymbols.Replace(args.Query, " ");

            // rebuild query for boolean search
            var queryParts = new HashSet<string>(args.Query
                .Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.ToLowerInvariant()));

            if (args.QualityFilter)
            {
                // search quality filter
                queryParts.Add("full");
                queryParts.Add("color");
            }

            FixMySqlQueryParts(queryParts);

            // every part of the query must be present as a tag
            args.Query = "+" + string.Join(" +", queryParts);

            Doujin[] doujins;

            // check if there is at least one matching item
            if (args.Exists == null)
            {
                doujins = await Query<Doujin>()
                    .FromSql(@"
SELECT *
FROM `Doujins`
WHERE MATCH `TagsDenormalized` AGAINST ({0} IN BOOLEAN MODE)
LIMIT 1", args.Query)
                    .ToArrayAsync(cancellationToken);

                args.Exists = doujins.Length != 0;
            }

            if (!args.Exists.Value)
                return new Doujin[0];

            // count the number of rows we can scan
            int maxCount;

            if (args.MaxCount == null)
                args.MaxCount = maxCount = await Query<Doujin>().CountAsync(cancellationToken);
            else
                maxCount = args.MaxCount.Value;

            var doujinList = new List<Doujin>();

            // iterate in chunks
            do
            {
                // get all matching items within the scanning range
                var builder = new StringBuilder().AppendLine($@"
SELECT d.*

FROM (
  # Sort then limit on the primary key
  SELECT `Id`
  FROM `Doujins`");

                if (args.Source != null)
                    builder.AppendLine(@"
  # Limiting to source
  WHERE `Source` = {1}");

                builder.AppendLine($@"
  # Return sorted by upload time descending
  ORDER BY `UploadTime` DESC

  LIMIT {args.ScanRange} OFFSET {args.ScanOffset}
) AS x

# Join on doujins
JOIN `Doujins` d ON d.`Id` = x.`Id`

# Filter items
WHERE MATCH d.`TagsDenormalized` AGAINST ({{0}} IN BOOLEAN MODE)

# Sort again
# MySql doesn't support FULLTEXT + BTREE composite index
ORDER BY d.`UploadTime` DESC");

                doujins = await Query<Doujin>()
                    .FromSql(builder.ToString(), args.Query, args.Source)
                    .ToArrayAsync(cancellationToken);

                doujinList.AddRange(doujins);

                // move to the next chunk
                args = args.Next();

                // increase the scan range exponentially
                if (doujins.Length < 2)
                    args.ScanRange *= 4;
                else if (doujins.Length < 4)
                    args.ScanRange *= 3;
                else if (doujins.Length < 8)
                    args.ScanRange *= 2;
            }
            while (doujinList.Count == 0 && args.ScanOffset < maxCount);

            return doujinList.ToArray();
        }

        public Task<Tag[]> GetTagsAsync(string value, CancellationToken cancellationToken = default) =>
            Query<Tag>()
                .Where(t => t.Value == value)
                .ToArrayAsync(cancellationToken);

        // in some queries, do not populate tags using Include.
        // when using Include with fulltext searching, EF Core automatically
        // orders doujins by their ID at the end and relevancy ordering is ignored.
        async Task PopulateTags(IEnumerable<Doujin> doujins, CancellationToken cancellationToken = default)
        {
            var dict = doujins.ToDictionary(d => d.Id);
            var id = dict.Keys;

            var tags = await Query<TagRef>()
                .Where(x => id.Contains(x.DoujinId))
                .Include(x => x.Tag)
                .ToArrayAsync(cancellationToken);

            foreach (var group in tags.GroupBy(x => x.DoujinId))
                dict[group.Key].Tags = group.ToList();
        }

        public Task<Collection> GetCollectionAsync(ulong userId, string name,
            CancellationToken cancellationToken = default) =>
            Query<Collection>()
                .Include(c => c.Doujins) // join table
                .FirstOrDefaultAsync(c => c.OwnerId == userId && c.Name == name, cancellationToken);

        public Task<Collection[]> GetCollectionsAsync(ulong userId, CancellationToken cancellationToken = default) =>
            Query<Collection>()
                .Include(c => c.Doujins) // join table
                .Where(c => c.OwnerId == userId)
                .ToArrayAsync(cancellationToken);

        public async Task<Doujin[]> GetCollectionAsync(ulong userId, string name,
            QueryFilterDelegate<Doujin> query, CancellationToken cancellationToken = default)
        {
            var collection = await GetCollectionAsync(userId, name, cancellationToken);

            if (collection == null)
                return null;

            var id = collection.Doujins.Select(x => x.DoujinId).ToArray();

            var doujins = await query(Query<Doujin>().Where(d => id.Contains(d.Id)))
                .OrderBy(collection.Sort, collection.SortDescending)
                .ToArrayAsync(cancellationToken);

            await PopulateTags(doujins, cancellationToken);

            return doujins;
        }

        public async Task<Guild> GetGuildAsync(ulong guildId, CancellationToken cancellationToken = default)
        {
            var guild = await Query<Guild>()
                .FirstOrDefaultAsync(g => g.Id == guildId, cancellationToken);

            if (guild == null)
            {
                // create entity for this guild
                guild = new Guild
                {
                    Id = guildId
                };

                Add(guild);
            }

            return guild;
        }

        public async Task<Guild[]> GetGuildsAsync(ulong[] guildIds, CancellationToken cancellationToken = default)
        {
            var guilds = await Query<Guild>()
                .Where(g => guildIds.Contains(g.Id))
                .ToListAsync(cancellationToken);

            // create entities for missing guilds
            foreach (var guildId in guildIds.Where(i => guilds.All(g => g.Id != i)))
            {
                var guild = new Guild
                {
                    Id = guildId
                };

                guilds.Add(guild);

                Add(guilds);
            }

            return guilds.ToArray();
        }

        public async Task<FeedChannel> GetFeedChannelAsync(ulong guildId, ulong channelId,
            CancellationToken cancellationToken = default)
        {
            var channel = await Query<FeedChannel>()
                .Include(c => c.Guild)
                .Include(c => c.LastDoujin)
                .Include(c => c.Tags).ThenInclude(x => x.Tag)
                .FirstOrDefaultAsync(c => c.Id == channelId, cancellationToken);

            if (channel == null)
            {
                var lastDoujin = await Query<Doujin>()
                    .OrderByDescending(d => d.ProcessTime)
                    .FirstOrDefaultAsync(cancellationToken);

                // create entity for this channel
                channel = new FeedChannel
                {
                    Id = channelId,
                    Guild = await GetGuildAsync(guildId, cancellationToken),
                    LastDoujin = lastDoujin,
                    Tags = new List<FeedChannelTag>()
                };

                Add(channel);
            }

            return channel;
        }

        public Task<FeedChannel[]> GetFeedChannelsAsync(CancellationToken cancellationToken = default) =>
            Query<FeedChannel>()
                .Include(c => c.Guild)
                .Include(c => c.LastDoujin)
                .Include(c => c.Tags).ThenInclude(x => x.Tag)
                .ToArrayAsync(cancellationToken);
    }

    public sealed class nhitomiDbContextDesignTimeFactory : IDesignTimeDbContextFactory<nhitomiDbContext>
    {
        public nhitomiDbContext CreateDbContext(string[] args) => new nhitomiDbContext(
            new DbContextOptionsBuilder<nhitomiDbContext>().UseMySql("Server=localhost;").Options);
    }
}