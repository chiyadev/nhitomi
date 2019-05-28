using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using nhitomi.Core;
using nhitomi.Interactivity.Triggers;

namespace nhitomi.Interactivity
{
    public delegate Task<IEnumerable<Doujin>> DoujinListEnumerator(IDatabase database, int offset);

    public class DoujinListMessage : ListMessage<DoujinListMessage.View, Doujin>
    {
        readonly DoujinListEnumerator _enumerator;

        public DoujinListMessage(DoujinListEnumerator enumerator)
        {
            _enumerator = enumerator;
        }

        protected override IEnumerable<IReactionTrigger> CreateTriggers()
        {
            yield return new FavoriteTrigger();
            yield return new DownloadTrigger();

            foreach (var trigger in base.CreateTriggers())
                yield return trigger;

            yield return new DeleteTrigger();
        }

        protected override Task<IEnumerable<Doujin>> GetValuesAsync(View view, int offset,
            CancellationToken cancellationToken = default) =>
            _enumerator(view.Database, offset);

        public class View : ListViewBase
        {
            public readonly IDatabase Database;

            public View(IDatabase database)
            {
                Database = database;
            }

            protected override Embed CreateEmbed(Doujin value) => DoujinMessage.View.CreateEmbed(value);
            protected override Embed CreateEmptyEmbed() => throw new NotImplementedException();
        }
    }
}