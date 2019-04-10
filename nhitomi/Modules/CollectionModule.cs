using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using nhitomi.Core;
using nhitomi.Database;

namespace nhitomi.Modules
{
    [Group("collection")]
    [Alias("c")]
    public class CollectionModule : ModuleBase
    {
        readonly IDatabase _database;
        readonly MessageFormatter _formatter;
        readonly InteractiveManager _interactive;
        readonly ISet<IDoujinClient> _clients;

        public CollectionModule(
            IDatabase database,
            MessageFormatter formatter,
            InteractiveManager interactive,
            ISet<IDoujinClient> clients)
        {
            _database = database;
            _formatter = formatter;
            _interactive = interactive;
            _clients = clients;
        }

        [Command]
        public async Task ListCollectionsAsync()
        {
            using (Context.Channel.EnterTypingState())
            {
                var collectionNames = await _database.GetCollectionsAsync(Context.User.Id);

                await ReplyAsync(embed: _formatter.CreateCollectionListEmbed(collectionNames));
            }
        }

        [Command]
        public async Task ShowAsync(string collectionName)
        {
            DoujinListInteractive interactive;

            using (Context.Channel.EnterTypingState())
            {
                var summaries = await _database.GetCollectionAsync(Context.User.Id, collectionName)
                    as IEnumerable<DoujinSummary>;

                var doujins = AsyncEnumerable.CreateEnumerable(() =>
                {
                    var enumerator = summaries.GetEnumerator();
                    IDoujin current = null;

                    return AsyncEnumerable.CreateEnumerator(
                        async token =>
                        {
                            if (!enumerator.MoveNext())
                                return false;

                            var client = _clients.FindByName(enumerator.Current.Source);
                            if (client == null)
                                return false;

                            current = await client.GetAsync(enumerator.Current.Id, token);

                            return current != null;
                        },
                        () => current,
                        enumerator.Dispose);
                });

                interactive = await _interactive.CreateDoujinListInteractiveAsync(doujins, ReplyAsync);
            }

            if (interactive != null)
                await _formatter.AddDoujinTriggersAsync(interactive.Message);
        }

        [Command]
        public async Task ModifyAsync(string collectionName, string operation, string source, string id)
        {
            switch (operation)
            {
                case "add":
                    break;

                case "remove":
                    break;
            }
        }

        [Command("list")]
        public async Task ListAsync(string collectionName)
        {
        }

        [Command("order")]
        public async Task OrderAsync(string collectionName, params int[] indices)
        {
        }

        [Command("sort")]
        public async Task SortAsync(string collectionName, string attribute)
        {
        }

        [Command("delete")]
        public async Task DeleteAsync(string collectionName)
        {
        }
    }
}