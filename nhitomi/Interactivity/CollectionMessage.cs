using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using nhitomi.Core;

namespace nhitomi.Interactivity
{
    public sealed class CollectionMessage : DoujinListMessage<CollectionMessage.View>
    {
        readonly ulong _userId;
        readonly string _collectionName;

        public CollectionMessage(ulong userId,
                                 string collectionName)
        {
            _userId         = userId;
            _collectionName = collectionName;
        }

        public class View : DoujinListView
        {
            new CollectionMessage Message => (CollectionMessage) base.Message;

            readonly IDatabase _db;

            public View(IDatabase db)
            {
                _db = db;
            }

            protected override Task<Doujin[]> GetValuesAsync(int offset,
                                                             CancellationToken cancellationToken = default) =>
                _db.GetCollectionAsync(
                    Message._userId,
                    Message._collectionName,
                    d => d.Skip(offset).Take(10),
                    cancellationToken);

            protected override string ListBeginningMessage => "collectionMessage.listBeginning";
            protected override string ListEndMessage => "collectionMessage.listEnd";
        }
    }
}