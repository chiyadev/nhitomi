using System;
using System.Collections.Generic;
using Discord;
using nhitomi.Core;
using nhitomi.Discord;
using nhitomi.Interactivity.Triggers;

namespace nhitomi.Interactivity
{
    public abstract class DoujinListMessage<TView> : ListMessage<TView, Doujin>, IDoujinMessage
        where TView : DoujinListMessage<TView>.DoujinListView
    {
        public Doujin Doujin { get; private set; }

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
            new DoujinListMessage<TView> Message => (DoujinListMessage<TView>) base.Message;

            protected override Embed CreateEmbed(Doujin value)
            {
                Message.Doujin = value;

                return DoujinMessage.View.CreateEmbed(value, Context.GetLocalization());
            }

            protected override Embed CreateEmptyEmbed() => throw new NotImplementedException();
        }
    }
}