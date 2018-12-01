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

        [Command("get")]
        [Alias("g")]
        [Summary("Retrieves doujin information from the specified source.")]
        [Remarks("n!get nhentai 177013")]
        public async Task GetAsync(
            string source,
            [Remainder] string id
        )
        {
            source = source?.Trim();

            // Find matching client
            var client = _clients.FirstOrDefault(c => source.Equals(c.Name, StringComparison.OrdinalIgnoreCase));

            if (client == null)
            {
                await ReplyAsync(
                    $"**nhitomi**: Source __{source}__ is not supported. Please see refer to the manual (**n!help**) for a full list of supported sources."
                );
                return;
            }

            // Send placeholder message
            var response = await ReplyAsync($"**{client.Name}**: Loading __{id}__...");

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
                return;
            }

            await response.ModifyAsync(
                content: $"**{client.Name}**: Loaded __{id}__ in {elapsed.Format()}",
                embed: MessageFormatter.EmbedDoujin(doujin)
            );

            // Create interactive
            await _interactive.CreateInteractiveAsync(
                requester: Context.User,
                response: response,
                triggers: add => add(
                    ("\uD83D\uDCBE", showDownload)
                ),
                allowTrash: true
            );

            Task showDownload() => ShowDownload(doujin, response.Channel, _settings);
        }

        public static async Task ShowDownload(
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
            await channel.SendMessageAsync(
                text: string.Empty,
                embed: MessageFormatter.EmbedDownload(
                    doujinName: doujin.PrettyName,
                    link: $"{settings.Http.Url}/dl/{downloadToken}",
                    validLength: validLength
                )
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
            var browser = new EnumerableBrowser<IDoujin>(results.GetEnumerator());

            IDoujin doujin;
            double[] elapsed;

            // Load first item manually
            using (Extensions.Measure(out elapsed))
                if (await browser.MoveNext())
                {
                    doujin = browser.Current;
                    await updateView();
                }
                else
                {
                    await response.ModifyAsync("**nhitomi**: No results...");
                    return;
                }

            // Don't proceed creating list interactive if there is only one result
            if (!await browser.MoveNext())
            {
                await interactive.CreateInteractiveAsync(
                    requester: request.Author,
                    response: response,
                    triggers: add => add(
                        ("\uD83D\uDCBE", showDownload)
                    ),
                    allowTrash: true
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
                    ("\uD83D\uDCBE", showDownload)
                ),
                onExpire: () => { browser.Dispose(); return Task.CompletedTask; },
                allowTrash: true
            );

            // Update content as the current doujin
            Task updateView(string content = null) => response.ModifyAsync(
                content: content ?? $"**{doujin.Source.Name}**: Loaded __{doujin.Id}__ in {elapsed.Format()}",
                embed: MessageFormatter.EmbedDoujin(doujin)
            );

            // Load next doujin
            async Task loadNext()
            {
                await response.ModifyAsync($"**nhitomi**: **[{browser.Index + 2}]** Loading...");

                using (Extensions.Measure(out elapsed))
                    if (!await browser.MoveNext())
                    {
                        await updateView($"**nhitomi**: **[{browser.Index + 1}]** Reached the end of list!");
                        return;
                    }

                doujin = browser.Current;
                await updateView();
            }

            // Load previous doujin
            async Task loadPrevious()
            {
                await response.ModifyAsync($"**nhitomi**: **[{browser.Index}]** Loading...");

                using (Extensions.Measure(out elapsed))
                    if (!browser.MovePrevious())
                    {
                        await updateView($"**nhitomi**: **[{browser.Index + 1}]** Reached the start of list!");
                        return;
                    }

                doujin = browser.Current;
                await updateView();
            }

            Task showDownload() => ShowDownload(doujin, response.Channel, settings);
        }
    }
}