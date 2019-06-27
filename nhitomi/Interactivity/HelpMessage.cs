using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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
        Doujins,
        Collections,
        Options,
        Examples,
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
                                                                         CancellationToken cancellationToken =
                                                                             default) => Task.FromResult(
                Enum.GetValues(typeof(HelpMessageSection))
                    .Cast<HelpMessageSection>()
                    .Skip(offset)
                    .ToArray());

            protected override Embed CreateEmbed(HelpMessageSection value)
            {
                var path = new LocalizationPath("helpMessage");
                var l    = Context.GetLocalization();

                var version = new
                {
                    version = typeof(Startup).Assembly.GetName()
                                             .Version.ToString(2),

                    codename = typeof(Startup).Assembly
                                              .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                              .InformationalVersion
                };

                var embed = new EmbedBuilder()
                           .WithTitle(path["title"][l])
                           .WithColor(Color.Purple)
                           .WithThumbnailUrl("https://github.com/chiyadev/nhitomi/raw/master/nhitomi.png")
                           .WithFooter(path["footer"][l, version]);

                switch (value)
                {
                    case HelpMessageSection.Doujins:

                        embed.Description =
                            $"nhitomi — {path["about"][l]}\n\n" +
                            $"{path["invite"][l, new { botInvite = _settings.Discord.BotInvite, guildInvite = _settings.Discord.Guild.GuildInvite }]}";

                        DoujinsSection(embed, path, l);
                        SourcesSection(embed, path, l);
                        break;

                    case HelpMessageSection.Collections:
                        CollectionsSection(embed, path, l);
                        break;

                    case HelpMessageSection.Options:
                        OptionsSection(embed, path, l);
                        break;

                    case HelpMessageSection.Examples:
                        ExamplesSection(embed, path, l);
                        break;

                    case HelpMessageSection.Other:
                        LanguagesSection(embed, path, l);

                        // only add translators if not English
                        if (l != Localization.Default)
                            TranslationsSection(embed, path, l);

                        OpenSourceSection(embed, path, l);
                        break;
                }

                return embed.Build();
            }

            void DoujinsSection(EmbedBuilder embed,
                                LocalizationPath path,
                                Localization l)
            {
                path = path["doujins"];

                var prefix = _settings.Discord.Prefix;

                embed.AddField($"— {path["heading"][l]} —",
                               $@"
- {prefix}get `source` `id` — {path["get"][l]}
- {prefix}from `source` — {path["from"][l]}
- {prefix}read `source` `id` — {path["read"][l]}
- {prefix}download `source` `id` — {path["download"][l]}
- {prefix}search `query` — {path["search"][l]}
".Trim());
            }

            static void SourcesSection(EmbedBuilder embed,
                                       LocalizationPath path,
                                       Localization l)
            {
                path = path["sources"];

                embed.AddField($"— {path["heading"][l]} —",
                               @"
- nhentai — `https://nhentai.net/`
- Hitomi — `https://hitomi.la/`
".Trim());
            }

            void CollectionsSection(EmbedBuilder embed,
                                    LocalizationPath path,
                                    Localization l)
            {
                path = path["collections"];

                var prefix = _settings.Discord.Prefix;

                embed.AddField($"— {path["heading"][l]} —",
                               $@"
- {prefix}collection list — {path["list"][l]}
- {prefix}collection `name` — {path["view"][l]}
- {prefix}collection `name` add `source` `id` — {path["add"][l]}
- {prefix}collection `name` remove `source` `id` — {path["remove"][l]}
- {prefix}collection `name` sort `attribute` — {path["sort"][l]}
- {prefix}collection `name` delete — {path["delete"][l]}
".Trim());
            }

            void OptionsSection(EmbedBuilder embed,
                                LocalizationPath path,
                                Localization l)
            {
                path = path["options"];

                var prefix = _settings.Discord.Prefix;

                embed.AddField($"— {path["heading"][l]} —",
                               $@"
- {prefix}option language `name` — {path["language"][l]}

- {prefix}feed add `tag` — {path["feed.add"][l]}
- {prefix}feed remove `tag` — {path["feed.remove"][l]}
- {prefix}feed mode `mode` — {path["feed.mode"][l]}
".Trim());
            }

            void ExamplesSection(EmbedBuilder embed,
                                 LocalizationPath path,
                                 Localization l)
            {
                path = path["examples"];

                var prefix = _settings.Discord.Prefix;

                embed.AddField($"— {path["heading"][l]} —",
                               $@"
{path["doujins"][l]}:
`{prefix}get nhentai 123`
`{prefix}dl hitomi 12345`
`{prefix}search glasses`

{path["collections"][l]}:
`{prefix}c list`
`{prefix}c favorites`
`{prefix}c favorites add nhentai 123`
`{prefix}c favorites remove nhentai 321`

{path["language"][l]}:
`{prefix}o language {l.Culture.Name}`
".Trim());
            }

            static void LanguagesSection(EmbedBuilder embed,
                                         LocalizationPath path,
                                         Localization l)
            {
                path = path["languages"];

                var builder = new StringBuilder();

                foreach (var localization in Localization.GetAllLocalizations())
                {
                    builder.AppendLine($"- `{localization.Culture.Name}` " +
                                       $"— {localization.Culture.EnglishName} | {localization.Culture.NativeName}");
                }

                embed.AddField($"— {path["heading"][l]} —", builder.ToString());
            }

            static void TranslationsSection(EmbedBuilder embed,
                                            LocalizationPath path,
                                            Localization l)
            {
                path = path["translations"];

                embed.AddField(
                    $"— {path["heading"][l]} —",
                    path["text"][l, new { translators = new LocalizationPath()["meta.translators"][l] }]);
            }

            static void OpenSourceSection(EmbedBuilder embed,
                                          LocalizationPath path,
                                          Localization l)
            {
                path = path["openSource"];

                embed.AddField($"— {path["heading"][l]} —",
                               $@"
{path["license"][l]}
{path["contribution"][l, new { repoUrl = "https://github.com/chiyadev/nhitomi" }]}
".Trim());
            }

            protected override Embed CreateEmptyEmbed() => throw new NotSupportedException();
        }
    }
}