using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;
using Discord.Commands;

namespace nhitomi
{
    public class MessageFormatter
    {
        static string join(IEnumerable<string> values) => values == null || !values.Any() ? null : string.Join(", ", values);

        const string DateFormat = "dddd, dd MMMM yyyy";

        public Embed EmbedDoujin(IDoujin doujin)
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

        public Embed EmbedHelp(
            IEnumerable<CommandInfo> commands,
            IEnumerable<IDoujinClient> clients
        )
        {
            var builder = new StringBuilder();
            var embed = new EmbedBuilder()
                .WithTitle("**nhitomi**: Help")
                .WithUrl("https://github.com/phosphene47/nhitomi")
                .WithDescription("**nhitomi** is a Discord bot for searching and downloading doujinshi on Discord!");

            foreach (var command in commands)
            {
                builder.Append($"- **n!{command.Name}**");
                if (command.Parameters.Count > 0)
                    builder.Append($" *{string.Join(" ", command.Parameters.Select(p => p.Name))}*");
                builder.Append($": {command.Summary}");
                builder.AppendLine();
            }
            embed.AddField("Commands", builder);
            builder.Clear();

            foreach (var client in clients)
            {
                builder.Append($"- **{client.Name.ToLowerInvariant()}**: {client.Url}");
                builder.AppendLine();
            }
            embed.AddField("Supported sources", builder);
            builder.Clear();

            return embed.Build();
        }
    }
}