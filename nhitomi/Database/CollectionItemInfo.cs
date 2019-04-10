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
            Source = doujin.Source.Name;
            Id = doujin.Id;
            Name = doujin.OriginalName ?? doujin.PrettyName;
            Artist = string.Join(", ", doujin.Artists);
        }

        public string Source { get; set; }
        public string Id { get; set; }

        public string Name { get; set; }
        public string Artist { get; set; }
    }
}
