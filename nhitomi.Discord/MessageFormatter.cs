// Copyright (c) 2018 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;
using Discord.Commands;

namespace nhitomi
{
    public static class MessageFormatter
    {
        static string join(IEnumerable<string> values) => values == null || !values.Any() ? null : string.Join(", ", values);

        const string DateFormat = "dddd, dd MMMM yyyy";

        public static Embed EmbedDoujin(IDoujin doujin)
        {
            var embed = new EmbedBuilder()
                .WithTitle(doujin.PrettyName ?? "Untitled")
                .WithDescription(
                    doujin.PrettyName != doujin.OriginalName
                        ? doujin.OriginalName
                        : null
                )
                .WithAuthor(
                    authoer => authoer
                        .WithName(join(doujin.Artists) ?? doujin.Source.Name)
                        .WithIconUrl(doujin.Source.IconUrl)
                )
                .WithUrl(doujin.SourceUrl)
                .WithImageUrl(doujin.PageUrls.First())
                .WithColor(Color.Green)
                .WithFooter($"Uploaded on {doujin.UploadTime.ToString(DateFormat)}");

            if (doujin.Language != null)
                embed.AddInlineField("Language", doujin.Language);
            if (doujin.ParodyOf != null)
                embed.AddInlineField("Parody of", doujin.ParodyOf);
            if (doujin.Categories != null)
                embed.AddInlineField("Categories", join(doujin.Categories));
            if (doujin.Characters != null)
                embed.AddInlineField("Characters", join(doujin.Characters));
            if (doujin.Tags != null)
                embed.AddInlineField("Tags", join(doujin.Tags));

            return embed.Build();
        }

        public static Embed EmbedHelp(
            IEnumerable<CommandInfo> commands,
            IEnumerable<IDoujinClient> clients
        )
        {
            var embed = new EmbedBuilder()
                .WithTitle("**nhitomi**: Help")
                .WithDescription(
                    "nhitomi is a Discord bot for searching and downloading doujinshi on Discord! " +
                    "Join our server: https://discord.gg/JFNga7q"
                )
                .WithColor(Color.Purple)
                .WithCurrentTimestamp();

            // Commands
            var builder = new StringBuilder();

            foreach (var command in commands)
            {
                builder.Append($"- **n!{command.Name}**");

                if (command.Parameters.Count > 0)
                    builder.Append($" __{string.Join("__, __", command.Parameters.Select(p => p.Name))}__");

                builder.Append($" — {command.Summary}");

                if (command.Remarks != null)
                    builder.Append($" e.g. `{command.Remarks}`.");

                builder.AppendLine();
            }
            embed.AddField("— Commands —", builder);
            builder.Clear();

            // Supported sources
            foreach (var client in clients)
            {
                builder.Append($"- **{client.Name.ToLowerInvariant()}** — {client.Url}");
                builder.AppendLine();
            }
            embed.AddField("— Supported sources —", builder);

            // Note
            embed.AddField("— Note —",
                "All content uploaded by this bot are sourced directly from the respective websites listed above. " +
                "These contents may be, but not limited to, offensive, profane, depressing, harassing and/or gory. " +
                "Please leave immediately if you find this type of content distressing.");

            // Contribution
            embed.AddField("— Contribution —",
                "This project is licensed under the MIT License. " +
                "Contributions are welcome! " +
                "https://github.com/phosphene47/nhitomi");

            return embed.Build();
        }

        public static Embed EmbedError()
        {
            var embed = new EmbedBuilder()
                .WithTitle("**nhitomi**: Error")
                .WithDescription(
                    "Sorry, we encountered an unexpected error and has been reported to the developers! " +
                    "Please join our official server for further assistance: https://discord.gg/JFNga7q"
                )
                .WithColor(Color.Red)
                .WithCurrentTimestamp();

            return embed.Build();
        }
    }
}