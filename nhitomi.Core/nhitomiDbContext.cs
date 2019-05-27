using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using nhitomi.Core.Clients;

namespace nhitomi.Core
{
    public interface IDatabase
    {
        IQueryable<TEntity> Query<TEntity>(bool readOnly = true) where TEntity : class;

        Task<bool> SaveAsync(CancellationToken cancellationToken = default);

        Task<Doujin> GetDoujinAsync(string source, string id,
            CancellationToken cancellationToken = default);

        Task<Doujin[]> GetDoujinsAsync((string source, string id)[] ids,
            CancellationToken cancellationToken = default);

        IAsyncEnumerable<Doujin> EnumerateDoujinsAsync(Func<IQueryable<Doujin>, IQueryable<Doujin>> query);

        Task<Collection> GetCollectionAsync(ulong userId, string name,
            CancellationToken cancellationToken = default);

        Task<IAsyncEnumerable<Doujin>> EnumerateCollectionAsync(ulong userId, string name,
            CancellationToken cancellationToken = default);
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

        public Task<Doujin> GetDoujinAsync(string source, string id, CancellationToken cancellationToken = default)
        {
            source = ClientRegistry.FixSource(source);
            id = ClientRegistry.FixId(id);

            return IncludeDoujin(Query<Doujin>())
                .Where(d => d.Source == source &&
                            d.SourceId == id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Doujin[]> GetDoujinsAsync((string source, string id)[] ids,
            CancellationToken cancellationToken = default)
        {
            switch (ids.Length)
            {
                case 0:
                    return new Doujin[0];
                case 1:
                    return new[] {await GetDoujinAsync(ids[0].source, ids[0].id, cancellationToken)};
            }

            var source = ids.Select(x => ClientRegistry.FixSource(x.source)).ToArray();
            var id = ids.Select(x => ClientRegistry.FixId(x.id)).ToArray();

            return await IncludeDoujin(Query<Doujin>())
                .Where(d => source.Contains(d.Source) &&
                            id.Contains(d.SourceId))
                // should use async enumerable instead?
                .ToArrayAsync(cancellationToken);
        }

        const int _chunkedLoadSize = 64;

        public IAsyncEnumerable<Doujin> EnumerateDoujinsAsync(Func<IQueryable<Doujin>, IQueryable<Doujin>> query) =>
            IncludeDoujin(query(Query<Doujin>()))
                .ToChunkedAsyncEnumerable(_chunkedLoadSize);

        static IQueryable<Doujin> IncludeDoujin(IQueryable<Doujin> queryable) => queryable
            .Include(d => d.Artist)
            .Include(d => d.Group)
            .Include(d => d.Scanlator)
            .Include(d => d.Language)
            .Include(d => d.ParodyOf)
            .Include(d => d.Characters)
            .Include(d => d.Categories)
            .Include(d => d.Tags)
            .Include(d => d.Pages);

        public Task<Collection> GetCollectionAsync(ulong userId, string name,
            CancellationToken cancellationToken = default)
        {
            name = name.ToLowerInvariant();

            return Query<Collection>()
                // include join table only
                .Include(c => c.Doujins)
                .FirstOrDefaultAsync(c => c.OwnerId == userId && c.Name == name, cancellationToken);
        }

        public async Task<IAsyncEnumerable<Doujin>> EnumerateCollectionAsync(ulong userId, string name,
            CancellationToken cancellationToken = default)
        {
            //todo: use one query to retrieve everything
            var collection = await GetCollectionAsync(userId, name, cancellationToken);

            if (collection == null)
                return null;

            var id = collection.Doujins.Select(x => x.DoujinId).ToArray();

            return IncludeDoujin(Query<Doujin>())
                .Where(d => id.Contains(d.Id))
                .OrderBy(collection.Sort, collection.SortDescending)
                .ToChunkedAsyncEnumerable(_chunkedLoadSize);
        }
    }

    public sealed class nhitomiDbContextDesignTimeFactory : IDesignTimeDbContextFactory<nhitomiDbContext>
    {
        public nhitomiDbContext CreateDbContext(string[] args) => new nhitomiDbContext(
            new DbContextOptionsBuilder().UseMySql("Server=localhost;").Options);
    }
}