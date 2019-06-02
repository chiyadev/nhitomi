using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Design;

namespace nhitomi.Core
{
    public delegate IQueryable<T> QueryFilterDelegate<T>(IQueryable<T> query);

    public interface IDatabase
    {
        EntityEntry<TEntity> Add<TEntity>(TEntity entity) where TEntity : class;
        EntityEntry<TEntity> Remove<TEntity>(TEntity entity) where TEntity : class;

        IQueryable<TEntity> Query<TEntity>(bool readOnly = true) where TEntity : class;

        Task<bool> SaveAsync(CancellationToken cancellationToken = default);

        Task<Doujin> GetDoujinAsync(string source, string id, CancellationToken cancellationToken = default);
        Task<Doujin[]> GetDoujinsAsync((string source, string id)[] ids, CancellationToken cancellationToken = default);

        Task<Doujin[]> GetDoujinsAsync(QueryFilterDelegate<Doujin> query,
            CancellationToken cancellationToken = default);

        Task<Collection> GetCollectionAsync(ulong userId, string name, CancellationToken cancellationToken = default);
        Task<Collection[]> GetCollectionsAsync(ulong userId, CancellationToken cancellationToken = default);

        Task<Doujin[]> GetCollectionAsync(ulong userId, string name, QueryFilterDelegate<Doujin> query,
            CancellationToken cancellationToken = default);

        Task<User> GetUserAsync(ulong userId, CancellationToken cancellationToken = default);

        Task<Guild> GetGuildAsync(ulong guildId, CancellationToken cancellationToken = default);
        Task<Guild[]> GetGuildsAsync(ulong[] guildIds, CancellationToken cancellationToken = default);
    }

    public class nhitomiDbContext : DbContext, IDatabase
    {
        public DbSet<Doujin> Doujins { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Collection> Collections { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Guild> Guilds { get; set; }

        public nhitomiDbContext(DbContextOptions<nhitomiDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            Doujin.Describe(modelBuilder);
            Collection.Describe(modelBuilder);
            User.Describe(modelBuilder);
        }

        public IQueryable<TEntity> Query<TEntity>(bool readOnly = true) where TEntity : class => Set<TEntity>();

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

        const int _chunkLoadSize = 1;

        public Task<Doujin> GetDoujinAsync(string source, string id, CancellationToken cancellationToken = default) =>
            Query<Doujin>()
                .Where(d => d.Source == source &&
                            d.SourceId == id)
                .IncludeRelated()
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
                .ToArrayAsync(cancellationToken);
        }

        public Task<Doujin[]> GetDoujinsAsync(QueryFilterDelegate<Doujin> query,
            CancellationToken cancellationToken = default) =>
            query(Query<Doujin>())
                .IncludeRelated()
                .ToArrayAsync(cancellationToken);

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
            //todo: use one query to retrieve everything
            var collection = await GetCollectionAsync(userId, name, cancellationToken);

            if (collection == null)
                return null;

            var id = collection.Doujins.Select(x => x.DoujinId).ToArray();

            return await query(Query<Doujin>())
                .Where(d => id.Contains(d.Id))
                .OrderBy(collection.Sort, collection.SortDescending)
                .IncludeRelated()
                .ToArrayAsync(cancellationToken);
        }

        public async Task<User> GetUserAsync(ulong userId, CancellationToken cancellationToken = default)
        {
            var user = await Query<User>()
                .Include(u => u.Collections)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                // create entity for this user
                user = new User
                {
                    Id = userId
                };

                Add(user);
            }

            return user;
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
    }

    public sealed class nhitomiDbContextDesignTimeFactory : IDesignTimeDbContextFactory<nhitomiDbContext>
    {
        public nhitomiDbContext CreateDbContext(string[] args) => new nhitomiDbContext(
            new DbContextOptionsBuilder<nhitomiDbContext>().UseMySql("Server=localhost;").Options);
    }
}