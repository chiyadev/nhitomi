using System.Threading;
using System.Threading.Tasks;
using Discord;

namespace nhitomi.Interactivity.Triggers
{
    public class DeleteTrigger : ReactionTrigger<DeleteTrigger.Action>
    {
        public override string Name => "Delete";
        public override IEmote Emote => new Emoji("\uD83D\uDDD1");
        public override bool CanRunStateless => true;

        public class Action : ActionBase
        {
            readonly InteractiveManager _interactive;

            public Action(InteractiveManager interactive)
            {
                _interactive = interactive;
            }

            public override async Task<bool> RunAsync(CancellationToken cancellationToken = default)
            {
                try
                {
                    if (Interactive != null && Interactive.Source?.Id != Context.User.Id)
                        return false;

                    // remove from interactive list
                    _interactive.InteractiveMessages.TryRemove(Context.Message.Id, out _);

                    // dispose interactive object
                    Interactive?.Dispose();

                    // delete message
                    await Context.Message.DeleteAsync();

                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}