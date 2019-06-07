using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using nhitomi.Core;

namespace nhitomi.Interactivity
{
    public sealed class DoujinListFromQueryMessage : DoujinListMessage<DoujinListFromQueryMessage.View>
    {
        readonly string _query;
        readonly bool _qualityFilter;

        public DoujinListFromQueryMessage(string query, bool qualityFilter = true)
        {
            _query = query;
            _qualityFilter = qualityFilter;
        }

        public class View : DoujinListView
        {
            new DoujinListFromQueryMessage Message => (DoujinListFromQueryMessage) base.Message;

            readonly IDatabase _db;

            public View(IDatabase db)
            {
                _db = db;
            }

            protected override Task<Doujin[]> GetValuesAsync(int offset,
                CancellationToken cancellationToken = default) =>
                _db.GetDoujinsAsync(x => x
                    .FullTextSearch(_db, new DoujinSearchArguments
                    {
                        Query = Message._query,
                        QualityFilter = Message._qualityFilter
                    })
                    .Skip(offset)
                    .Take(10));
        }
    }
}