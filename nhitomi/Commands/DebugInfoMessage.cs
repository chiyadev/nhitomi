using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using nhitomi.Discord;
using IDiscordClient = nhitomi.Discord.IDiscordClient;

namespace nhitomi.Commands
{
    public sealed class DebugInfoMessage : ReplyMessage
    {
        readonly nhitomiCommandContext _context;
        readonly IDiscordClient _discord;
        readonly IDiscordMessageHandler _messageHandler;
        readonly IDiscordReactionHandler _reactionHandler;
        readonly MemoryInfo _memory;
        readonly IInteractiveManager _interactive;

        public DebugInfoMessage(nhitomiCommandContext context, IDiscordClient discord, IDiscordMessageHandler messageHandler, IDiscordReactionHandler reactionHandler, MemoryInfo memory, IInteractiveManager interactive)
        {
            _context         = context;
            _discord         = discord;
            _messageHandler  = messageHandler;
            _reactionHandler = reactionHandler;
            _memory          = memory;
            _interactive     = interactive;
        }

        const long _mebibytes = 1024 * 1024;

        protected override Task<ReplyContent> RenderAsync(CancellationToken cancellationToken = default) => Task.FromResult(new ReplyContent
        {
            Embed = new EmbedBuilder
            {
                Title = "Debug info",
                Fields =
                {
                    new EmbedFieldBuilder
                    {
                        Name = "Discord",
                        Value = $@"
Shards: {_discord.Shards.Count} shards (id: {_discord.GetShardFor(_context.Guild).ShardId})
Guilds: {_discord.Guilds.Count} guilds
Channels: {_discord.Guilds.Sum(g => g.TextChannels.Count) + _discord.PrivateChannels.Count} channels
Users: {_discord.Guilds.Sum(g => g.MemberCount)} users
Latency: {_discord.Latency}ms
Handled messages: {_messageHandler.Handled} messages ({_messageHandler.Total} total)
Handled reactions: {_reactionHandler.Handled} reactions ({_reactionHandler.Total} total)
Interactive messages: {_interactive.Count} messages
".Trim()
                    },
                    new EmbedFieldBuilder
                    {
                        Name = "Process",
                        Value = $@"
Virtual memory: {_memory.Virtual / _mebibytes}MiB
Working set memory: {_memory.WorkingSet / _mebibytes}MiB
Managed memory: {_memory.Managed / _mebibytes}MiB
".Trim()
                    },
                    new EmbedFieldBuilder
                    {
                        Name = "Runtime",
                        Value = $@"
{RuntimeInformation.OSDescription} {RuntimeInformation.OSArchitecture}
{RuntimeInformation.FrameworkDescription}
".Trim()
                    }
                }
            }
        });
    }
}