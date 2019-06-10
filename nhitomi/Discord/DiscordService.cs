using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using nhitomi.Core;
using nhitomi.Globalization;

namespace nhitomi.Discord
{
    public interface IDiscordContext
    {
        IDiscordClient Client { get; }
        IUserMessage Message { get; }
        IMessageChannel Channel { get; }

        IUser User { get; }
        Guild GuildSettings { get; }
    }

    public class DiscordService : DiscordSocketClient
    {
        readonly AppSettings _settings;

        public DiscordService(IOptions<AppSettings> options) : base(options.Value.Discord)
        {
            _settings = options.Value;

            Ready += () =>
            {
                while (_readyQueue.TryDequeue(out var source))
                    source.TrySetResult(null);

                return Task.CompletedTask;
            };
        }

        readonly ConcurrentQueue<TaskCompletionSource<object>> _readyQueue =
            new ConcurrentQueue<TaskCompletionSource<object>>();

        public async Task ConnectAsync()
        {
            if (LoginState != LoginState.LoggedOut || _settings.Discord.Token == null)
                return;

            // login
            await LoginAsync(TokenType.Bot, _settings.Discord.Token);
            await StartAsync();
        }

        public async Task WaitForReadyAsync(CancellationToken cancellationToken = default)
        {
            var source = new TaskCompletionSource<object>();

            _readyQueue.Enqueue(source);

            using (cancellationToken.Register(() => source.TrySetCanceled()))
                await source.Task;
        }
    }

    public static class DiscordContextExtensions
    {
        public static Localization GetLocalization(this IDiscordContext context) =>
            Localization.GetLocalization(context.GuildSettings?.Language);

        public static IDisposable BeginTyping(this IDiscordContext context) => context.Channel.EnterTypingState();

        public static Task ReplyAsync(this IDiscordContext context, IMessageChannel channel, string localizationKey,
            object variables = null) =>
            channel.SendMessageAsync(new LocalizationPath(localizationKey)[context.GetLocalization(), variables]);

        public static Task ReplyAsync(this IDiscordContext context, string localizationKey, object variables = null) =>
            context.ReplyAsync(context.Channel, localizationKey, variables);

        public static async Task ReplyDmAsync(this IDiscordContext context, string localizationKey,
            object variables = null) =>
            await context.ReplyAsync(await context.User.GetOrCreateDMChannelAsync(), localizationKey, variables);
    }
}