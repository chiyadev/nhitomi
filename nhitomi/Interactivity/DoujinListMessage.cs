using System;
using System.Collections.Generic;
using Discord;
using nhitomi.Core;
using nhitomi.Interactivity.Triggers;

namespace nhitomi.Interactivity
{
    public abstract class DoujinListMessage<TView> : ListMessage<TView, Doujin>
        where TView : DoujinListMessage<TView>.DoujinListView
    {
        protected override IEnumerable<IReactionTrigger> CreateTriggers()
        {
            yield return new FavoriteTrigger();
            yield return new DownloadTrigger();

            foreach (var trigger in base.CreateTriggers())
                yield return trigger;

            yield return new DeleteTrigger();
        }

        public abstract class DoujinListView : ListViewBase
        {
            protected override Embed CreateEmbed(Doujin value) =>
                DoujinMessage.View.CreateEmbed(value, Context.Localization);

            protected override Embed CreateEmptyEmbed() => throw new NotImplementedException();
        }
    }
}