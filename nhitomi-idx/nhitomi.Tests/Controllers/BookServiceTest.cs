using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChiyaFlake;
using Microsoft.Extensions.DependencyInjection;
using nhitomi.Database;
using nhitomi.Models;
using nhitomi.Models.Queries;
using nhitomi.Scrapers;
using NUnit.Framework;
using OneOf;
using OneOf.Types;

namespace nhitomi.Controllers
{
    /// <summary>
    /// <see cref="BookService"/>
    /// </summary>
    public class BookServiceTest : TestBaseServices
    {
        public async Task<DbBook> CreateAsync(BookBase book, BookContentBase content, BookImage[] pages)
        {
            var client = Services.GetService<IElasticClient>();

            var entry = client.Entry(new DbBook
            {
                Contents = new[]
                {
                    new DbBookContent
                    {
                        Pages = pages.ToArray(p => new DbBookImage().Apply(p))
                    }.ApplyBase(content)
                }
            }.ApplyBase(book));

            return await entry.CreateAsync();
        }

        public async Task<OneOf<(DbBook, DbBookContent), NotFound>> CreateContentAsync(string id, BookContentBase content, BookImage[] pages)
        {
            var client = Services.GetService<IElasticClient>();

            var cont = new DbBookContent
            {
                Id    = Snowflake.New,
                Pages = pages.ToArray(p => new DbBookImage().Apply(p))
            }.ApplyBase(content);

            var entry = await client.GetEntryAsync<DbBook>(id);

            do
            {
                if (entry.Value == null)
                    return new NotFound();

                entry.Value.Contents = entry.Value.Contents.Append(cont).ToArray();
            }
            while (!await entry.TryUpdateAsync());

            return (entry.Value, cont);
        }

        [Test]
        public async Task CreateAsync()
        {
            var books = Services.GetService<IBookService>();

            // create book
            var createBookResult = await CreateAsync(new BookBase
            {
                PrimaryName = "my book",
                EnglishName = "name 2",
                Tags = new Dictionary<BookTag, string[]>
                {
                    [BookTag.Artist] = new[] { "artist", "artist 2" },
                    [BookTag.Tag]    = new[] { "tag" }
                }
            }, new BookContentBase
            {
                Language = LanguageType.English
            }, new[]
            {
                new BookImage
                {
                    Notes = new ImageNote[0]
                },
                new BookImage
                {
                    Notes = new[]
                    {
                        new ImageNote
                        {
                            Content = "note content"
                        }
                    }
                }
            });

            var book = createBookResult.Convert();

            Assert.That(book, Is.Not.Null);
            Assert.That(book.PrimaryName, Is.EqualTo("my book"));
            Assert.That(book.EnglishName, Is.EqualTo("name 2"));
            Assert.That(book.Tags, Is.Not.Null.Or.Empty);
            Assert.That(book.Tags[BookTag.Artist], Has.Exactly(2).Items);
            Assert.That(book.Tags[BookTag.Artist][1], Is.EqualTo("artist 2"));
            Assert.That(book.Contents, Has.One.Items);
            Assert.That(book.Contents[0].Source, Is.EqualTo(ScraperType.Unknown));
            Assert.That(book.Contents[0].Language, Is.EqualTo(LanguageType.English));
            Assert.That(book.Contents[0].Pages, Has.Exactly(2).Items);
            Assert.That(book.Contents[0].Pages[1].Notes, Has.Exactly(1).Items);
            Assert.That(book.Contents[0].Pages[1].Notes[0].Content, Is.EqualTo("note content"));

            var getBookResult = await books.GetAsync(book.Id);

            Assert.That(getBookResult.AsT0.Id, Is.EqualTo(book.Id));

            var getContentResult = await books.GetContentAsync(book.Id, book.Contents[0].Id);

            Assert.That(getContentResult.AsT0.Item1.Id, Is.EqualTo(book.Id));
            Assert.That(getContentResult.AsT0.Item2.Id, Is.EqualTo(book.Contents[0].Id));

            // create another content
            var contentResult = await CreateContentAsync(book.Id, new BookContentBase
            {
                Language = LanguageType.Chinese
            }, new[]
            {
                new BookImage
                {
                    Notes = new[]
                    {
                        new ImageNote
                        {
                            Content = "second note"
                        }
                    }
                }
            });

            Assert.That(contentResult.AsT0.Item1.Id, Is.EqualTo(book.Id));

            var secondContent = contentResult.AsT0.Item2.Convert();

            Assert.That(secondContent.Language, Is.EqualTo(LanguageType.Chinese));
            Assert.That(secondContent.Pages, Has.Exactly(1).Items);

            getBookResult = await books.GetAsync(book.Id);

            Assert.That(getBookResult.AsT0.Id, Is.EqualTo(book.Id));

            var secondBook = getBookResult.AsT0.Convert();

            Assert.That(secondBook.Contents, Has.Exactly(2).Items);
            Assert.That(secondBook.Contents[0].Id, Is.EqualTo(book.Contents[0].Id));
            Assert.That(secondBook.Contents[1].Id, Is.EqualTo(secondContent.Id));

            var getSecondContentResult = await books.GetContentAsync(book.Id, secondContent.Id);

            Assert.That(getSecondContentResult.AsT0.Item1.Id, Is.EqualTo(book.Id));
            Assert.That(getSecondContentResult.AsT0.Item2.Id, Is.EqualTo(secondContent.Id));
        }

