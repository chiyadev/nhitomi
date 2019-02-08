// Copyright (c) 2018-2019 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System.Threading.Tasks;
using Discord;

namespace nhitomi
{
    public static class DiscordExtensions
    {
        public static EmbedBuilder AddFieldString(this EmbedBuilder builder, string name, string value, bool inline = false)
        {
            if (string.IsNullOrWhiteSpace(value))
                return builder;

            return builder.AddField(name, value, inline);
        }

        public static async Task ModifyAsync(this IUserMessage message, string content = null, Embed embed = null)
        {
            if (string.IsNullOrWhiteSpace(content) && embed == null)
                await message.DeleteAsync();
            else
                await message.ModifyAsync(m =>
                {
                    m.Content = content;
                    m.Embed = embed;
                });
        }
    }
}