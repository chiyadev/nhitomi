using Discord;
using Discord.Commands;

namespace nhitomi.Discord
{
    public sealed class DiscordContext : ICommandContext
    {
        public IDiscordClient Client { get; }

        public IGuild Guild => Channel is IGuildChannel c ? c.Guild : null;
        public IMessageChannel Channel { get; }
        public IUser User { get; }
        public IUserMessage Message { get; }

        public DiscordContext(DiscordService discord, MessageContext context)
        {
            Client = discord.Socket;
            Message = context.Message;
            Channel = context.Message.Channel;
            User = context.Message.Author;
        }

        public DiscordContext(DiscordService discord, ReactionContext context)
        {
            Client = discord.Socket;
            Channel = context.Message.Channel;
            User = context.User;
        }
    }
}