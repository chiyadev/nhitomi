using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using nhitomi.Core;
using nhitomi.Globalization;

namespace nhitomi.Discord
{
    public class GuildWelcomeMessageService : BackgroundService
    {
        readonly AppSettings _settings;
        readonly DiscordService _discord;
        readonly DiscordErrorReporter _errorReporter;

        public GuildWelcomeMessageService(IOptions<AppSettings> options, DiscordService discord,
            DiscordErrorReporter errorReporter)
        {
            _settings = options.Value;
            _discord = discord;
            _errorReporter = errorReporter;

            _discord.JoinedGuild += HandleJoinedGuild;
        }

        async Task HandleJoinedGuild(SocketGuild guild)
        {
            try
            {
                // use default localization
                var l = Localization.Default;
                var path = new LocalizationPath("welcomeMessage");
                var prefix = _settings.Discord.Prefix;

                var content = $@"
{path["text"][l]}

**|** {path["get"][l, new {prefix}]}
**|** {path["download"][l, new {prefix}]}
**|** {path["search"][l, new {prefix}]}
**|** {path["language"][l, new {prefix}]}

{path["referHelp"][l, new {prefix}]}

{path["openSource"][l, new {repoUrl = "https://github.com/chiyadev/nhitomi"}]}
".Trim();

                foreach (var channel in guild.TextChannels.OrderBy(c => c.Position))
                {
                    var perms = guild.CurrentUser.GetPermissions(channel);

                    // first channel where we can send messages
                    if (perms.SendMessages)
                    {
                        await channel.SendMessageAsync(content);
                        return;
                    }
                }

                // no channel to send messages
                // send to owner
                await guild.Owner.SendMessageAsync($@"
{content}
".Trim());
            }
            catch (Exception e)
            {
                await _errorReporter.ReportAsync(e, new GuildJoinedContext
                {
                    Client = _discord,
                    Channel = guild.DefaultChannel,
                    User = guild.Owner
                }, false);
            }
        }

        class GuildJoinedContext : IDiscordContext
        {
            public IDiscordClient Client { get; set; }
            public IUserMessage Message => null;
            public IMessageChannel Channel { get; set; }
            public IUser User { get; set; }
            public Guild GuildSettings => new Guild();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
    }
}