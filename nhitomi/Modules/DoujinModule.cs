// Copyright (c) 2018-2019 fate/loli
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

namespace nhitomi.Modules
{
    public class DoujinModule : ModuleBase
    {
        readonly AppSettings _settings;
        readonly ISet<IDoujinClient> _clients;
        readonly MessageFormatter _formatter;
        readonly InteractiveManager _interactive;

        public DoujinModule(
            IOptions<AppSettings> options,
            ISet<IDoujinClient> clients,
            MessageFormatter formatter,
            InteractiveManager interactive)
        {
            _settings = options.Value;
            _clients = clients;
            _formatter = formatter;
            _interactive = interactive;
        }

        [Command("get")]
        [Alias("g")]
        [Summary("Retrieves doujin information from the specified source.")]
        public async Task GetAsync(string source, [Remainder] string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return;

            var client = _clients.FindByName(source);

            if (client == null)
            {
                await ReplyAsync(_formatter.UnsupportedSource(source));
                return;
            }

            var response = await ReplyAsync(_formatter.LoadingDoujin(source, id));
            var doujin = await client.GetAsync(id);

            if (doujin == null)
            {
                await response.ModifyAsync(_formatter.DoujinNotFound(source));
            }
            else
            {
                await response.ModifyAsync(embed: _formatter.CreateDoujinEmbed(doujin));
                await _formatter.AddDoujinTriggersAsync(response);
            }
        }

        [Command("all")]
        [Alias("a")]
        [Summary("Displays all doujins from the specified source uploaded recently.")]
        public async Task ListAsync([Remainder] string source = null)
        {
            IUserMessage response;
            IAsyncEnumerable<IDoujin> results;

            if (string.IsNullOrWhiteSpace(source))
            {
                response = await ReplyAsync(_formatter.LoadingDoujin());
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

                response = await ReplyAsync(_formatter.LoadingDoujin(client.Name));
                results = await client.SearchAsync(null);
            }

            if (await _interactive.InitListInteractiveAsync(response, results.Select(_formatter.CreateDoujinEmbed)))
                await _formatter.AddDoujinTriggersAsync(response);
        }

        [Command("search")]
        [Alias("s")]
        [Summary("Searches for doujins by the title and tags across the supported sources " +
                 "that match the specified query.")]
        public async Task SearchAsync([Remainder] string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                await ReplyAsync(_formatter.InvalidQuery());
                return;
            }

            var response = await ReplyAsync(_formatter.SearchingDoujins(query: query));
            var results = Extensions.Interleave(await Task.WhenAll(_clients.Select(c => c.SearchAsync(query))));

            if (await _interactive.InitListInteractiveAsync(response, results.Select(_formatter.CreateDoujinEmbed)))
                await _formatter.AddDoujinTriggersAsync(response);
        }

        [Command("searchen")]
        [Alias("se")]
        [Summary("Equivalent to `n!search english`.")]
        public Task SearchEnglishAsync([Remainder] string query) => SearchAsync(query + " english");

        [Command("searchjp")]
        [Alias("sj")]
        [Summary("Equivalent to `n!search japanese`.")]
        public Task SearchJapaneseAsync([Remainder] string query) => SearchAsync(query + " japanese");

        [Command("searchch")]
        [Alias("sc")]
        [Summary("Equivalent to `n!search chinese`.")]
        public Task SearchChineseAsync([Remainder] string query) => SearchAsync(query + " chinese");

        [Command("download")]
        [Alias("dl")]
        [Summary("Sends a download link for the specified doujin.")]
        public async Task DownloadAsync(string source, [Remainder] string id)
        {
            var guild = await Context.Client.GetGuildAsync(_settings.Discord.Guild.GuildId);

            // allow downloading only for users of guild
            if (guild != null &&
                !_settings.Doujin.AllowNonGuildMemberDownloads &&
                await guild.GetUserAsync(Context.User.Id) == null)
            {
                await Context.User.SendMessageAsync(_formatter.JoinGuildForDownload());
                return;
            }

            var client = _clients.FindByName(source);

            if (client == null)
            {
                await ReplyAsync(_formatter.UnsupportedSource(source));
                return;
            }

            var response = await ReplyAsync(_formatter.LoadingDoujin(source, id));
            var doujin = await client.GetAsync(id);

            if (doujin == null)
            {
                await response.ModifyAsync(_formatter.DoujinNotFound(source));
            }
            else
            {
                await response.ModifyAsync(embed: _formatter.CreateDownloadEmbed(doujin));
                await _formatter.AddDownloadTriggersAsync(response);
            }
        }
    }
}
