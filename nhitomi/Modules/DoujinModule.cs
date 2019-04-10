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
        public async Task GetAsync(string source, string id)
        {
            var client = _clients.FindByName(source);

            if (client == null)
            {
                await ReplyAsync(_formatter.UnsupportedSource(source));
                return;
            }

            IUserMessage response;

            using (Context.Channel.EnterTypingState())
            {
                var doujin = await client.GetAsync(id);

                if (doujin == null)
                {
                    await ReplyAsync(_formatter.DoujinNotFound(source));
                    return;
                }

                response = await ReplyAsync(embed: _formatter.CreateDoujinEmbed(doujin));
            }

            await _formatter.AddDoujinTriggersAsync(response);
        }

        [Command("all")]
        [Alias("a")]
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

        [Command("search")]
        [Alias("s")]
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

        [Command("searchen")]
        [Alias("se")]
        public Task SearchEnglishAsync([Remainder] string query) => SearchAsync(query + " english");

        [Command("searchjp")]
        [Alias("sj")]
        public Task SearchJapaneseAsync([Remainder] string query) => SearchAsync(query + " japanese");

        [Command("searchch")]
        [Alias("sc")]
        public Task SearchChineseAsync([Remainder] string query) => SearchAsync(query + " chinese");

        [Command("download")]
        [Alias("dl")]
        public async Task DownloadAsync(string source, string id)
        {
            var client = _clients.FindByName(source);

            if (client == null)
            {
                await ReplyAsync(_formatter.UnsupportedSource(source));
                return;
            }

            using (Context.Channel.EnterTypingState())
            {
                var guild = await Context.Client.GetGuildAsync(_settings.Discord.Guild.GuildId);

                // allow downloading only for users of guild
                if (guild != null &&
                    !_settings.Doujin.AllowNonGuildMemberDownloads &&
                    await guild.GetUserAsync(Context.User.Id) == null)
                {
                    await Context.User.SendMessageAsync(_formatter.JoinGuildForDownload);
                    return;
                }

                var doujin = await client.GetAsync(id);

                if (doujin == null)
                {
                    await ReplyAsync(_formatter.DoujinNotFound(source));
                    return;
                }

                await ReplyAsync(embed: _formatter.CreateDownloadEmbed(doujin));
            }
        }
    }
}
