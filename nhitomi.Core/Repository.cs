using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace nhitomi.Core
{
    public class Repository
    {
        readonly IDatabase _database;

        public Repository(IDatabase database)
        {
            _database = database;
        }

        public Task<Doujin> GetDoujin(string source, string id) =>
            IncludeDoujin(_database.Query<Doujin>())
                .Where(d => d.Source == source && d.SourceId == id)
                .FirstOrDefaultAsync();

        static IQueryable<Doujin> IncludeDoujin(IQueryable<Doujin> queryable) => queryable
            .Include(d => d.Scanlator)
            .Include(d => d.Language)
            .Include(d => d.ParodyOf)
            .Include(d => d.Characters)
            .Include(d => d.Categories)
            .Include(d => d.Artists)
            .Include(d => d.Groups)
            .Include(d => d.Tags)
            .Include(d => d.Pages);
    }
}