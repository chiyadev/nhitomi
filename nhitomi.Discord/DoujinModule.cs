// Copyright (c) 2018 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Options;

namespace nhitomi
{
    public class DoujinModule : ModuleBase
    {
        readonly AppSettings _settings;
        readonly InteractiveScheduler _interactive;
        readonly ISet<IDoujinClient> _clients;

        public DoujinModule(
            IOptions<AppSettings> options,
            InteractiveScheduler interactive,
            ISet<IDoujinClient> clients
        )
        {
            _settings = options.Value;
            _interactive = interactive;
            _clients = clients;
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
                    "Please see refer to the manual (**n!help**) for a full list of supported sources."
                );

            return (client, response);
        }

        async Task<(IDoujinClient, IDoujin, double[], IUserMessage)> getAsync(string source, string id)
        {
            var (client, response) = await getClientAsync(source);

            if (client == null)
                return (client, null, null, response);

            // Send placeholder message
            response = await ReplyAsync($"**{client.Name}**: Loading __{id}__...");

            // Load doujin
            IDoujin doujin;
            double[] elapsed;

            using (Extensions.Measure(out elapsed))
                doujin = await client.GetAsync(id);

            // Show result
            if (doujin == null)
            {
                await response.ModifyAsync(
                    content: $"**{client.Name}**: No such doujin!"
                );
                return (client, null, elapsed, response);
            }

            return (client, doujin, elapsed, response);
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
            var (client, doujin, elapsed, response) = await getAsync(source, id);

            if (doujin == null)
                return;

            await response.ModifyAsync(
                content: $"**{client.Name}**: Loaded __{id}__ in {elapsed.Format()}",
                embed: MessageFormatter.EmbedDoujin(doujin)
            );

            // Message for download toggling
            var downloadMessage = (IUserMessage)null;

            // Create interactive
            await _interactive.CreateInteractiveAsync(
                requester: Context.User,
                response: response,
                triggers: add => add(
                    ("\uD83D\uDCBE", toggleDownload)
                ),
                allowTrash: true,
                onExpire: () => downloadMessage?.DeleteAsync()
            );

            async Task toggleDownload()
            {
                if (downloadMessage != null)
                {
                    await downloadMessage.DeleteAsync();
                    downloadMessage = null;
                }
                else
                    downloadMessage = await ShowDownload(doujin, response.Channel, _settings);
            }
        }

        public static Task<IUserMessage> ShowDownload(
            IDoujin doujin,
            IMessageChannel channel,
            AppSettings settings
        )
        {
            var secret = settings.Discord.Token;
            var validLength = settings.Doujin.TokenValidLength;

            // Create token
            var downloadToken = doujin.CreateToken(secret, expiresIn: validLength);

            // Send download message
            return channel.SendMessageAsync(
                text: string.Empty,
                embed: MessageFormatter.EmbedDownload(
                    doujinName: doujin.PrettyName,
                    link: $"{settings.Http.Url}/dl/{downloadToken}",
                    validLength: validLength
                )
            );
        }

        [Command("all")]
        [Alias("a")]
        [Summary("Displays all doujins from the specified source uploaded recently.")]
        [Remarks("n!all hitomi")]
        public async Task ListAsync(
            [Remainder]
            string source = null
        )
        {
            IUserMessage response;
            IAsyncEnumerable<IDoujin> results;

            if (string.IsNullOrWhiteSpace(source))
            {
                response = await ReplyAsync($"**nhitomi**: Loading...");
                results = Extensions.Interleave(
                    await Task.WhenAll(_clients.Select(c => c.SearchAsync(null)))
                );
            }
            else
            {
                // Get client
                IDoujinClient client;
                (client, response) = await getClientAsync(source);

                if (client == null)
                    return;

                results = await client.SearchAsync(null);
            }

            // Interleave results from each client
            await DisplayListAsync(
                request: Context.Message,
                response: response,
                results: results,
                interactive: _interactive,
                settings: _settings
            );
        }

