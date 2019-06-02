using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using nhitomi.Globalization;

namespace nhitomi.Discord
{
    public interface IDiscordContext
    {
        IDiscordClient Client { get; }
        IUserMessage Message { get; }
        IMessageChannel Channel { get; }

        IUser User { get; }
        Localization Localization { get; }
    }

    public class DiscordService : DiscordSocketClient
    {
        readonly AppSettings _settings;

        public DiscordService(IOptions<AppSettings> options) : base(options.Value.Discord)
        {
            _settings = options.Value;
        }

        public async Task ConnectAsync()
        {
            if (LoginState != LoginState.LoggedOut || _settings.Discord.Token == null)
                return;

            // login
            await LoginAsync(TokenType.Bot, _settings.Discord.Token);
            await StartAsync();
        }
    }

    public static class DiscordContextExtensions
    {
        public static IDisposable BeginTyping(this IDiscordContext context) => context.Channel.EnterTypingState();

        public static Task ReplyAsync(this IDiscordContext context, IMessageChannel channel, string localizationKey,
            object variables = null) =>
            channel.SendMessageAsync(new LocalizationPath(localizationKey)[context.Localization, variables]);

        public static Task ReplyAsync(this IDiscordContext context, string localizationKey, object variables = null) =>
            context.ReplyAsync(context.Channel, localizationKey, variables);

        public static async Task ReplyDmAsync(this IDiscordContext context, string localizationKey,
            object variables = null) =>
            await context.ReplyAsync(await context.User.GetOrCreateDMChannelAsync(), localizationKey, variables);
    }
}