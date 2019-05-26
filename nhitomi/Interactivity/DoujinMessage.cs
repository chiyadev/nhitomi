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

        protected override async Task UpdateViewAsync(CancellationToken cancellationToken = default)
        {
            var embed = new EmbedBuilder()
                .WithTitle(Doujin.OriginalName ?? Doujin.PrettyName)
                .WithDescription(Doujin.OriginalName == Doujin.PrettyName ? null : Doujin.PrettyName)
                .WithAuthor(a => a
                    .WithName(Join(Doujin.Artists?.Select(x => x.Value)) ?? Doujin.Source)
                    .WithIconUrl(Doujin.Source.IconUrl))
                .WithUrl(Doujin.GalleryUrl)
                .WithImageUrl(Doujin.Pages.First().Url)
                .WithColor(Color.Green)
                .WithFooter($"{Doujin.Source}/{Doujin.SourceId}");

            embed.AddFieldSafe("Language", Doujin.Language?.Value, true);
            embed.AddFieldSafe("Parody of", Doujin.ParodyOf?.Value, true);
            embed.AddFieldSafe("Categories", Join(Doujin.Categories?.Select(x => x.Value)), true);
            embed.AddFieldSafe("Characters", Join(Doujin.Characters?.Select(x => x.Value)), true);
            embed.AddFieldSafe("Tags", Join(Doujin.Tags?.Select(x => x.Value)), true);
            embed.AddFieldSafe("Content", $"{Doujin.Pages.Count} pages", true);

            await SetViewAsync(embed.Build(), cancellationToken);
        }

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