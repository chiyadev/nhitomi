using System;
using System.Threading;
using System.Threading.Tasks;
using Discord.Net;
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

        public DiscordErrorReporter(IOptions<AppSettings> options,
                                    DiscordService discord,
                                    InteractiveManager interactiveManager,
                                    ILogger<DiscordErrorReporter> logger)
        {
            _settings           = options.Value;
            _discord            = discord;
            _interactiveManager = interactiveManager;
            _logger             = logger;
        }

        public async Task ReportAsync(Exception e,
                                      IDiscordContext context,
                                      bool friendlyReply = true,
                                      CancellationToken cancellationToken = default)
        {
            try
            {
                // handle permission exceptions differently
                if (e is HttpException httpException && httpException.DiscordCode == 50013) // 500013 missing perms
                {
                    await ReportMissingPermissionAsync(context, cancellationToken);
                    return;
                }

                // send error message to the current channel
                if (friendlyReply)
                    await _interactiveManager.SendInteractiveAsync(
                        new ErrorMessage(e),
                        context,
                        cancellationToken);

                // send detailed error message to the guild error channel
                var errorChannel = _discord.GetGuild(_settings.Discord.Guild.GuildId)
                                          ?.GetTextChannel(_settings.Discord.Guild.ErrorChannelId);

                if (errorChannel != null)
                    await _interactiveManager.SendInteractiveAsync(
                        new ErrorMessage(e, true),
                        new DiscordContextWrapper(context)
                        {
                            Channel = errorChannel
                        },
                        cancellationToken);
            }
            catch (Exception reportingException)
            {
                // ignore reporting errors
                _logger.LogWarning(reportingException, "Failed to report exception: {0}", e);
            }
        }

        static async Task ReportMissingPermissionAsync(IDiscordContext context,
                                                       CancellationToken cancellationToken = default)
        {
            try
            {
                // tell the user in DM that we don't have perms
                await context.ReplyDmAsync("errorMessage.missingPerms");
            }
            catch
            {
                // the user has DM disabled
                // we can only hope they figure out the permissions by themselves
            }
        }
    }
}