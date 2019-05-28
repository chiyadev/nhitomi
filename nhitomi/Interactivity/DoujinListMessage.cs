using System;
using System.Collections.Generic;
using System.Threading;
using Discord;
using nhitomi.Core;
using nhitomi.Globalization;
using nhitomi.Interactivity.Triggers;

namespace nhitomi.Interactivity
{
    public class DoujinListMessage : ListMessage<DoujinListMessage.View, Doujin>
    {
        readonly DoujinEnumerator _enumerator;

        public DoujinListMessage(DoujinEnumerator enumerator)
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

        public delegate IAsyncEnumerable<Doujin> DoujinEnumerator(IDatabase database, int offset);

        protected override IAsyncEnumerable<Doujin> GetValuesAsync(View view, int offset,
            CancellationToken cancellationToken = default) =>
            _enumerator(view.Database, offset);

        public class View : ListViewBase
        {
            public readonly IDatabase Database;

            readonly ILocalization _localization;

            public View(IDatabase database, ILocalization localization)
            {
                Database = database;
                _localization = localization;
            }

            protected override Embed CreateEmbed(Doujin value) =>
                DoujinMessage.View.CreateEmbed(value, _localization[Context]);

            protected override Embed CreateEmptyEmbed() => throw new NotImplementedException();
        }
    }
}