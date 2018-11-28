using System;
using System.Collections.Generic;
using System.Linq;
using Discord;

namespace nhitomi
{
    public class MessageFormatter
    {
        static string join(IEnumerable<string> values) => values == null || !values.Any() ? null : string.Join(", ", values);

        const string DateFormat = "dddd, dd MMMM yyyy";

        public Embed EmbedDoujin(IDoujin doujin)
        {
            var builder = new EmbedBuilder()
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
                builder.AddInlineField("Language", doujin.Language);
            if (doujin.ParodyOf != null)
                builder.AddInlineField("Parody of", doujin.ParodyOf);
            if (doujin.Categories != null)
                builder.AddInlineField("Categories", join(doujin.Categories));
            if (doujin.Characters != null)
                builder.AddInlineField("Characters", join(doujin.Characters));
            if (doujin.Tags != null)
                builder.AddInlineField("Tags", join(doujin.Tags));

            return builder.Build();
        }


    }
}