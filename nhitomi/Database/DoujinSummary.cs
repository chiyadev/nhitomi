using nhitomi.Core;

namespace nhitomi.Database
{
    public class DoujinSummary
    {
        public static DoujinSummary FromString(string str)
        {
            var parts = str.Split(';', 2);
            var name = parts[1];
            parts = parts[0].Split('/', 2);
            var source = parts[0];
            var id = parts[1];

            return new DoujinSummary
            {
                Source = source,
                Id = id,
                Name = name
            };
        }

        public static DoujinSummary FromDoujin(IDoujin doujin) => new DoujinSummary
        {
            Source = doujin.Source.Name,
            Id = doujin.Id,
            Name = doujin.OriginalName ?? doujin.PrettyName
        };

        public string Source { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }

        public override string ToString() => $"{Source}/{Id};{Name}";
    }
}
