using System.Collections.Generic;
using Discord;
using nhitomi.Core;
using nhitomi.Discord;
using nhitomi.Interactivity.Triggers;

namespace nhitomi.Interactivity
{
    public class DownloadMessage : InteractiveMessage<DownloadMessage.View>
    {
        readonly Doujin _doujin;

        public DownloadMessage(Doujin doujin)
        {
            _doujin = doujin;
        }

        protected override IEnumerable<IReactionTrigger> CreateTriggers()
        {
            yield return new DeleteTrigger();
        }

        public class View : EmbedViewBase
        {
            new DownloadMessage Message => (DownloadMessage) base.Message;

            protected override Embed CreateEmbed()
            {
                var doujin = Message._doujin;
                var l      = Context.GetLocalization()["downloadMessage"];

                return new EmbedBuilder
                {
                    Title        = l["title", new { doujin }],
                    Url          = GetUrl(doujin),
                    ThumbnailUrl = $"https://nhitomi.chiya.dev/api/v1/images/{doujin.AccessId}/-1",
                    Description  = l["text", new { doujin }],
                    Color        = Color.LightOrange
                }.Build();
            }

            static string GetUrl(Doujin d) => $"https://chiya.dev/nhitomi-dl/?id={d.AccessId}";
        }
    }
}