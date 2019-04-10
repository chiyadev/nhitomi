using System;
using System.Linq;
using Amazon.DynamoDBv2.DataModel;
using nhitomi.Core;

namespace nhitomi.Database
{
    public class CollectionItemInfo
    {
        public CollectionItemInfo()
        {
        }

        public CollectionItemInfo(IDoujin doujin)
        {
            AddTime = DateTime.UtcNow;
            Source = doujin.Source.Name;
            Id = doujin.Id;
            Name = doujin.OriginalName ?? doujin.PrettyName;
            Artist = string.Join(", ", doujin.Artists.OrderBy(a => a));
        }

        [DynamoDBProperty("addTime")] public DateTime AddTime { get; set; }

        // source and id are stored as the key of items mapping (see CollectionInfo)
        [DynamoDBIgnore] public string Source { get; set; }
        [DynamoDBIgnore] public string Id { get; set; }

        [DynamoDBProperty("name")] public string Name { get; set; }
        [DynamoDBProperty("artist")] public string Artist { get; set; }

        public override string ToString() => $"[{Source}] {Name}";
    }
}