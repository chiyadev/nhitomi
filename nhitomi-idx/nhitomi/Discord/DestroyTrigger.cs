using System.Threading;
using System.Threading.Tasks;
using Discord;

namespace nhitomi.Discord
{
    /// <summary>
    /// A trigger that deletes a message and destroys its interactive.
    /// </summary>
    public class DestroyTrigger : ReactionTrigger
    {
        readonly InteractiveMessage _message;

        public override IEmote Emote => new Emoji("\uD83D\uDDD1"); // trashcan

        public DestroyTrigger(InteractiveMessage message)
        {
            _message = message;
        }

        protected override async Task<bool> RunAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _message.Reply.DeleteAsync();
                await _message.DisposeAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}