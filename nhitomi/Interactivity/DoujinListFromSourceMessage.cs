using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using nhitomi.Core;

namespace nhitomi.Interactivity
{
    public sealed class DoujinListFromSourceMessage : DoujinListMessage<DoujinListFromSourceMessage.View>
    {
        readonly string _source;

        public DoujinListFromSourceMessage(string source)
        {
            _source = source;
        }

        public class View : DoujinListView
        {
            new DoujinListFromSourceMessage Message => (DoujinListFromSourceMessage) base.Message;

            readonly IDatabase _db;

            public View(IDatabase db)
            {
                _db = db;
            }

            protected override Task<Doujin[]> GetValuesAsync(int offset,
                                                             CancellationToken cancellationToken = default) =>
                _db.GetDoujinsAsync(x =>
                {
                    x = x.Where(d => d.Source == Message._source);

                    // todo: ascending option
                    x = x.OrderByDescending(d => d.UploadTime);

                    return x
                          .Skip(offset)
                          .Take(10);
                });
        }
    }
}