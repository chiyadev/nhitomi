using System.Collections.Generic;
using Nest;
using nhitomi.Models;
using nhitomi.Models.Queries;

namespace nhitomi.Database
{
    public class DbBookSuggestProcessor : SuggestProcessorBase<DbBook, BookSuggestResult>
    {
        public DbBookSuggestProcessor(SuggestQuery query) : base(query) { }

        public override BookSuggestResult CreateResult(IEnumerable<ISuggestOption<DbBook>> options)
        {
            var result = new BookSuggestResult
            {
                PrimaryName = new List<SuggestItem>(),
                EnglishName = new List<SuggestItem>(),
                Tags        = new Dictionary<BookTag, List<SuggestItem>>()
            };

            foreach (var option in options)
            {
                var (type, value) = SuggestionFormatter.Parse(option.Text);

                var item = new SuggestItem
                {
                    Id    = option.Id,
                    Score = option.Score,
                    Text  = value
                };

                switch (type)
                {
                    case null:
                        break;

                    case -1:
                        result.PrimaryName.Add(item);
                        break;

                    case -2:
                        result.EnglishName.Add(item);
                        break;

                    default:
                        var tag = (BookTag) type;

                        if (!result.Tags.TryGetValue(tag, out var list))
                            result.Tags[tag] = list = new List<SuggestItem>();

                        list.Add(item);
                        break;
                }
            }

            return result;
        }
    }
}