        [Test]
        public async Task DeleteAsync()
        {
            var client = Services.GetService<IElasticClient>();

            var dummy = new DbBook
            {
                Contents = new[]
                {
                    new DbBookContent
                    {
                        Pages = new[]
                        {
                            new DbBookImage()
                        }
                    }
                }
            };

            var book = await client.Entry(dummy).CreateAsync();

            var books = Services.GetService<IBookService>();

            // delete book
            var deleteResult = await books.DeleteAsync(book.Id, new SnapshotArgs
            {
                Event  = SnapshotEvent.BeforeDeletion,
                Reason = "this book sucks"
            });

            Assert.That(deleteResult.IsT0, Is.True);

            // ensure deleted
            var getResult = await books.GetAsync(book.Id);

            Assert.That(getResult.IsT0, Is.False);

            // ensure snapshot
            var snapshots = Services.GetService<ISnapshotService>();

            var snapshotResult = await snapshots.SearchAsync(ObjectType.Book, new SnapshotQuery
            {
                TargetId = book.Id,
                Limit    = 1
            });

            Assert.That(snapshotResult.Total, Is.EqualTo(1));
            Assert.That(snapshotResult.Items[0].Event, Is.EqualTo(SnapshotEvent.BeforeDeletion));
            Assert.That(snapshotResult.Items[0].Reason, Is.EqualTo("this book sucks"));

            // delete nonexistent
            deleteResult = await books.DeleteAsync(book.Id, new SnapshotArgs());

            Assert.That(deleteResult.IsT0, Is.False);
        }

        [Test]
        public async Task DeleteSoleContent()
        {
            var client = Services.GetService<IElasticClient>();

            var dummy = new DbBook
            {
                Contents = new[]
                {
                    new DbBookContent
                    {
                        Pages = new[]
                        {
                            new DbBookImage()
                        }
                    }
                }
            };

            var book = await client.Entry(dummy).CreateAsync();

            var books = Services.GetService<IBookService>();

            // delete content
            var deleteResult = await books.DeleteContentAsync(book.Id, book.Contents[0].Id, new SnapshotArgs
            {
                Event  = SnapshotEvent.BeforeDeletion,
                Reason = "this content sucks"
            });

            Assert.That(deleteResult.IsT1, Is.True);

            // ensure snapshot
            var snapshots = Services.GetService<ISnapshotService>();

            var snapshotResult = await snapshots.SearchAsync(ObjectType.Book, new SnapshotQuery
            {
                TargetId = book.Id,
                Limit    = 1
            });

            Assert.That(snapshotResult.Total, Is.EqualTo(1));
            Assert.That(snapshotResult.Items[0].Event, Is.EqualTo(SnapshotEvent.BeforeDeletion));
            Assert.That(snapshotResult.Items[0].Reason, Is.EqualTo("this content sucks"));

            // deleting the only content in the book should delete the entire book
            var getResult = await books.GetAsync(book.Id);

            Assert.That(getResult.IsT0, Is.False);
        }

        [Test]
        public async Task DeleteAdditionalContent()
        {
            var client = Services.GetService<IElasticClient>();

            var dummy = new DbBook
            {
                Contents = new[]
                {
                    new DbBookContent
                    {
                        Pages = new[]
                        {
                            new DbBookImage()
                        }
                    },
                    new DbBookContent
                    {
                        Pages = new[]
                        {
                            new DbBookImage()
                        }
                    }
                }
            };

            var book = await client.Entry(dummy).CreateAsync();

            var books = Services.GetService<IBookService>();

            // delete first content
            var deleteResult = await books.DeleteContentAsync(book.Id, book.Contents[0].Id, new SnapshotArgs
            {
                Event  = SnapshotEvent.BeforeDeletion,
                Reason = "this content sucks"
            });

            Assert.That(deleteResult.AsT0.Id, Is.EqualTo(book.Id));

            // ensure snapshot
            var snapshots = Services.GetService<ISnapshotService>();

            var snapshotResult = await snapshots.SearchAsync(ObjectType.Book, new SnapshotQuery
            {
                TargetId = book.Id,
                Limit    = 1
            });

            Assert.That(snapshotResult.Total, Is.EqualTo(1));
            Assert.That(snapshotResult.Items[0].Event, Is.EqualTo(SnapshotEvent.BeforeDeletion));
            Assert.That(snapshotResult.Items[0].Reason, Is.EqualTo("this content sucks"));

            // book should not be deleted if there is still content remaining
            var getResult = await books.GetAsync(book.Id);

            Assert.That(getResult.AsT0.Contents, Has.Exactly(1).Items);
            Assert.That(getResult.AsT0.Contents[0].Id, Is.EqualTo(book.Contents[1].Id)); // second content should be remaining
        }

        [Test]
        public async Task SuggestNames()
        {
            var books = Services.GetService<IBookService>();

            await CreateAsync(new BookBase
            {
                PrimaryName = "test primary name",
                EnglishName = "test english name"
            }, new BookContentBase(), new BookImage[0]);

            await CreateAsync(new BookBase
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

            await CreateAsync(new BookBase
            {
                Tags = new Dictionary<BookTag, string[]>
                {
                    [BookTag.Artist]     = new[] { "artist" },
                    [BookTag.Convention] = new[] { "c97" }
                }
            }, new BookContentBase(), new BookImage[0]);

            await CreateAsync(new BookBase
            {
                Tags = new Dictionary<BookTag, string[]>
                {
                    [BookTag.Artist]     = new[] { "art" },
                    [BookTag.Convention] = new[] { "c98" },
                    [BookTag.Parody]     = new[] { "book parody" }
                }
            }, new BookContentBase(), new BookImage[0]);

            await CreateAsync(new BookBase
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
        public async Task SuggestNoDuplicate()
        {
            var books = Services.GetService<IBookService>();

            await CreateAsync(new BookBase
            {
                PrimaryName = "test"
            }, new BookContentBase(), new BookImage[0]);

            await CreateAsync(new BookBase
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