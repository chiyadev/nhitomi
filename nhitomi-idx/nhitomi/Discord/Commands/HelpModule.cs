using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.Options;
using nhitomi.Localization;
using nhitomi.Scrapers;
using Qmmands;

namespace nhitomi.Discord.Commands
{
    public class HelpModule : ModuleBase<nhitomiCommandContext>
    {
        [Command("help", "h"), Name("help")]
        public Task HelpAsync() => Context.SendAsync<HelpMessage>();
    }

    public class HelpMessage : InteractiveMessage, IListTriggerTarget
    {
        readonly ILocale _l;
        readonly IDiscordClient _client;
        readonly IScraperService _scrapers;
        readonly DiscordOptions _options;

        public HelpMessage(nhitomiCommandContext context, IDiscordClient client, IScraperService scrapers, IOptionsSnapshot<DiscordOptions> options)
        {
            _l        = context.Locale.Sections["help"];
            _client   = client;
            _scrapers = scrapers;
            _options  = options.Value;
        }

        protected override IEnumerable<ReactionTrigger> CreateTriggers()
        {
            foreach (var trigger in base.CreateTriggers())
                yield return trigger;

            yield return new ListTrigger(this, ListTriggerDirection.Left);
            yield return new ListTrigger(this, ListTriggerDirection.Right);
        }

        enum Page
        {
            Book,
            Collection,
            OpenSource
        }

        Page _current;

        protected override ReplyContent Render()
        {
            var title = $"nhitomi: {_l["title"]}";

            var content = new ReplyContent
            {
                Embed = new EmbedBuilder
                {
                    Title        = title,
                    Color        = Color.Purple,
                    ThumbnailUrl = "https://github.com/chiyadev/nhitomi/raw/master/nhitomi.png",
                    Footer = new EmbedFooterBuilder
                    {
                        Text = $"v{VersionInfo.Version} {VersionInfo.Commit.ShortHash} — {_l["owner"]}"
                    }
                }
            };

            switch (_current)
            {
                case Page.Book:
                {
                    var tagline = $"nhitomi — {_l["tagline"]}";
                    var invite  = _l["invite", new { serverInvite = _options.ServerInvite, botInvite = $@"https://discordapp.com/oauth2/authorize?scope=bot&client_id={_client.CurrentUser.Id}&permissions={_options.BotInvitePerms}" }];

                    content.Embed.Description = $"{tagline}\n\n{invite}";

                    var l = _l.Sections["doujinshi"];

                    content.Embed.Fields.AddRange(new[]
                    {
                        new EmbedFieldBuilder
                        {
                            Name = l["title"],
                            Value = $@"
- `{_options.Prefix}get [link]` — {l["get"]}
- `{_options.Prefix}from [source]` — {l["from"]}
- `{_options.Prefix}search [query]` — {l["search"]}
- `{_options.Prefix}view [link]` — {l["view"]}
- `{_options.Prefix}download [link]` — {l["download"]}
".Trim()
                        },
                        new EmbedFieldBuilder
                        {
                            Name  = l["sources.title"],
                            Value = string.Join('\n', _scrapers.Books.Select(s => $"- {s.Type.GetEnumName()} — {s.Url}"))
                        }
                    });
                    break;
                }

                case Page.Collection:
                {
                    var l = _l.Sections["collections"];

                    content.Embed.Fields.AddRange(new[]
                    {
                        new EmbedFieldBuilder
                        {
                            Name = l["title"],
                            Value = $@"
- `{_options.Prefix}collection` — {l["list"]}
- `{_options.Prefix}collection [name]` — {l["show"]}
- `{_options.Prefix}collection [name] add [link]` — {l["add"]}
- `{_options.Prefix}collection [name] remove [link]` — {l["remove"]}
- `{_options.Prefix}collection [name] delete [link]` — {l["delete"]}
".Trim()
                        }
                    });
                    break;
                }

                case Page.OpenSource:
                {
                    var l = _l.Sections["oss"];

                    content.Embed.Fields.AddRange(new[]
                    {
                        new EmbedFieldBuilder
                        {
                            Name = l["title"],
                            Value = $@"
{l["license"]}
[GitHub](https://github.com/chiyadev/nhitomi) / [License](https://github.com/chiyadev/nhitomi/blob/master/LICENSE)"
                        }
                    });
                    break;
                }
            }

            return content;
        }

        int IListTriggerTarget.Position => (int) _current;

        public Task<bool> SetPositionAsync(int position, CancellationToken cancellationToken = default)
        {
            if (position > (int) Page.OpenSource)
                return Task.FromResult(false);

            _current = (Page) position;

            return Task.FromResult(true);
        }
    }
}