using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace nhitomi.Core
{
    public interface IDatabase
    {
        IQueryable<TEntity> Query<TEntity>(bool readOnly = true) where TEntity : class;

        Task<bool> SaveAsync(CancellationToken cancellationToken = default);
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

        public nhitomiDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            Doujin.Describe(modelBuilder);
        }

        IQueryable<TEntity> IDatabase.Query<TEntity>(bool readOnly)
        {
            IQueryable<TEntity> queryable = Set<TEntity>();

            if (readOnly)
                queryable = queryable.AsNoTracking();

            return queryable;
        }

        async Task<bool> IDatabase.SaveAsync(CancellationToken cancellationToken)
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
    }

    public sealed class nhitomiDbContextDesignTimeFactory : IDesignTimeDbContextFactory<nhitomiDbContext>
    {
        public nhitomiDbContext CreateDbContext(string[] args) => new nhitomiDbContext(
            new DbContextOptionsBuilder().UseMySql("Server=localhost;").Options);
    }
}