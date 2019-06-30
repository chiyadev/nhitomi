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

        public DoujinMessage(Doujin doujin,
                             bool isFeed = false)
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
                CreateEmbed(Message.Doujin, Context.GetLocalization()["doujinMessage"], Message._isFeed);

            public static Embed CreateEmbed(Doujin doujin,
                                            LocalizationAccess l,
                                            bool isFeed = false)
            {
                return new EmbedBuilder
                {
                    Title       = doujin.OriginalName,
                    Description = doujin.OriginalName == doujin.PrettyName ? null : doujin.PrettyName,
                    Url         = GetGalleryUrl(doujin),
                    ImageUrl    = $"https://nhitomi.chiya.dev/api/v1/images/{doujin.AccessId}/-1",
                    Color       = Color.Green,

                    Author = new EmbedAuthorBuilder
                    {
                        Name    = doujin.GetTag(TagType.Artist)?.Value ?? doujin.Source,
                        IconUrl = GetSourceIconUrl(doujin)
                    },

                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"{doujin.Source}/{doujin.SourceId} {(isFeed ? " | feed" : null)}"
                    },

                    Fields = getFields()
                            .Where(x => !string.IsNullOrWhiteSpace(x.value))
                            .Select(x => new EmbedFieldBuilder
                             {
                                 Name  = x.name,
                                 Value = x.value
                             })
                            .ToList()
                }.Build();

                IEnumerable<(string name, string value)> getFields() => new[]
                    {
                        TagType.Language,
                        TagType.Group,
                        TagType.Parody,
                        TagType.Category,
                        TagType.Character,
                        TagType.Tag
                    }.Select(type => (l[type.ToString()].ToString(),
                                      string.Join(", ", doujin.GetTags(type).Select(t => t.Value))))
                     .Append((l["content"], l["contentValue", new { doujin }]));
            }
        }

        public static string GetGalleryUrl(Doujin d) //todo: move this to GalleryUtility.cs
        {
            switch (d.Source.ToLowerInvariant())
            {
                case "nhentai": return nhentaiClient.GetGalleryUrl(d);
                case "hitomi":  return HitomiClient.GetGalleryUrl(d);

                default: return null;
            }
        }

        public static string GetSourceIconUrl(Doujin d)
        {
            switch (d.Source.ToLowerInvariant())
            {
                case "nhentai": return "https://cdn.cybrhome.com/media/website/live/icon/icon_nhentai.net_57f740.png";
                case "hitomi":  return "https://ltn.hitomi.la/favicon-160x160.png";
            }

            return null;
        }

        public static bool TryParseDoujinIdFromMessage(IMessage message,
                                                       out (string source, string id) id,
                                                       out bool isFeed)
        {
            var footer = message.Embeds.FirstOrDefault(e => e is Embed)?.Footer?.Text;

            if (footer == null)
            {
                id     = (null, null);
                isFeed = false;
                return false;
            }

            // source/id
            var parts = footer.Split('|')[0].Split('/', 2);

            id     = (parts[0].Trim(), parts[1].Trim());
            isFeed = footer.Contains("feed");
            return true;
        }
    }
}