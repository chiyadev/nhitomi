using System.Threading;
using System.Threading.Tasks;
using nhitomi.Core;

namespace nhitomi.Interactivity
{
    public sealed class DoujinListFromQueryMessage : DoujinListMessage<DoujinListFromQueryMessage.View>
    {
        readonly DoujinSearchArgs _args;

        public DoujinListFromQueryMessage(DoujinSearchArgs args)
        {
            _args = args;
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
                _db.SearchDoujinsAsync(Message._args, cancellationToken);
        }
    }
}