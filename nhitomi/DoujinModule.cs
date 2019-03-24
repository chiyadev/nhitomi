// Copyright (c) 2018-2019 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using nhitomi.Core;
using Newtonsoft.Json;

namespace nhitomi
{
    public class DoujinModule : ModuleBase
    {
        readonly AppSettings _settings;
        readonly InteractiveScheduler _interactive;
        readonly ISet<IDoujinClient> _clients;
        readonly JsonSerializer _json;

        public DoujinModule(
            IOptions<AppSettings> options,
            InteractiveScheduler interactive,
            ISet<IDoujinClient> clients,
            JsonSerializer json
        )
        {
            _settings = options.Value;
            _interactive = interactive;
            _clients = clients;
            _json = json;
        }

        async Task<(IDoujinClient, IUserMessage)> getClientAsync(string source)
        {
            source = source?.Trim();

            // Find matching client
            var client = _clients.FirstOrDefault(c => c.Name.Equals(source, StringComparison.OrdinalIgnoreCase));

            IUserMessage response = null;

            if (client == null)
                response = await ReplyAsync(
                    $"**nhitomi**: Source __{source}__ is not supported. " +
                    $"Please see refer to the manual (**{_settings.Prefix}help**) for a full list of supported sources."
                );

            return (client, response);
        }

        async Task<(IDoujinClient, IDoujin, IUserMessage)> getDoujinAsync(string source, string id)
        {
            var (client, response) = await getClientAsync(source);

            if (client == null)
                return (null, null, response);

            // Send placeholder message
            response = await ReplyAsync($"**{client.Name}**: Loading __{id}__...");

            // Load doujin
            var doujin = await client.GetAsync(id);

            if (doujin == null)
            {
                await response.ModifyAsync($"**{client.Name}**: No such doujin!");
            }

            return (client, doujin, response);
        }

        [Command("get")]
        [Alias("g")]
        [Summary("Retrieves doujin information from the specified source.")]
        [Remarks("n!get nhentai 177013")]
        public async Task GetAsync(
            string source,
            [Remainder] string id
        )
        {
            var (client, doujin, response) = await getDoujinAsync(source, id);

            if (doujin == null)
                return;

            await response.ModifyAsync(
                $"**{client.Name}**: __{id}__",
                MessageFormatter.EmbedDoujin(doujin)
            );

            await ShowDoujin(_interactive, Context.User, response, doujin, Context.Client, _json, _settings);
        }

        public static async Task ShowDoujin(
            InteractiveScheduler interactive,
            IUser requester,
            IUserMessage response,
            IDoujin doujin,
            IDiscordClient client,
            JsonSerializer serializer,
            AppSettings settings
        )
        {
            // Message for download toggling
            var downloadMessage = (IUserMessage) null;

            // Create interactive
            await interactive.CreateInteractiveAsync(
                requester,
                response,
                add => add(
                    ("\uD83D\uDCBE", toggleDownload)
                ),
                allowTrash: true,
                onExpire: () => downloadMessage?.DeleteAsync()
            );

            async Task toggleDownload(SocketReaction reaction)
            {
                if (downloadMessage != null)
                {
                    await downloadMessage.DeleteAsync();
                    downloadMessage = null;
                }
                else
                {
                    downloadMessage =
                        await ShowDownload(doujin, response.Channel, requester, client, serializer, settings);
                }
            }
        }

        public static async Task<IUserMessage> ShowDownload(
            IDoujin doujin,
            IMessageChannel channel,
            IUser user,
            IDiscordClient client,
            JsonSerializer serializer,
            AppSettings settings
        )
        {
            var guild = await client.GetGuildAsync(515395714264858653);

            // Allow downloading only for users of guild
            if (await guild.GetUserAsync(user.Id) == null)
                return await user.SendMessageAsync(
                    $"**nhitomi**: Please join our server to enable downloading! https://discord.gg/JFNga7q");

            var secret = settings.Discord.Token;
            var validLength = settings.Doujin.DownloadValidLength;

            // Create token
            var downloadToken = doujin.CreateDownloadToken(
                secret,
                expireMinutes: validLength,
                serializer: serializer);

            // Send download message
            return await channel.SendMessageAsync(
                string.Empty,
                embed: MessageFormatter.EmbedDownload(
                    doujin.PrettyName,
                    $"{settings.Http.Url}/dl/{downloadToken}",
                    validLength
                )
            );
        }

        [Command("all")]
        [Alias("a")]
        [Summary("Displays all doujins from the specified source uploaded recently.")]
        [Remarks("n!all hitomi")]
        public async Task ListAsync(
            [Remainder] string source = null
        )
        {
            IUserMessage response;
            IAsyncEnumerable<IDoujin> results;

            if (string.IsNullOrWhiteSpace(source))
            {
                response = await ReplyAsync($"**nhitomi**: Loading...");
                results = Extensions.Interleave(await Task.WhenAll(_clients.Select(c => c.SearchAsync(null))));
            }
            else
            {
                // Get client
                IDoujinClient client;
                (client, response) = await getClientAsync(source);

                if (client == null)
                    return;

                response = await ReplyAsync($"**{client.Name}**: Loading...");
                results = await client.SearchAsync(null);
            }

            // Interleave results from each client
            await DisplayListAsync(Context.Message, response, results, _interactive, Context.Client, _json, _settings);
        }

