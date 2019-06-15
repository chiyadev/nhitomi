using System.Collections.Generic;
using System.Linq;
using Discord;
using nhitomi.Core;
using nhitomi.Core.Clients.Hitomi;
using nhitomi.Core.Clients.nhentai;
using nhitomi.Discord;
using nhitomi.Globalization;
using nhitomi.Interactivity.Triggers;
using TagType = nhitomi.Core.TagType;

namespace nhitomi.Interactivity
{
    public interface IDoujinMessage : IInteractiveMessage
    {
        Doujin Doujin { get; }
    }

    public class DoujinMessage : InteractiveMessage<DoujinMessage.View>, IDoujinMessage
    {
        readonly bool _isFeed;

        public Doujin Doujin { get; }

        public DoujinMessage(Doujin doujin, bool isFeed = false)
        {
            Doujin = doujin;

            _isFeed = isFeed;
        }

        protected override IEnumerable<IReactionTrigger> CreateTriggers()
        {
            yield return new FavoriteTrigger();
            yield return new ReadTrigger();
            yield return new DownloadTrigger();

            if (!_isFeed)
                yield return new DeleteTrigger();
        }

        public class View : EmbedViewBase
        {
            new DoujinMessage Message => (DoujinMessage) base.Message;

            protected override Embed CreateEmbed() =>
                CreateEmbed(Message.Doujin, Context.GetLocalization(), Message._isFeed);

            public static Embed CreateEmbed(Doujin doujin, Localization l, bool isFeed = false)
            {
                var path = new LocalizationPath("doujinMessage");

                var embed = new EmbedBuilder()
                    .WithTitle(path["title"][l, new {doujin}])
                    .WithDescription(doujin.OriginalName == doujin.PrettyName ? null : doujin.PrettyName)
                    .WithAuthor(a => a
                        .WithName(doujin.GetTag(TagType.Artist)?.Value ?? doujin.Source)
                        .WithIconUrl(GetSourceIconUrl(doujin)))
                    .WithUrl(GetGalleryUrl(doujin))
                    .WithImageUrl($"https://nhitomi.chiya.dev/api/v1/images/{doujin.AccessId}/-1")
                    .WithColor(Color.Green)
                    .WithFooter(path["footer"][l, new {doujin}] +
                                (isFeed ? " | feed" : null));

                AddField(embed, path["language"][l], doujin.GetTag(TagType.Language)?.Value);
                AddField(embed, path["group"][l], doujin.GetTag(TagType.Group)?.Value);
                AddField(embed, path["parody"][l], doujin.GetTag(TagType.Parody)?.Value);
                AddField(embed, path["categories"][l], doujin.GetTags(TagType.Category)?.Select(t => t.Value));
                AddField(embed, path["characters"][l], doujin.GetTags(TagType.Character)?.Select(t => t.Value));
                AddField(embed, path["tags"][l], doujin.GetTags(TagType.Tag)?.Select(t => t.Value));
                AddField(embed, path["contents"][l], path["contentsValue"][l, new {doujin}]);

                return embed.Build();
            }

            static void AddField(EmbedBuilder builder, string name, string value, bool inline = false)
            {
                if (string.IsNullOrWhiteSpace(value))
                    return;

                builder.AddField(name.Trim(), value.Trim(), inline);
            }

            static void AddField(EmbedBuilder builder, string name, IEnumerable<string> values, bool inline = true)
            {
                var array = values?.ToArray();

                if (array == null || array.Length == 0)
                    return;

                AddField(builder, name, string.Join(", ", array), inline);
            }

            public static string GetGalleryUrl(Doujin d)
            {
                switch (d.Source.ToLowerInvariant())
                {
                    case "nhentai": return nhentaiClient.GetGalleryUrl(d);
                    case "hitomi": return HitomiClient.GetGalleryUrl(d);

                    default:
                        return null;
                }
            }

            public static string GetSourceIconUrl(Doujin d)
            {
                switch (d.Source.ToLowerInvariant())
                {
                    case "nhentai":
                        return "https://cdn.cybrhome.com/media/website/live/icon/icon_nhentai.net_57f740.png";

                    case "hitomi":
                        return "https://ltn.hitomi.la/favicon-160x160.png";
                }

                return null;
            }
        }

        public static bool TryParseDoujinIdFromMessage(IMessage message, out (string source, string id) id,
            out bool isFeed)
        {
            var footer = message.Embeds.FirstOrDefault(e => e is Embed)?.Footer?.Text;

            if (footer == null)
            {
                id = (null, null);
                isFeed = false;
                return false;
            }

            // source/id
            var parts = footer.Split('|')[0].Split('/', 2);

            id = (parts[0].Trim(), parts[1].Trim());
            isFeed = footer.Contains("feed");
            return true;
        }
    }
}