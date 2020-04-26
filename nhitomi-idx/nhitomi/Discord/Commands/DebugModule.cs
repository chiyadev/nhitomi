using System.Threading.Tasks;
using Qmmands;

namespace nhitomi.Discord.Commands
{
    /// <summary>
    /// Contains commands useful for debugging purposes.
    /// </summary>
    [Group("debug")]
    public class DebugModule : ModuleBase<nhitomiCommandContext>
    {
        [Command, Name("info")]
        public Task InfoAsync() => Context.SendAsync<DebugInfoMessage>();
    }
}