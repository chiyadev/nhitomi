using System.Threading.Tasks;
using Discord.Commands;
using nhitomi.Database;

namespace nhitomi.Modules
{
    [Group("collection")]
    [Alias("c")]
    public class CollectionModule : ModuleBase
    {
        readonly IDatabase _database;
        readonly MessageFormatter _formatter;

        public CollectionModule(IDatabase database, MessageFormatter formatter)
        {
            _database = database;
            _formatter = formatter;
        }

        [Command]
        public async Task ListCollectionsAsync()
        {
            using (Context.Channel.EnterTypingState())
            {
                var collectionNames = await _database.GetCollectionsAsync(Context.User.Id);
            }
        }

        [Command]
        public async Task ShowAsync(string collectionName)
        {
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