using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nhitomi.Controllers;
using nhitomi.Database;
using nhitomi.Models;
using NUnit.Framework;

namespace nhitomi.Scrapers
{
    public class BookScraperTest : TestBaseServices
    {
        sealed class Options : ScraperOptions { }

        sealed class Scraper : BookScraperBase
        {
            public override string Name => null;
            public override ScraperType Type => ScraperType.nhentai;
            public override string Url => null;

            public Scraper(IServiceProvider services, IOptionsMonitor<Options> options, ILogger<Scraper> logger) : base(services, options, logger) { }

            sealed class DummyAdaptor : BookAdaptor
            {
                public override BookBase Book { get; }
                public override IEnumerable<ContentAdaptor> Contents { get; }

                public DummyAdaptor(BookBase book, BookContentBase content)
                {
                    Book     = book;
                    Contents = new[] { new Nested(content) };
                }

                sealed class Nested : ContentAdaptor
                {
                    public override string Id => null;
                    public override string Data => null;
                    public override int Pages => 0;
                    public override BookContentBase Content { get; }

                    public Nested(BookContentBase content)
                    {
                        Content = content;
                    }
                }
            }

            // refer to BookScraperBase on the criteria for merging books
            BookAdaptor[] _books =
            {
                // unique book 1
                new DummyAdaptor(new BookBase
                {
                    PrimaryName = "my doujinshi 1",
                    EnglishName = "english doujinshi 1",
                    Tags = new Dictionary<BookTag, string[]>
                    {
                        [BookTag.Artist]     = new[] { "artist 1" },
                        [BookTag.Circle]     = new[] { "circle 1" },
                        [BookTag.Character]  = new[] { "nana", "nono" },
                        [BookTag.Convention] = new[] { "c100" }
                    }
                }, new BookContentBase
                {
                    Language = LanguageType.Japanese
                }),

                // unique book 2: mismatched volume number
                new DummyAdaptor(new BookBase
                {
                    PrimaryName = "my doujinshi 2",
                    Tags = new Dictionary<BookTag, string[]>
                    {
                        [BookTag.Artist]     = new[] { "artist 1" },
                        [BookTag.Circle]     = new[] { "circle 1" },
                        [BookTag.Character]  = new[] { "nana", "nono" },
                        [BookTag.Convention] = new[] { "c100" }
                    }
                }, new BookContentBase
                {
                    Language = LanguageType.Chinese
                }),

                // merge into 1: same circle, convention, character
                new DummyAdaptor(new BookBase
                {
                    PrimaryName = " my   doujinshi   1",
                    Tags = new Dictionary<BookTag, string[]>
                    {
                        [BookTag.Circle]     = new[] { "circle 1", "circle 2" },
                        [BookTag.Character]  = new[] { "nana", "nunu" },
                        [BookTag.Convention] = new[] { "c100", "c101" }
                    }
                }, new BookContentBase
                {
                    Language = LanguageType.English
                }),

                // merge into 1: same english name
                new DummyAdaptor(new BookBase
                {
                    EnglishName = "english doujinshi   1",
                    Tags = new Dictionary<BookTag, string[]>
                    {
                        [BookTag.Artist]     = new[] { "my artist 1", "artist 1" },
                        [BookTag.Circle]     = new[] { "unrelated circle" },
                        [BookTag.Series]     = new[] { "unrelated series" },
                        [BookTag.Character]  = new[] { "nana" },
                        [BookTag.Convention] = new[] { "c100" }
                    }
                }, new BookContentBase
                {
                    Language = LanguageType.French
                }),

                // unique book 3: mismatched artist and no circle
                new DummyAdaptor(new BookBase
                {
                    PrimaryName = "my doujinshi 1",
                    EnglishName = "english doujinshi 1",
                    Tags = new Dictionary<BookTag, string[]>
                    {
                        [BookTag.Artist]     = new[] { "some completely different artist" },
                        [BookTag.Character]  = new[] { "nana", "nono" },
                        [BookTag.Convention] = new[] { "c100" }
                    }
                }, new BookContentBase
                {
                    Language = LanguageType.Korean
                }),

                // unique book 4: attempt to match english name to primary
                new DummyAdaptor(new BookBase
                {
                    PrimaryName = "english doujinshi 1",
                    EnglishName = "my doujinshi 1",
                    Tags = new Dictionary<BookTag, string[]>
                    {
                        [BookTag.Circle]     = new[] { "circle 1" },
                        [BookTag.Character]  = new[] { "nana" },
                        [BookTag.Convention] = new[] { "c100" }
                    }
                }, new BookContentBase
                {
                    Language = LanguageType.German
                })
            };