        [Command("search")]
        [Alias("s")]
        [Summary(
            "Searches for doujins by the title and tags across the supported sources that match the specified query.")]
        [Remarks("n!search glasses")]
        public async Task SearchAsync(
            [Remainder] string query
        )
        {
            query = query?.Trim();

            if (string.IsNullOrEmpty(query))
            {
                await ReplyAsync("**nhitomi**: Please specify your query.");
                return;
            }

            // Send placeholder message
            var response = await ReplyAsync($"**nhitomi**: Searching __{query}__...");
            var results = Extensions.Interleave(await Task.WhenAll(_clients.Select(c => c.SearchAsync(query))));

            // Interleave results from each client
            await DisplayListAsync(Context.Message, response, results, _interactive, Context.Client, _json, _settings);
        }

        [Command("searchen")]
        [Alias("se")]
        [Summary("Equivalent to `n!search english`.")]
        [Remarks("n!searchen neko")]
        public Task SearchEnglishAsync([Remainder] string query) => SearchAsync(query + " english");

        [Command("searchjp")]
        [Alias("sj")]
        [Summary("Equivalent to `n!search japanese`.")]
        [Remarks("n!searchjp maid")]
        public Task SearchJapaneseAsync([Remainder] string query) => SearchAsync(query + " japanese");

        [Command("searchch")]
        [Alias("sc")]
        [Summary("Equivalent to `n!search chinese`.")]
        [Remarks("n!searchch inu")]
        public Task SearchChineseAsync([Remainder] string query) => SearchAsync(query + " chinese");

        public static async Task DisplayListAsync(
            IUserMessage request,
            IUserMessage response,
            IAsyncEnumerable<IDoujin> results,
            InteractiveScheduler interactive,
            IDiscordClient client,
            JsonSerializer serializer,
            AppSettings settings
        )
        {
            var browser = new EnumerableBrowser<IDoujin>(
                results
                    .Where(d => d != null)
                    .GetEnumerator()
            );

            // Load first item manually
            if (await browser.MoveNext())
            {
                await updateView();
            }
            else
            {
                await response.ModifyAsync("**nhitomi**: No results...");
                return;
            }

            // Message for download toggling
            var downloadMessage = (IUserMessage) null;

            // Don't proceed creating list interactive if there is only one result
            if (!await browser.MoveNext())
            {
                await interactive.CreateInteractiveAsync(
                    request.Author,
                    response,
                    add => add(
                        ("\uD83D\uDCBE", toggleDownload)
                    ),
                    allowTrash: true,
                    onExpire: () => downloadMessage?.DeleteAsync()
                );

                browser.Dispose();
                return;
            }

            browser.MovePrevious();

            // Create list interactive
            await interactive.CreateInteractiveAsync(
                request.Author,
                response,
                add => add(
                    ("\u25c0", loadPrevious),
                    ("\u25b6", loadNext),
                    ("\uD83D\uDCBE", toggleDownload)
                ),
                () =>
                {
                    browser.Dispose();
                    return downloadMessage?.DeleteAsync();
                },
                true
            );

            // Update content as the current doujin
            Task updateView(string content = null) => response.ModifyAsync(
                content ?? $"**{browser.Current.Source.Name}**: __{browser.Current.Id}__",
                MessageFormatter.EmbedDoujin(browser.Current)
            );

            // Load next doujin
            async Task loadNext(SocketReaction reaction)
            {
                if (!await browser.MoveNext())
                {
                    await updateView($"**nhitomi**: End of list!");
                    return;
                }

                await updateView();
                await updateDownload();
            }

            // Load previous doujin
            async Task loadPrevious(SocketReaction reaction)
            {
                if (!browser.MovePrevious())
                {
                    await updateView($"**nhitomi**: Start of list!");
                    return;
                }

                await updateView();
                await updateDownload();
            }

            async Task toggleDownload(SocketReaction reaction)
            {
                if (downloadMessage != null)
                {
                    await downloadMessage.DeleteAsync();
                    downloadMessage = null;
                }
                else
                    downloadMessage = await ShowDownload(browser.Current, response.Channel, request.Author, client,
                        serializer, settings);
            }

            async Task updateDownload()
            {
                if (downloadMessage == null)
                    return;

                var secret = settings.Discord.Token;
                var validLength = settings.Doujin.DownloadValidLength;

                // Create token
                var downloadToken = browser.Current.CreateDownloadToken(
                    secret,
                    expireMinutes: validLength,
                    serializer: serializer);

                await downloadMessage.ModifyAsync(
                    string.Empty,
                    MessageFormatter.EmbedDownload(
                        browser.Current.PrettyName,
                        $"{settings.Http.Url}/dl/{downloadToken}",
                        validLength
                    )
                );
            }
        }

        [Command("download")]
        [Alias("dl")]
        [Summary("Sends a download link for the specified doujin.")]
        [Remarks("n!download nhentai 177013")]
        public async Task DownloadAsync(
            string source,
            [Remainder] string id
        )
        {
            var guild = await Context.Client.GetGuildAsync(515395714264858653);

            // Allow downloading only for users of guild
            if ((await guild.GetUserAsync(Context.User.Id)) == null)
            {
                await Context.User.SendMessageAsync(
                    $"**nhitomi**: Please join our server to enable downloading! https://discord.gg/JFNga7q");
                return;
            }

            var (client, doujin, response) = await getDoujinAsync(source, id);

            if (doujin == null)
                return;

            var secret = _settings.Discord.Token;
            var validLength = _settings.Doujin.DownloadValidLength;

            // Create token
            var downloadToken = doujin.CreateDownloadToken(
                secret,
                expireMinutes: validLength,
                serializer: _json);

            await response.ModifyAsync(
                $"**{client.Name}**: Download __{id}__",
                MessageFormatter.EmbedDownload(
                    doujin.PrettyName,
                    $"{_settings.Http.Url}/dl/{downloadToken}",
                    validLength
                )
            );
        }
    }
}
