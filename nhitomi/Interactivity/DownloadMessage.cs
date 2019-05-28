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

        protected override void InitializeView(View view)
        {
            base.InitializeView(view);

            view.Doujin = _doujin;
        }

        public class View : EmbedViewBase
        {
            public Doujin Doujin;

            protected override Embed CreateEmbed() => new EmbedBuilder()
                .WithTitle($"**{Doujin.Source}**: {Doujin.OriginalName ?? Doujin.PrettyName}")
                .WithUrl($"https://nhitomi.chiya.dev/dl/{Doujin.Source}/{Doujin.SourceId}")
                .WithDescription(
                    $"Click the link above to start downloading `{Doujin.OriginalName ?? Doujin.PrettyName}`.\n")
                .WithColor(Color.LightOrange)
                .WithCurrentTimestamp()
                .Build();
        }
    }
}