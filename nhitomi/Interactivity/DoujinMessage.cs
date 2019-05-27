using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using nhitomi.Core;
using nhitomi.Interactivity.Triggers;

namespace nhitomi.Interactivity
{
    public interface IDoujinInteractive
    {
        Doujin Doujin { get; }
    }

    public class DoujinMessage : InteractiveMessage, IDoujinInteractive
    {
        public Doujin Doujin { get; }

        public DoujinMessage(Doujin doujin)
        {
            Doujin = doujin;
        }

        protected override IEnumerable<ReactionTrigger> CreateTriggers()
        {
            yield return new FavoriteTrigger();
            yield return new DownloadTrigger();
            yield return new DeleteTrigger();
        }

        static string Join(IEnumerable<string> values)
        {
            var array = values?.ToArray();

            return array == null || array.Length == 0
                ? null
                : string.Join(", ", array);
        }

        public static Embed CreateEmbed(Doujin doujin)
        {
            var embed = new EmbedBuilder()
                .WithTitle(doujin.OriginalName ?? doujin.PrettyName)
                .WithDescription(doujin.OriginalName == doujin.PrettyName ? null : doujin.PrettyName)
                .WithAuthor(a => a
                    .WithName(Join(doujin.Artists?.Select(x => x.Value)) ?? doujin.Source)
                    .WithIconUrl(doujin.Source.IconUrl))
                .WithUrl(doujin.GalleryUrl)
                .WithImageUrl(doujin.Pages.First().Url)
                .WithColor(Color.Green)
                .WithFooter($"{doujin.Source}/{doujin.SourceId}");

            AddField(embed, "Language", doujin.Language?.Value, true);
            AddField(embed, "Parody of", doujin.ParodyOf?.Value, true);
            AddField(embed, "Categories", Join(doujin.Categories?.Select(x => x.Value)), true);
            AddField(embed, "Characters", Join(doujin.Characters?.Select(x => x.Value)), true);
            AddField(embed, "Tags", Join(doujin.Tags?.Select(x => x.Value)), true);
            AddField(embed, "Content", $"{doujin.Pages.Count} pages", true);

            return embed.Build();
        }

        static void AddField(EmbedBuilder builder, string name, string value, bool inline = false)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            builder.AddField(name, value, inline);
        }

        protected override Task UpdateViewAsync(CancellationToken cancellationToken = default) =>
            SetViewAsync(CreateEmbed(Doujin), cancellationToken);

        public static bool TryParseDoujinIdFromMessage(IMessage message, out (string source, string id) id)
        {
            var identifier = message.Embeds.FirstOrDefault(e => e is Embed)?.Footer?.Text;

            if (identifier == null)
            {
                id = (null, null);
                return false;
            }

            // source/id
            var parts = identifier.Split('/', 2);

            id = (parts[0], parts[1]);
            return true;
        }
    }
}