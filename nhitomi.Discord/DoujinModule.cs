using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace nhitomi
{
    public class DoujinModule : ModuleBase
    {
        readonly InteractiveScheduler _interactive;
        readonly ISet<IDoujinClient> _clients;

        public DoujinModule(
            InteractiveScheduler interactive,
            ISet<IDoujinClient> clients
        )
        {
            _interactive = interactive;
            _clients = clients;
        }

        [Command("get")]
        [Summary("Retrieves doujin information from the specified source.")]
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
                await response.ModifyAsync(
                    content: $"**{client.Name}**: No such doujin!"
                );
            else
                await response.ModifyAsync(
                    content: $"**{client.Name}**: Loaded __{id}__ in {elapsed.Format()}",
                    embed: MessageFormatter.EmbedDoujin(doujin)
                );
        }

        [Command("search")]
        [Summary("Searches for doujins by the title and tags across the supported sources that match the specified query.")]
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

            // Interleave results from each client
            await DisplayListAsync(
                request: Context.Message,
                response: response,
                results: Extensions.Interleave(_clients.Select(c => c.Search(query))),
                interactive: _interactive
            );
        }

        public static async Task DisplayListAsync(
            IUserMessage request,
            IUserMessage response,
            IAsyncEnumerable<IDoujin> results,
            InteractiveScheduler interactive
        )
        {
            var browser = new EnumerableBrowser<IDoujin>(results.GetEnumerator());

            IDoujin doujin;
            double[] elapsed;

            // Load first item manually
            using (Extensions.Measure(out elapsed))
                if (await browser.MoveNext(CancellationToken.None))
                {
                    doujin = browser.Current;
                    await updateView();
                }
                else
                {
                    await response.ModifyAsync("**nhitomi**: No results...");
                    return;
                }

            // Create interactive
            await interactive.CreateInteractiveAsync(
                requester: request.Author,
                response: response,
                triggers: add => add(
                    ("\u25c0", loadPrevious),
                    ("\u25b6", loadNext)
                ),
                onExpire: () => { browser.Dispose(); return Task.CompletedTask; },
                allowTrash: true
            );

            // Update content as the current doujin
            Task updateView(string content = null) => response.ModifyAsync(
                content: content ?? $"**{doujin.Source.Name}**: **[{browser.Index + 1}]** Loaded __{doujin.Id}__ in {elapsed.Format()}",
                embed: MessageFormatter.EmbedDoujin(doujin)
            );

            // Load next doujin
            async Task loadNext()
            {
                await response.ModifyAsync($"**nhitomi**: **[{browser.Index + 2}]** Loading...");

                using (Extensions.Measure(out elapsed))
                    if (!await browser.MoveNext(CancellationToken.None))
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
        }
    }
}