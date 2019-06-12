using System.Collections.Generic;
using Discord;
using nhitomi.Core;
using nhitomi.Discord;
using nhitomi.Globalization;
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
                var path = new LocalizationPath("downloadMessage");
                var l = Context.GetLocalization();

                return new EmbedBuilder()
                    .WithTitle(path["title"][l, new {doujin = Message._doujin}])
                    .WithUrl(GetUrl(Message._doujin))
                    .WithThumbnailUrl($"https://nhitomi.chiya.dev/api/v1/images/{Message._doujin.AccessId}/-1")
                    .WithDescription(path["text"][l, new {doujin = Message._doujin}])
                    .WithColor(Color.LightOrange)
                    .Build();
            }

            static string GetUrl(Doujin d) => $"https://chiya.dev/nhitomi-dl/?id={d.AccessId}";
        }
    }
}