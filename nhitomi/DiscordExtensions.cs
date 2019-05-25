// Copyright (c) 2018-2019 chiya.dev
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System.Threading.Tasks;
using Discord;

namespace nhitomi
{
    public static class DiscordExtensions
    {
        public static EmbedBuilder AddFieldSafe(this EmbedBuilder builder,
            string name,
            string value,
            bool inline = false) =>
            string.IsNullOrWhiteSpace(value)
                ? builder
                : builder.AddField(name, value, inline);

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