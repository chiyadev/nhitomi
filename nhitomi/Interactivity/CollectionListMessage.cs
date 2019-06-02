using System.Collections.Generic;
using System.Linq;
using Discord;
using nhitomi.Core;
using nhitomi.Interactivity.Triggers;

namespace nhitomi.Interactivity
{
    public class CollectionListMessage : InteractiveMessage<CollectionListMessage.View>
    {
        readonly Collection[] _collections;

        public CollectionListMessage(Collection[] collections)
        {
            _collections = collections;
        }

        protected override IEnumerable<IReactionTrigger> CreateTriggers()
        {
            yield return new DeleteTrigger();
        }

        public class View : EmbedViewBase
        {
            public new CollectionListMessage Message => (CollectionListMessage) base.Message;

            //todo: more info
            protected override Embed CreateEmbed() => new EmbedBuilder()
                .WithTitle("**nhitomi**: Collections")
                .WithDescription(Message._collections.Length == 0
                    ? "You have no collections."
                    : $"- {string.Join("\n- ", Message._collections.Select(c => c.Name))}")
                .WithColor(Color.Teal)
                .WithCurrentTimestamp()
                .Build();
        }
    }
}