            public override string GetExternalUrl(DbBookContent content) => null;

            protected override IAsyncEnumerable<BookAdaptor> ScrapeAsync(CancellationToken cancellationToken = default)
            {
                var books = Interlocked.Exchange(ref _books, null);

                if (books == null)
                    return AsyncEnumerable.Empty<BookAdaptor>();

                return books.ToAsyncEnumerable();
            }
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            services.Configure<Options>(o => { });
        }

        [Test]
        public async Task Merging()
        {
            var scraper = ActivatorUtilities.CreateInstance<Scraper>(Services);

            await scraper.ForceRunAsync();

            var books = Services.GetService<IBookService>();

            Assert.That(await books.CountAsync(), Is.EqualTo(4));

            var all = (await books.SearchAsync(new BookQuery { Limit = 4 })).Items.OrderBy(b => b.CreatedTime).ToArray();

            // unique book 1 (merged scrape 1, 3, 4)
            var one = all[0];

            Assert.That(one.PrimaryName, Is.EqualTo("my doujinshi 1"));
            Assert.That(one.EnglishName, Is.EqualTo("english doujinshi 1"));
            Assert.That(one.TagsArtist, Is.EquivalentTo(new[] { "artist 1", "my artist 1" }));
            Assert.That(one.TagsCircle, Is.EquivalentTo(new[] { "circle 1", "circle 2", "unrelated circle" }));
            Assert.That(one.TagsSeries, Is.EquivalentTo(new[] { "unrelated series" }));
            Assert.That(one.TagsCharacter, Is.EquivalentTo(new[] { "nana", "nono", "nunu" }));
            Assert.That(one.TagsConvention, Is.EquivalentTo(new[] { "c100", "c101" }));
            Assert.That(one.Contents, Has.Exactly(3).Items);
            Assert.That(one.Contents[0].Source, Is.EqualTo(ScraperType.nhentai));
            Assert.That(one.Contents.Select(c => c.Language), Is.EquivalentTo(new[] { LanguageType.Japanese, LanguageType.English, LanguageType.French }));

            // unique book 2 (scrape 2)
            var two = all[1];

            Assert.That(two.PrimaryName, Is.EqualTo("my doujinshi 2"));
            Assert.That(two.TagsArtist, Is.EqualTo(new[] { "artist 1" }));
            Assert.That(two.TagsCircle, Is.EqualTo(new[] { "circle 1" }));
            Assert.That(two.TagsCharacter, Is.EqualTo(new[] { "nana", "nono" }));
            Assert.That(two.TagsConvention, Is.EqualTo(new[] { "c100" }));
            Assert.That(two.Contents, Has.Exactly(1).Items);
            Assert.That(two.Contents[0].Language, Is.EqualTo(LanguageType.Chinese));

            // unique book 3 (scrape 5)
            var three = all[2];

            Assert.That(three.TagsArtist, Is.EqualTo(new[] { "some completely different artist" }));
            Assert.That(three.Contents, Has.Exactly(1).Items);
            Assert.That(three.Contents[0].Language, Is.EqualTo(LanguageType.Korean));

            // unique book 4 (scrape 6)
            var four = all[3];

            Assert.That(four.Contents, Has.Exactly(1).Items);
            Assert.That(four.Contents[0].Language, Is.EqualTo(LanguageType.German));
        }
    }
}