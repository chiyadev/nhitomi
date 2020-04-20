using Nest;
using nhitomi.Models.Queries;

namespace nhitomi.Database
{
    public class DbUserQueryByNameProcessor : IQueryProcessor<DbUser>
    {
        readonly FilterQuery<string> _username;

        public DbUserQueryByNameProcessor(string username)
        {
            _username = username;
        }

        public SearchDescriptor<DbUser> Process(SearchDescriptor<DbUser> descriptor)
            => descriptor.Take(1)
                         .MultiQuery(q => q.Filter(_username, u => u.Username));
    }
}