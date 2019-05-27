using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using nhitomi.Core;
using nhitomi.Interactivity.Triggers;

namespace nhitomi.Interactivity
{
    public class DownloadMessage : InteractiveMessage
    {
        readonly Doujin _doujin;

        public DownloadMessage(Doujin doujin)
        {
            _doujin = doujin;
        }

        protected override IEnumerable<ReactionTrigger> CreateTriggers()
        {
            yield return new DeleteTrigger();
        }

        protected override async Task<bool> InitializeViewAsync(CancellationToken cancellationToken = default)
        {
            var embed = new EmbedBuilder()
                .WithTitle($"**{_doujin.Source}**: {_doujin.OriginalName ?? _doujin.PrettyName}")
                .WithUrl($"https://nhitomi.chiya.dev/dl/{_doujin.Source}/{_doujin.SourceId}")
                .WithDescription(
                    $"Click the link above to start downloading `{_doujin.OriginalName ?? _doujin.PrettyName}`.\n")
                .WithColor(Color.LightOrange)
                .WithCurrentTimestamp()
                .Build();

            await SetViewAsync(embed, cancellationToken);

            return true;
        }
    }
}