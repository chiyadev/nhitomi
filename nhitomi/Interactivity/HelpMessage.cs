using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.Options;
using nhitomi.Discord;
using nhitomi.Globalization;
using nhitomi.Interactivity.Triggers;

namespace nhitomi.Interactivity
{
    public enum HelpMessageSection
    {
        DoujinAndCollection,
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
                var l = Context.GetLocalization();

                var embed = new EmbedBuilder()
                    .WithTitle(path["title"][l])
                    .WithColor(Color.Purple)
                    .WithThumbnailUrl("https://github.com/chiyadev/nhitomi/raw/master/nhitomi.png")
                    .WithFooter(path["footer"][l, new
                    {
                        version = typeof(Startup).Assembly
                            .GetName().Version
                            .ToString(2),
                        codename = typeof(Startup).Assembly
                            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                            .InformationalVersion
                    }]);

                var prefix = _settings.Discord.Prefix;

                switch (value)
                {
                    case HelpMessageSection.DoujinAndCollection:
                        embed.Description =
                            $"nhitomi — {path["about"][l]}\n\n" +
                            $"{path["invite"][l, new {invite = _settings.Discord.Guild.GuildInvite}]}";

                        path = path["doujins"];

                        embed.AddField($"— {path["heading"][l]} —", $@"
- {prefix}get `source` `id` — {path["get"][l]}
- {prefix}from `source` — {path["from"][l]}
- {prefix}search `query` — {path["search"][l]}
- {prefix}download `source` `id` — {path["download"][l]}
".Trim());

                        path = path.Up["collections"];

                        embed.AddField($"— {path["heading"][l]} —", $@"
- {prefix}collection list — {path["list"][l]}
- {prefix}collection `name` — {path["view"][l]}
- {prefix}collection `name` add `source` `id` — {path["add"][l]}
- {prefix}collection `name` remove `source` `id` — {path["remove"][l]}
- {prefix}collection `name` sort `attribute` — {path["sort"][l]}
- {prefix}collection `name` delete — {path["delete"][l]}
".Trim());
                        break;

                    case HelpMessageSection.Other:
                        path = path["aliases"];

                        embed.AddField($"— {path["heading"][l]} —", $@"
{prefix}**h** — help
{prefix}**g** `source` `id` — get
{prefix}**f** `source` — from
{prefix}**s** `query` — search
{prefix}**dl** `source` `id` — download
{prefix}**c** **l** — collection list
{prefix}**c** `name` — collection
{prefix}**c** `name` **a** `source` `id` — collection add
{prefix}**c** `name` **r** `source` `id` — collection remove
{prefix}**c** `name` **s** `attribute` — collection sort
{prefix}**c** `name` **d** — collection delete
".Trim());

                        path = path.Up["sources"];

                        embed.AddField($"— {path["heading"][l]} —", @"
- nhentai — `https://nhentai.net/`
- Hitomi — `https://hitomi.la/`
".Trim());

                        // only add translations if not English
                        if (l != Localization.Default)
                        {
                            path = path.Up["translations"];

                            embed.AddField(
                                $"— {path["heading"][l]} —",
                                path["text"][l, new {translators = new LocalizationPath()["meta"]["translators"][l]}]);
                        }

                        path = path.Up["openSource"];

                        embed.AddField($"— {path["heading"][l]} —", $@"
{path["license"][l]}
{path["contribution"][l, new {repoUrl = "https://github.com/chiyadev/nhitomi"}]}
".Trim());
                        break;
                }

                return embed.Build();
            }

            protected override Embed CreateEmptyEmbed() => throw new NotImplementedException();
        }
    }
}