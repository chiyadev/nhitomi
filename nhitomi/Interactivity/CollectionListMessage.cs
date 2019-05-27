using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using nhitomi.Core;
using nhitomi.Interactivity.Triggers;

namespace nhitomi.Interactivity
{
    public class CollectionListMessage : InteractiveMessage
    {
        readonly Collection[] _collections;

        public CollectionListMessage(Collection[] collections)
        {
            _collections = collections;
        }

        protected override IEnumerable<ReactionTrigger> CreateTriggers()
        {
            yield return new DeleteTrigger();
        }

        protected override async Task<bool> InitializeViewAsync(CancellationToken cancellationToken = default)
        {
            //todo: more info
            var embed = new EmbedBuilder()
                .WithTitle("**nhitomi**: Collections")
                .WithDescription(_collections.Length == 0
                    ? "You have no collections."
                    : $"- {string.Join("\n- ", _collections.Select(c => c.Name))}")
                .WithColor(Color.Teal)
                .WithCurrentTimestamp()
                .Build();

            await SetViewAsync(embed, cancellationToken);

            return true;
        }
    }
}