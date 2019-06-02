using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.Options;
using nhitomi.Globalization;
using nhitomi.Interactivity.Triggers;

namespace nhitomi.Interactivity
{
    public enum HelpMessageSection
    {
        Doujins,
        Collections,
        Other
    }

    public class HelpMessage : ListMessage<HelpMessage.View, HelpMessageSection>
    {
        protected override IEnumerable<IReactionTrigger> CreateTriggers()
        {
            foreach (var trigger in base.CreateTriggers())
                yield return trigger;

            yield return new DeleteTrigger();
        }

        public class View : ListViewBase
        {
            readonly AppSettings _settings;

            public View(IOptions<AppSettings> options)
            {
                _settings = options.Value;
            }

            protected override Task<HelpMessageSection[]> GetValuesAsync(int offset,
                CancellationToken cancellationToken = default) =>
                Task.FromResult(
                    Enum.GetValues(typeof(HelpMessageSection))
                        .Cast<HelpMessageSection>()
                        .Skip(offset)
                        .ToArray());

            protected override Embed CreateEmbed(HelpMessageSection value)
            {
                var path = new LocalizationPath("helpMessage");
                var c = Context;

                var embed = new EmbedBuilder()
                    .WithTitle($"**nhitomi**: {path["title"][c]}")
                    .WithColor(Color.Purple)
                    .WithThumbnailUrl(_settings.ImageUrl) //todo: go into localization
                    .WithFooter(path["footer"][c]);

                var prefix = _settings.Discord.Prefix;

                switch (value)
                {
                    case HelpMessageSection.Doujins:
                        embed.Description =
                            $"nhitomi — {path["about"][c]}\n\n" +
                            $"{path["invite"][c, new {invite = _settings.Discord.Guild.GuildInvite}]}";

                        path = path["doujins"];

                        embed.AddField($"— {path["heading"][c]} —", $@"
- {prefix}get `source` `id` — {path["get"][c]}
- {prefix}from `source` — {path["from"][c]}
- {prefix}search `query` — {path["search"][c]}
- {prefix}download `source` `id` — {path["download"][c]}
".Trim());
                        break;

                    case HelpMessageSection.Collections:
                        path = path["collections"];

                        embed.AddField($"— {path["heading"][c]} —", $@"
- {prefix}collection list — {path["list"][c]}
- {prefix}collection `name` — {path["view"][c]}
- {prefix}collection `name` add `source` `id` — {path["add"][c]}
- {prefix}collection `name` remove `source` `id` — {path["remove"][c]}
- {prefix}collection `name` sort `attribute` — {path["sort"][c]}
- {prefix}collection `name` delete — {path["delete"][c]}
".Trim());
                        break;

                    case HelpMessageSection.Other:
                        path = path["sources"];

                        embed.AddField($"— {path["heading"][c]} —", @"
- nhentai — `https://nhentai.net/`
- Hitomi — `https://hitomi.la/`
".Trim());

                        // only add translations if not English
                        if (c != Localization.Default)
                        {
                            path = path.Up["translations"];

                            embed.AddField(
                                $"— {path["heading"][c]} —",
                                path["text"][c, new {translators = new LocalizationPath()["meta"]["translators"][c]}]);
                        }

                        path = path.Up["openSource"];

                        embed.AddField($"— {path["heading"][c]} —", $@"
{path["license"][c]}
{path["contribution"][c, new {repoUrl = "https://github.com/chiyadev/nhitomi"}]}
".Trim());
                        break;
                }

                return embed.Build();
            }

            protected override Embed CreateEmptyEmbed() => throw new NotImplementedException();
        }
    }
}