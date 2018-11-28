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
        readonly MessageFormatter _formatter;
        readonly InteractiveScheduler _interactive;
        readonly ISet<IDoujinClient> _clients;

        public DoujinModule(
            MessageFormatter formatter,
            InteractiveScheduler interactive,
            ISet<IDoujinClient> clients
        )
        {
            _formatter = formatter;
            _interactive = interactive;
            _clients = clients;
        }

        [Command("get")]
        [Summary("Gets a doujin from the specified source.")]
        public async Task GetAsync(
            [Summary("The source to retrieve from.")]
            string source,
            [Remainder, Summary("Doujin identifier.")]
            string id
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
                    embed: _formatter.EmbedDoujin(doujin)
                );
        }

        [Command("search")]
        [Summary("Searches for doujins that match the specified query.")]
        public async Task SearchAsync(
            [Remainder, Summary("Search query.")]
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
            var browser = new EnumerableBrowser<IDoujin>(
                Extensions.Interleave(_clients.Select(c => c.Search(query)))
                    .GetEnumerator()
            );

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
            await _interactive.CreateInteractiveAsync(
                context: Context,
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
                embed: _formatter.EmbedDoujin(doujin)
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