using System.Threading.Tasks;
using nhitomi.Discord;
using Qmmands;

namespace nhitomi.Commands
{
    /// <summary>
    /// Contains commands useful for debugging purposes.
    /// </summary>
    [Group("debug")]
    public class DebugModule : ModuleBase<nhitomiCommandContext>
    {
        [Command]
        [Description("commands.debug.info.desc")]
        public Task InfoAsync() => Context.SendAsync<DebugInfoMessage>();
    }
}