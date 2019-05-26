using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.DependencyInjection;

namespace nhitomi.Interactivity.Triggers
{
    public class DeleteTrigger : ReactionTrigger
    {
        public override string Name => "Delete";
        public override IEmote Emote => new Emoji("\uD83D\uDDD1");
        public override bool CanRunStateless => true;

        public override async Task RunAsync(CancellationToken cancellationToken = default)
        {
            // remove from interactive list
            Services.GetRequiredService<InteractiveManager>().InteractiveMessages.TryRemove(Message.Id, out _);

            // dispose interactive object
            Interactive?.Dispose();

            // delete message
            await Message.DeleteAsync();
        }
    }
}