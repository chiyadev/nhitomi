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

        protected override void InitializeView(View view)
        {
            base.InitializeView(view);

            view.Collections = _collections;
        }

        public class View : EmbedViewBase
        {
            public Collection[] Collections;

            //todo: more info
            protected override Embed CreateEmbed() => new EmbedBuilder()
                .WithTitle("**nhitomi**: Collections")
                .WithDescription(Collections.Length == 0
                    ? "You have no collections."
                    : $"- {string.Join("\n- ", Collections.Select(c => c.Name))}")
                .WithColor(Color.Teal)
                .WithCurrentTimestamp()
                .Build();
        }
    }
}