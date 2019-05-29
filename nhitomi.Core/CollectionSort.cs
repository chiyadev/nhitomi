using System.Linq;

namespace nhitomi.Core
{
    public enum CollectionSort
    {
        UploadTime,
        ProcessTime,
        Identifier,
        Name,
        Artist,
        Group,
        Scanlator,
        Language,
        Parody
    }

    public static class CollectionSortExtensions
    {
        public static IQueryable<Doujin> OrderBy(this IQueryable<Doujin> queryable, CollectionSort sort, bool descend)
        {
            switch (sort)
            {
                case CollectionSort.UploadTime:
                    queryable = queryable.OrderBy(d => d.UploadTime);
                    break;
                case CollectionSort.ProcessTime:
                    queryable = queryable.OrderBy(d => d.ProcessTime);
                    break;
                case CollectionSort.Identifier:
                    queryable = queryable.OrderBy(d => d.Source).ThenBy(d => d.SourceId);
                    break;
                case CollectionSort.Name:
                    queryable = queryable.OrderBy(d => d.PrettyName).ThenBy(d => d.OriginalName);
                    break;
                case CollectionSort.Artist:
                    queryable = queryable
                        .OrderBy(d => d.Tags.Select(t => t.Tag).First(t => t.Type == TagType.Artist));
                    break;
                case CollectionSort.Group:
                    queryable = queryable
                        .OrderBy(d => d.Tags.Select(t => t.Tag).First(t => t.Type == TagType.Group));
                    break;
                case CollectionSort.Scanlator:
                    queryable = queryable
                        .OrderBy(d => d.Tags.Select(t => t.Tag).First(t => t.Type == TagType.Scanlator));
                    break;
                case CollectionSort.Language:
                    queryable = queryable
                        .OrderBy(d => d.Tags.Select(t => t.Tag).First(t => t.Type == TagType.Language));
                    break;
                case CollectionSort.Parody:
                    queryable = queryable
                        .OrderBy(d => d.Tags.Select(t => t.Tag).First(t => t.Type == TagType.Parody));
                    break;
            }

            if (descend)
                queryable = queryable.Reverse();

            return queryable;
        }
    }
}