        [Command("search")]
        [Alias("s")]
        [Summary("Searches for doujins by the title and tags across the supported sources that match the specified query.")]
        [Remarks("n!search glasses loli")]
        public async Task SearchAsync(
            [Remainder]
            string query
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
            var results = await Task.WhenAll(_clients.Select(c => c.SearchAsync(query)));

            // Interleave results from each client
            await DisplayListAsync(
                request: Context.Message,
                response: response,
                results: Extensions.Interleave(results),
                interactive: _interactive,
                settings: _settings
            );
        }

        public static async Task DisplayListAsync(
            IUserMessage request,
            IUserMessage response,
            IAsyncEnumerable<IDoujin> results,
            InteractiveScheduler interactive,
            AppSettings settings
        )
        {
            var browser = new EnumerableBrowser<IDoujin>(
                results
                    .Where(d => d != null)
                    .GetEnumerator()
            );

            double[] elapsed;

            // Load first item manually
            using (Extensions.Measure(out elapsed))
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
            var downloadMessage = (IUserMessage)null;

            // Don't proceed creating list interactive if there is only one result
            if (!await browser.MoveNext())
            {
                await interactive.CreateInteractiveAsync(
                    requester: request.Author,
                    response: response,
                    triggers: add => add(
                        ("\uD83D\uDCBE", toggleDownload)
                    ),
                    allowTrash: true,
                    onExpire: () => downloadMessage?.DeleteAsync()
                );

                browser.Dispose();
                return;
            }
            else browser.MovePrevious();

            // Create list interactive
            await interactive.CreateInteractiveAsync(
                requester: request.Author,
                response: response,
                triggers: add => add(
                    ("\u25c0", loadPrevious),
                    ("\u25b6", loadNext),
                    ("\uD83D\uDCBE", toggleDownload)
                ),
                onExpire: () =>
                {
                    browser.Dispose();
                    return downloadMessage?.DeleteAsync();
                },
                allowTrash: true
            );

            // Update content as the current doujin
            Task updateView(string content = null) => response.ModifyAsync(
                content: content ?? $"**{browser.Current.Source.Name}**: Loaded __{browser.Current.Id}__ in {elapsed.Format()}",
                embed: MessageFormatter.EmbedDoujin(browser.Current)
            );

            // Load next doujin
            async Task loadNext()
            {
                await response.ModifyAsync($"**nhitomi**: Loading...");

                using (Extensions.Measure(out elapsed))
                    if (!await browser.MoveNext())
                    {
                        await updateView($"**nhitomi**: Reached the end of list!");
                        return;
                    }

                await updateView();
                await updateDownload();
            }

            // Load previous doujin
            async Task loadPrevious()
            {
                await response.ModifyAsync($"**nhitomi**: Loading...");

                using (Extensions.Measure(out elapsed))
                    if (!browser.MovePrevious())
                    {
                        await updateView($"**nhitomi**: Reached the start of list!");
                        return;
                    }

                await updateView();
                await updateDownload();
            }

            async Task toggleDownload()
            {
                if (downloadMessage != null)
                {
                    await downloadMessage.DeleteAsync();
                    downloadMessage = null;
                }
                else
                    downloadMessage = await ShowDownload(browser.Current, response.Channel, settings);
            }

            async Task updateDownload()
            {
                if (downloadMessage == null)
                    return;

                var secret = settings.Discord.Token;
                var validLength = settings.Doujin.TokenValidLength;

                // Create token
                var downloadToken = browser.Current.CreateToken(secret, expiresIn: validLength);

                await downloadMessage.ModifyAsync(
                    content: string.Empty,
                    embed: MessageFormatter.EmbedDownload(
                        doujinName: browser.Current.PrettyName,
                        link: $"{settings.Http.Url}/dl/{downloadToken}",
                        validLength: validLength
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
            var (client, doujin, elapsed, response) = await getAsync(source, id);

            if (doujin == null)
                return;

            var secret = _settings.Discord.Token;
            var validLength = _settings.Doujin.TokenValidLength;

            // Create token
            var downloadToken = doujin.CreateToken(secret, expiresIn: validLength);

            await response.ModifyAsync(
                content: $"**{client.Name}**: Loaded __{id}__ in {elapsed.Format()}",
                embed: MessageFormatter.EmbedDownload(
                    doujinName: doujin.PrettyName,
                    link: $"{_settings.Http.Url}/dl/{downloadToken}",
                    validLength: validLength
                )
            );
        }
    }
}