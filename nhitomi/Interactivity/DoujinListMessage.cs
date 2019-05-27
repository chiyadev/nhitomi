using System;
using System.Collections.Generic;
using Discord;
using nhitomi.Core;
using nhitomi.Interactivity.Triggers;

namespace nhitomi.Interactivity
{
    public class DoujinListMessage : ListInteractiveMessage<Doujin>
    {
        public DoujinListMessage(IAsyncEnumerable<Doujin> enumerable) : base(enumerable)
        {
        }

        protected override IEnumerable<ReactionTrigger> CreateTriggers()
        {
            yield return new FavoriteTrigger();
            yield return new DownloadTrigger();

            foreach (var trigger in base.CreateTriggers())
                yield return trigger;

            yield return new DeleteTrigger();
        }

        protected override Embed CreateEmbed(IServiceProvider services, Doujin value) =>
            DoujinMessage.CreateEmbed(value);

        protected override Embed CreateEmptyEmbed(IServiceProvider services) => throw new NotImplementedException();
    }
}