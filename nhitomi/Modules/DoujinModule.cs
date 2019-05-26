// Copyright (c) 2018-2019 chiya.dev
//
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System.Collections.Generic;
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
            Doujin doujin;

            using (Context.Channel.EnterTypingState())
            {
                doujin = await _database.GetDoujinAsync(source, id);

                if (doujin == null)
                {
                    await ReplyAsync(_formatter.DoujinNotFound(source));
                    return;
                }
            }

            await _interactive.SendInteractiveAsync(new DoujinMessage(doujin), Context);
        }

        [Command("all"), Alias("a")]
        public async Task ListAsync([Remainder] string source = null)
        {
            DoujinListInteractive interactive;

            using (Context.Channel.EnterTypingState())
            {
                IAsyncEnumerable<IDoujin> results;

                if (string.IsNullOrWhiteSpace(source))
                {
                    results = Extensions.Interleave(await Task.WhenAll(_clients.Select(c => c.SearchAsync(null))));
                }
                else
                {
                    var client = _clients.FindByName(source);

                    if (client == null)
                    {
                        await ReplyAsync(_formatter.UnsupportedSource(source));
                        return;
                    }

                    results = await client.SearchAsync(null);
                }

                interactive = await _interactive.CreateDoujinListInteractiveAsync(results, ReplyAsync);
            }

            if (interactive != null)
                await _formatter.AddDoujinTriggersAsync(interactive.Message);
        }

        [Command("search"), Alias("s")]
        public async Task SearchAsync([Remainder] string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                await ReplyAsync(_formatter.InvalidQuery());
                return;
            }

            DoujinListInteractive interactive;

            using (Context.Channel.EnterTypingState())
            {
                var results = Extensions.Interleave(await Task.WhenAll(_clients.Select(c => c.SearchAsync(query))));

                interactive = await _interactive.CreateDoujinListInteractiveAsync(results, ReplyAsync);
            }

            if (interactive != null)
                await _formatter.AddDoujinTriggersAsync(interactive.Message);
        }

        [Command("searchen"), Alias("se")]
        public Task SearchEnglishAsync([Remainder] string query) => SearchAsync(query + " english");

        [Command("searchjp"), Alias("sj")]
        public Task SearchJapaneseAsync([Remainder] string query) => SearchAsync(query + " japanese");

        [Command("searchch"), Alias("sc")]
        public Task SearchChineseAsync([Remainder] string query) => SearchAsync(query + " chinese");

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