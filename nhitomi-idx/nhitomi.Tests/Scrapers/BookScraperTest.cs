using System;
using System.Collections.Generic;
using System.IO;
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
            public override ScraperType Type => ScraperType.Unknown;
            public override string Url => null;

            public Scraper(IServiceProvider services, IOptionsMonitor<Options> options, ILogger<Scraper> logger) : base(services, options, logger) { }

            // refer to BookScraperBase on the criteria for merging books

            DbBook[] _books =
            {
                // unique book 1
                new DbBook
                {
                    PrimaryName    = "my doujinshi 1",
                    EnglishName    = "english doujinshi 1",
                    TagsArtist     = new[] { "artist 1" },
                    TagsCircle     = new[] { "circle 1" },
                    TagsCharacter  = new[] { "nana", "nono" },
                    TagsConvention = new[] { "c100" },
                    Contents = new[]
                    {
                        new DbBookContent
                        {
                            Language = LanguageType.Japanese
                        }
                    }
                },

                // unique book 2: mismatched volume number
                new DbBook
                {
                    PrimaryName    = "my doujinshi 2",
                    TagsArtist     = new[] { "artist 1" },
                    TagsCircle     = new[] { "circle 1" },
                    TagsCharacter  = new[] { "nana", "nono" },
                    TagsConvention = new[] { "c100" },
                    Contents = new[]
                    {
                        new DbBookContent
                        {
                            Language = LanguageType.Chinese
                        }
                    }
                },

                // merge into 1: same circle, convention, character
                new DbBook
                {
                    PrimaryName    = " my   doujinshi   1",
                    TagsCircle     = new[] { "circle 1", "circle 2" },
                    TagsCharacter  = new[] { "nana", "nunu" },
                    TagsConvention = new[] { "c100", "c101" },
                    Contents = new[]
                    {
                        new DbBookContent
                        {
                            Language = LanguageType.English
                        }
                    }
                },

                // merge into 1: same english name
                new DbBook
                {
                    EnglishName    = "english doujinshi   1",
                    TagsArtist     = new[] { "my artist 1", "artist 1" },
                    TagsCircle     = new[] { "unrelated circle" },
                    TagsSeries     = new[] { "unrelated series" },
                    TagsCharacter  = new[] { "nana" },
                    TagsConvention = new[] { "c100" },
                    Contents = new[]
                    {
                        new DbBookContent
                        {
                            Language = LanguageType.French
                        }
                    }
                },

                // unique book 3: mismatched artist and no circle
                new DbBook
                {
                    PrimaryName    = "my doujinshi 1",
                    EnglishName    = "english doujinshi 1",
                    TagsArtist     = new[] { "some completely different artist" },
                    TagsCharacter  = new[] { "nana", "nono" },
                    TagsConvention = new[] { "c100" },
                    Contents = new[]
                    {
                        new DbBookContent
                        {
                            Language = LanguageType.Korean
                        }
                    }
                },

                // unique book 4: attempt to match english name to primary
                new DbBook
                {
                    PrimaryName    = "english doujinshi 1",
                    EnglishName    = "my doujinshi 1",
                    TagsCircle     = new[] { "circle 1" },
                    TagsCharacter  = new[] { "nana" },
                    TagsConvention = new[] { "c100" },
                    Contents = new[]
                    {
                        new DbBookContent
                        {
                            Language = LanguageType.German
                        }
                    }
                }
            };

            protected override IAsyncEnumerable<DbBook> ScrapeAsync(CancellationToken cancellationToken = default)
            {
                var books = Interlocked.Exchange(ref _books, null);

                if (books == null)
                    return AsyncEnumerable.Empty<DbBook>();

                return books.ToAsyncEnumerable();
            }

            public override Task<Stream> GetImageAsync(DbBook book, DbBookContent content, int index, CancellationToken cancellationToken = default) => Task.FromResult<Stream>(null);
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