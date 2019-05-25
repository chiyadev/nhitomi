using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace nhitomi.Core
{
    public class nhitomiDbContext : DbContext
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
    }

    public sealed class nhitomiDbContextDesignTimeFactory : IDesignTimeDbContextFactory<nhitomiDbContext>
    {
        public nhitomiDbContext CreateDbContext(string[] args) => new nhitomiDbContext(
            new DbContextOptionsBuilder().UseMySql("Server=localhost;").Options);
    }
}