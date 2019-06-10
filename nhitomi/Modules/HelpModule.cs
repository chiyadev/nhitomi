using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using nhitomi.Discord;
using nhitomi.Discord.Parsing;
using nhitomi.Interactivity;

namespace nhitomi.Modules
{
    [Module("help", IsPrefixed = false)]
    public class HelpModule
    {
        readonly IMessageContext _context;
        readonly InteractiveManager _interactive;

        public HelpModule(IMessageContext context, InteractiveManager interactive)
        {
            _context = context;
            _interactive = interactive;
        }

        [Command("help")]
        public Task HelpAsync(CancellationToken cancellationToken = default) =>
            _interactive.SendInteractiveAsync(new HelpMessage(), _context, cancellationToken);

        [Command("debug", Aliases = null)]
        public Task DebugAsync(CancellationToken cancellationToken = default) =>
            _interactive.SendInteractiveAsync(new DebugMessage(), _context, cancellationToken);

        sealed class DebugMessage : EmbedMessage<DebugMessage.View>
        {
            public class View : ViewBase
            {
                readonly DiscordService _discord;
                readonly MessageHandlerService _messageHandler;
                readonly ReactionHandlerService _reactionHandler;
                readonly InteractiveManager _interactive;

                public View(DiscordService discord, MessageHandlerService messageHandler,
                    ReactionHandlerService reactionHandler, InteractiveManager interactive)
                {
                    _discord = discord;
                    _messageHandler = messageHandler;
                    _reactionHandler = reactionHandler;
                    _interactive = interactive;
                }

                sealed class ProcessMemory
                {
                    public readonly long Virtual;
                    public readonly long WorkingSet;
                    public readonly long Managed;

                    const long _mebibytes = 1024 * 1024;

                    public ProcessMemory()
                    {
                        using (var process = Process.GetCurrentProcess())
                        {
                            Virtual = process.VirtualMemorySize64 / _mebibytes;
                            WorkingSet = process.WorkingSet64 / _mebibytes;
                        }

                        Managed = GC.GetTotalMemory(false) / _mebibytes;
                    }
                }

                public override async Task<bool> UpdateAsync(CancellationToken cancellationToken = default)
                {
                    var memory = new ProcessMemory();

                    var embed = new EmbedBuilder()
                        .WithTitle("**nhitomi**: Debug information")
                        .WithFields(
                            new EmbedFieldBuilder()
                                .WithName("Discord")
                                .WithValue($@"
Guilds: {_discord.Guilds.Count} guilds
Channels: {_discord.Guilds.Sum(g => g.TextChannels.Count) + _discord.PrivateChannels.Count} channels
Latency: {_discord.Latency}ms
Handled messages: {_messageHandler.HandledMessages} messages ({_messageHandler.ReceivedMessages} received)
Handled reactions: {_reactionHandler.HandledReactions} reactions ({_reactionHandler.ReceivedReactions} received)
Interactive messages: {_interactive.InteractiveMessages.Count} messages
Interactive triggers: {_interactive.InteractiveMessages.Sum(m => m.Value.Triggers.Count)} triggers
".Trim()),
                            new EmbedFieldBuilder()
                                .WithName("Process")
                                .WithValue($@"
Virtual memory: {memory.Virtual}MiB
Working set memory: {memory.WorkingSet}MiB
Managed memory: {memory.Managed}MiB
".Trim()),
                            new EmbedFieldBuilder()
                                .WithName("Runtime")
                                .WithValue($@"
{RuntimeInformation.OSDescription} {RuntimeInformation.OSArchitecture}
{RuntimeInformation.FrameworkDescription}
".Trim()))
                        .Build();

                    await SetEmbedAsync(embed, cancellationToken);
                    return true;
                }
            }
        }
    }
}