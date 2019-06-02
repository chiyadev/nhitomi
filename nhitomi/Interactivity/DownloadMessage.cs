using System.Collections.Generic;
using Discord;
using nhitomi.Core;
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
            public new DownloadMessage Message => (DownloadMessage) base.Message;

            protected override Embed CreateEmbed() => new EmbedBuilder()
                .WithTitle($"**{Message._doujin.Source}**: {Message._doujin.Name}")
                .WithUrl($"https://nhitomi.chiya.dev/v1/dl/{Message._doujin.Source}/{Message._doujin.SourceId}")
                .WithDescription(
                    $"Click the link above to start downloading `{Message._doujin.Name}`.\n")
                .WithColor(Color.LightOrange)
                .WithCurrentTimestamp()
                .Build();
        }
    }
}