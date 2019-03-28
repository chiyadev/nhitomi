// Copyright (c) 2018-2019 fate/loli
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;
using Discord.Commands;
using nhitomi.Core;

namespace nhitomi
{
    public static class MessageFormatter
    {
        static string join(IEnumerable<string> values) =>
            values != null && values.Any() ? string.Join(", ", values) : null;

        const string _dateFormat = "dddd, dd MMMM yyyy";

        public static Embed EmbedDoujin(IDoujin doujin)
        {
            var embed = new EmbedBuilder()
                .WithTitle(doujin.PrettyName ?? "Untitled")
                .WithDescription(
                    doujin.PrettyName != doujin.OriginalName
                        ? doujin.OriginalName
                        : null
                )
                .WithAuthor(a => a
                    .WithName(join(doujin.Artists) ?? doujin.Source.Name)
                    .WithIconUrl(doujin.Source.IconUrl)
                )
                .WithUrl(doujin.SourceUrl)
                .WithImageUrl(doujin.Pages.First().Url)
                .WithColor(Color.Green)
                .WithFooter($"Uploaded on {doujin.UploadTime.ToString(_dateFormat)}");

            if (doujin.Language != null)
                embed.AddFieldString("Language", doujin.Language, true);
            if (doujin.ParodyOf != null)
                embed.AddFieldString("Parody of", doujin.ParodyOf, true);
            if (doujin.Categories != null)
                embed.AddFieldString("Categories", join(doujin.Categories), true);
            if (doujin.Characters != null)
                embed.AddFieldString("Characters", join(doujin.Characters), true);
            if (doujin.Tags != null)
                embed.AddFieldString("Tags", join(doujin.Tags), true);

            embed.AddField("Content", $"{doujin.PageCount} pages", true);

            return embed.Build();
        }

        public static Embed EmbedHelp(
            string prefix,
            IEnumerable<CommandInfo> commands,
            IEnumerable<IDoujinClient> clients,
            string guildInvite)
        {
            var embed = new EmbedBuilder()
                .WithTitle("**nhitomi**: Help")
                .WithDescription(
                    "nhitomi — a Discord bot for searching and downloading doujinshi.\n" +
                    $"Join our server: {guildInvite}")
                .WithColor(Color.Purple)
                .WithCurrentTimestamp();

            // Commands
            var builder = new StringBuilder();

            foreach (var command in commands)
            {
                builder.Append($"- **{prefix}{command.Name}**");

                if (command.Parameters.Count > 0)
                    builder.Append($" __{string.Join("__ __", command.Parameters.Select(p => p.Name))}__");

                builder.AppendLine($" — {command.Summary}");
            }

            embed.AddField("— Commands —", builder);
            builder.Clear();

            // Supported sources
            foreach (var client in clients)
            {
                builder.Append($"- {client.Name.ToLowerInvariant()} — {client.Url}");
                builder.AppendLine();
            }

            embed.AddField("— Sources —", builder);
            builder.Clear();

            return embed.Build();
        }

        public static Embed EmbedError(
            string guildInvite)
        {
            var embed = new EmbedBuilder()
                .WithTitle("**nhitomi**: Error")
                .WithDescription(
                    "Sorry, we encountered an unexpected error and have reported it to the developers! " +
                    $"Please join our official server for further assistance: {guildInvite}")
                .WithColor(Color.Red)
                .WithCurrentTimestamp();

            return embed.Build();
        }

        public static Embed EmbedDownload(
            string doujinName,
            string link)
        {
            var embed = new EmbedBuilder()
                .WithTitle($"**nhitomi**: {doujinName}")
                .WithUrl(link)
                .WithDescription($"Click the link above to start downloading `{doujinName}`.\n")
                .WithColor(Color.LightOrange)
                .WithCurrentTimestamp();

            return embed.Build();
        }
    }
}
