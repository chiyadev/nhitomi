using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nhitomi.Interactivity;

namespace nhitomi.Discord
{
    public class DiscordErrorReporter
    {
        readonly AppSettings _settings;
        readonly DiscordService _discord;
        readonly InteractiveManager _interactiveManager;
        readonly ILogger<DiscordErrorReporter> _logger;

        public DiscordErrorReporter(IOptions<AppSettings> options, DiscordService discord,
            InteractiveManager interactiveManager, ILogger<DiscordErrorReporter> logger)
        {
            _settings = options.Value;
            _discord = discord;
            _interactiveManager = interactiveManager;
            _logger = logger;
        }

        public async Task ReportAsync(Exception e, IDiscordContext context, bool channelReply = true)
        {
            try
            {
                // send error message to the current channel
                if (channelReply)
                {
                    await _interactiveManager.SendInteractiveAsync(
                        new ErrorMessage(e),
                        context);
                }

                // send detailed error message to the guild error channel
                var errorChannel = _discord
                    .GetGuild(_settings.Discord.Guild.GuildId)?
                    .GetTextChannel(_settings.Discord.Guild.ErrorChannelId);

                if (errorChannel != null)
                {
                    await _interactiveManager.SendInteractiveAsync(
                        new ErrorMessage(e, true),
                        new DiscordContextWrapper(context)
                        {
                            Channel = errorChannel
                        });
                }
            }
            catch (Exception e2)
            {
                // ignore reporting errors
                _logger.LogWarning(e2, "Failed to report exception: {0}", e);
            }
        }
    }
}