using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using nhitomi.Models;
using nhitomi.Models.Queries;
using NUnit.Framework;

namespace nhitomi.Controllers
{
    public class BookSuggestTest : TestBaseServices
    {
        [Test]
        public async Task SuggestNames()
        {
            var books = Services.GetService<IBookService>();

            await books.CreateAsync(new BookBase
            {
                PrimaryName = "test primary name",
                EnglishName = "test english name"
            }, new BookContentBase(), new BookImage[0]);

            await books.CreateAsync(new BookBase
            {
                PrimaryName = "test primary 2",
                EnglishName = "another english name"
            }, new BookContentBase(), new BookImage[0]);

            var result = await books.SuggestAsync(new SuggestQuery
            {
                Limit  = 0,
                Prefix = "test"
            });

            Assert.That(result.PrimaryName, Has.Exactly(0).Items);
            Assert.That(result.EnglishName, Has.Exactly(0).Items);

            result = await books.SuggestAsync(new SuggestQuery
            {
                Limit  = 1,
                Prefix = "nothing"
            });

            Assert.That(result.PrimaryName, Has.Exactly(0).Items);
            Assert.That(result.EnglishName, Has.Exactly(0).Items);

            result = await books.SuggestAsync(new SuggestQuery
            {
                Limit  = 10,
                Prefix = "test primary"
            });

            Assert.That(result.PrimaryName.Select(x => x.Text), Is.EquivalentTo(new[] { "test primary name", "test primary 2" }));
            Assert.That(result.EnglishName, Has.Exactly(0).Items);

            result = await books.SuggestAsync(new SuggestQuery
            {
                Limit  = 10,
                Prefix = "another"
            });

            Assert.That(result.PrimaryName, Has.Exactly(0).Items);
            Assert.That(result.EnglishName.Select(x => x.Text), Is.EquivalentTo(new[] { "another english name" }));
        }

        [Test]
        public async Task SuggestTags()
        {
            var books = Services.GetService<IBookService>();

            await books.CreateAsync(new BookBase
            {
                Tags = new Dictionary<BookTag, string[]>
                {
                    [BookTag.Artist]     = new[] { "artist" },
                    [BookTag.Convention] = new[] { "c97" }
                }
            }, new BookContentBase(), new BookImage[0]);

            await books.CreateAsync(new BookBase
            {
                Tags = new Dictionary<BookTag, string[]>
                {
                    [BookTag.Artist]     = new[] { "art" },
                    [BookTag.Convention] = new[] { "c98" },
                    [BookTag.Parody]     = new[] { "book parody" }
                }
            }, new BookContentBase(), new BookImage[0]);

            await books.CreateAsync(new BookBase
            {
                Tags = new Dictionary<BookTag, string[]>
                {
                    [BookTag.Circle] = new[] { "circles" },
                    [BookTag.Series] = new[] { "book series" }
                }
            }, new BookContentBase(), new BookImage[0]);

            var result = await books.SuggestAsync(new SuggestQuery
            {
                Limit  = 10,
                Prefix = "art"
            });

            Assert.That(result.Tags, Has.Exactly(1).Items);
            Assert.That(result.Tags[BookTag.Artist].Select(x => x.Text), Is.EquivalentTo(new[] { "artist", "art" }));

            result = await books.SuggestAsync(new SuggestQuery
            {
                Limit  = 10,
                Prefix = "c"
            });

            Assert.That(result.Tags, Has.Exactly(2).Items);
            Assert.That(result.Tags[BookTag.Convention].Select(x => x.Text), Is.EquivalentTo(new[] { "c97", "c98" }));
            Assert.That(result.Tags[BookTag.Circle].Select(x => x.Text), Is.EquivalentTo(new[] { "circles" }));

            result = await books.SuggestAsync(new SuggestQuery
            {
                Limit  = 10,
                Prefix = "book"
            });

            Assert.That(result.Tags, Has.Exactly(2).Items);
            Assert.That(result.Tags[BookTag.Parody].Select(x => x.Text), Is.EquivalentTo(new[] { "book parody" }));
            Assert.That(result.Tags[BookTag.Series].Select(x => x.Text), Is.EquivalentTo(new[] { "book series" }));
        }

        [Test]
        public async Task NoDuplicate()
        {
            var books = Services.GetService<IBookService>();

            await books.CreateAsync(new BookBase
            {
                PrimaryName = "test"
            }, new BookContentBase(), new BookImage[0]);

            await books.CreateAsync(new BookBase
            {
                PrimaryName = "test"
            }, new BookContentBase(), new BookImage[0]);

            var result = await books.SuggestAsync(new SuggestQuery
            {
                Limit  = 10,
                Prefix = "test"
            });

            Assert.That(result.PrimaryName, Has.Exactly(1).Items);
            Assert.That(result.PrimaryName[0].Text, Is.EqualTo("test"));

            Assert.That(result.EnglishName, Has.Exactly(0).Items);
        }
    }
}