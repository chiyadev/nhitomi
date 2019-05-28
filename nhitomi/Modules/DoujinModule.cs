// Copyright (c) 2018-2019 chiya.dev
//
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Options;
using nhitomi.Core;
using nhitomi.Interactivity;

namespace nhitomi.Modules
{
    public class DoujinModule : ModuleBase
    {
        readonly AppSettings _settings;
        readonly IDatabase _database;
        readonly MessageFormatter _formatter;
        readonly InteractiveManager _interactive;

        public DoujinModule(IOptions<AppSettings> options, IDatabase database, MessageFormatter formatter,
            InteractiveManager interactive)
        {
            _settings = options.Value;
            _database = database;
            _formatter = formatter;
            _interactive = interactive;
        }

        [Command("get"), Alias("g")]
        public async Task GetAsync(string source, string id)
        {
            using (Context.Channel.EnterTypingState())
            {
                var doujin = await _database.GetDoujinAsync(source, id);

                if (doujin == null)
                {
                    await ReplyAsync(_formatter.DoujinNotFound(source));
                    return;
                }

                await _interactive.SendInteractiveAsync(new DoujinMessage(doujin), Context);
            }
        }

        [Command("from"), Alias("f")]
        public async Task FromAsync([Remainder] string source = null)
        {
            using (Context.Channel.EnterTypingState())
            {
                IAsyncEnumerable<Doujin> enumerate(IDatabase db, int offset) => db.EnumerateDoujinsAsync(x =>
                {
                    if (!string.IsNullOrEmpty(source))
                        x = x.Where(d => d.Source == source);

                    // todo: ascending option
                    x = x.OrderByDescending(d => d.UploadTime);

                    return x.Skip(offset).Take(50);
                });

                await _interactive.SendInteractiveAsync(new DoujinListMessage(enumerate), Context);
            }
        }

        [Command("search"), Alias("s")]
        public async Task SearchAsync([Remainder] string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                await ReplyAsync(_formatter.InvalidQuery());
                return;
            }

            using (Context.Channel.EnterTypingState())
            {
                IAsyncEnumerable<Doujin> enumerate(IDatabase db, int offset) => db.EnumerateDoujinsAsync(x =>
                    x.FullTextSearch(_database, query,
                        d => d.OriginalName,
                        d => d.PrettyName));

                await _interactive.SendInteractiveAsync(new DoujinListMessage(enumerate), Context);
            }
        }

        [Command("download"), Alias("dl")]
        public async Task DownloadAsync(string source, string id)
        {
            Doujin doujin;

            using (Context.Channel.EnterTypingState())
            {
                // allow downloading only for users of guild
                if (!_settings.Doujin.AllowNonGuildMemberDownloads)
                {
                    var guild = await Context.Client.GetGuildAsync(_settings.Discord.Guild.GuildId);

                    // guild user is null; user is not in guild
                    if (await guild.GetUserAsync(Context.User.Id) == null)
                    {
                        await Context.User.SendMessageAsync(_formatter.JoinGuildForDownload);
                        return;
                    }
                }

                doujin = await _database.GetDoujinAsync(source, id);

                if (doujin == null)
                {
                    await ReplyAsync(_formatter.DoujinNotFound(source));
                    return;
                }
            }

            await _interactive.SendInteractiveAsync(new DownloadMessage(doujin), Context);
        }
    }
}