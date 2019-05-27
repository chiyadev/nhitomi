using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace nhitomi.Core
{
    public delegate IQueryable<T> QueryFilterDelegate<T>(IQueryable<T> query);

    public interface IDatabase
    {
        IQueryable<TEntity> Query<TEntity>(bool readOnly = true) where TEntity : class;

        Task<bool> SaveAsync(CancellationToken cancellationToken = default);

        Task<Doujin> GetDoujinAsync(string source, string id, CancellationToken cancellationToken = default);
        IAsyncEnumerable<Doujin> GetDoujinsAsync((string source, string id)[] ids);
        IAsyncEnumerable<Doujin> EnumerateDoujinsAsync(QueryFilterDelegate<Doujin> query);

        Task<Collection> GetCollectionAsync(ulong userId, string name, CancellationToken cancellationToken = default);
        Task<Collection[]> GetCollectionsAsync(ulong userId, CancellationToken cancellationToken = default);

        Task<IAsyncEnumerable<Doujin>> EnumerateCollectionAsync(ulong userId, string name,
            QueryFilterDelegate<Doujin> query, CancellationToken cancellationToken = default);
    }

    public class nhitomiDbContext : DbContext, IDatabase
    {
        public DbSet<Doujin> Doujins { get; set; }
        public DbSet<Scanlator> Scanlators { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<ParodyOf> Parodies { get; set; }
        public DbSet<Character> Characters { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Artist> Artists { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Page> Pages { get; set; }
        public DbSet<Collection> Collections { get; set; }
        public DbSet<User> Users { get; set; }

        public nhitomiDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            Doujin.Describe(modelBuilder);
            Collection.Describe(modelBuilder);
            User.Describe(modelBuilder);
        }

        public IQueryable<TEntity> Query<TEntity>(bool readOnly)
            where TEntity : class
        {
            IQueryable<TEntity> queryable = Set<TEntity>();

            if (readOnly)
                queryable = queryable.AsNoTracking();

            return queryable;
        }

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
                return false;
            }
        }

        const int _chunkLoadSize = 64;

        public Task<Doujin> GetDoujinAsync(string source, string id, CancellationToken cancellationToken = default) =>
            Query<Doujin>()
                .Where(d => d.Source == source &&
                            d.SourceId == id)
                .IncludeRelated()
                .FirstOrDefaultAsync(cancellationToken);

        public IAsyncEnumerable<Doujin> GetDoujinsAsync((string source, string id)[] ids)
        {
            switch (ids.Length)
            {
                case 0:
                    return AsyncEnumerable.Empty<Doujin>();
            }

            var source = ids.Select(x => x.source);
            var id = ids.Select(x => x.id);

            return EnumerateDoujinsAsync(x => x
                .Where(d => source.Contains(d.Source) &&
                            id.Contains(d.SourceId)));
        }

        public IAsyncEnumerable<Doujin> EnumerateDoujinsAsync(QueryFilterDelegate<Doujin> query) =>
            query(Query<Doujin>())
                .IncludeRelated()
                .ToChunkedAsyncEnumerable(_chunkLoadSize);

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

        public async Task<IAsyncEnumerable<Doujin>> EnumerateCollectionAsync(ulong userId, string name,
            QueryFilterDelegate<Doujin> query, CancellationToken cancellationToken = default)
        {
            //todo: use one query to retrieve everything
            var collection = await GetCollectionAsync(userId, name, cancellationToken);

            if (collection == null)
                return null;

            var id = collection.Doujins.Select(x => x.DoujinId).ToArray();

            return query(Query<Doujin>())
                .Where(d => id.Contains(d.Id))
                .OrderBy(collection.Sort, collection.SortDescending)
                .IncludeRelated()
                .ToChunkedAsyncEnumerable(_chunkLoadSize);
        }
    }

    public sealed class nhitomiDbContextDesignTimeFactory : IDesignTimeDbContextFactory<nhitomiDbContext>
    {
        public nhitomiDbContext CreateDbContext(string[] args) => new nhitomiDbContext(
            new DbContextOptionsBuilder().UseMySql("Server=localhost;").Options);
    }
}