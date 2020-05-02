using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using nhitomi.Database;

namespace nhitomi.Discord.Commands
{
    /// <summary>
    /// Responsible for selecting the optimal content of a book based on user preferences.
    /// </summary>
    public interface IBookContentSelector
    {
        ValueTask<DbBookContent> SelectAsync(DbBook book, nhitomiCommandContext context, CancellationToken cancellationToken = default);
    }

    public class BookContentSelector : IBookContentSelector
    {
        public ValueTask<DbBookContent> SelectAsync(DbBook book, nhitomiCommandContext context, CancellationToken cancellationToken = default)
        {
            var selected = book.Contents
                                // prefer content of user language
                               .OrderBy(c => c.Language == context.User.Language ? -1 : 1)

                                // then by language binary order
                               .ThenBy(c => (int) c.Language)
                               .First();

            return new ValueTask<DbBookContent>(selected);
        }
    }
}