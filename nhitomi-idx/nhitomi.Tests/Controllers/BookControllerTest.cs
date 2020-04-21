using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using nhitomi.Models;
using nhitomi.Models.Queries;
using nhitomi.Models.Requests;
using NUnit.Framework;

namespace nhitomi.Controllers
{
    public class BookControllerTest : TestBaseServices
    {
        [Test]
        public async Task CreateAndDelete()
        {
            var controller = Services.GetService<BookController>();

            var book = (await controller.CreateAsync(new NewBookRequest
            {
                Book = new BookBase
                {
                    PrimaryName = "my book",
                    EnglishName = "name 2",
                    Tags = new Dictionary<BookTag, string[]>
                    {
                        [BookTag.Artist] = new[] { "artist", "artist 2" },
                        [BookTag.Tag]    = new[] { "tag" }
                    }
                },
                Content = new BookContentBase
                {
                    Language = LanguageType.English,
                    Sources = new[]
                    {
                        new WebsiteSource
                        {
                            Website    = "google.com",
                            Identifier = "book"
                        }
                    }
                },
                Pages = new[]
                {
                    new BookImage
                    {
                        Width  = 1,
                        Height = 2,
                        Pieces = new[]
                        {
                            new Piece
                            {
                                Size = 100,
                                Hash = new byte[] { 1, 2, 3 }
                            }
                        }
                    },
                    new BookImage
                    {
                        Width  = 3,
                        Height = 4,
                        Pieces = new[]
                        {
                            new Piece
                            {
                                Size = 200,
                                Hash = new byte[0]
                            }
                        }
                    }
                },
                ThumbnailImage = TestUtils.DummyImage()
            })).Value;

            Assert.That(book, Is.Not.Null);
            Assert.That(book.PrimaryName, Is.EqualTo("my book"));
            Assert.That(book.EnglishName, Is.EqualTo("name 2"));
            Assert.That(book.Tags, Is.Not.Null.Or.Empty);
            Assert.That(book.Tags[BookTag.Artist], Has.Exactly(2).Items);
            Assert.That(book.Tags[BookTag.Artist][1], Is.EqualTo("artist 2"));
            Assert.That(book.Contents, Has.One.Items);

            Assert.That((await controller.GetAsync(book.Id)).Value.Id, Is.EqualTo(book.Id));

            var content = (await controller.GetContentAsync(book.Id, book.Contents[0].Id)).Value;

            Assert.That(content, Is.Not.Null);
            Assert.That(content.Id, Is.EqualTo(book.Contents[0].Id));
            Assert.That(content.Pages, Has.Exactly(2).Items);
            Assert.That(content.Pages[0].Width, Is.EqualTo(1));
            Assert.That(content.Pages[0].Pieces, Has.Exactly(1).Items);
            Assert.That(content.Pages[0].Pieces[0].Size, Is.EqualTo(100));
            Assert.That(content.Pages[0].Pieces[0].Hash, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(content.Pages[1].Width, Is.EqualTo(3));
            Assert.That(content.Language, Is.EqualTo(LanguageType.English));
            Assert.That(content.Sources, Has.Exactly(1).Items);
            Assert.That(content.Sources[0].Website, Is.EqualTo("google.com"));
            Assert.That(content.Sources[0].Identifier, Is.EqualTo("book"));

            var content2 = (await controller.CreateContentAsync(book.Id, new NewBookContentRequest
            {
                Content = new BookContentBase
                {
                    Language = LanguageType.ChineseSimplified
                },
                Pages = new[]
                {
                    new BookImage
                    {
                        Width  = 5,
                        Height = 6,
                        Pieces = new[]
                        {
                            new Piece
                            {
                                Size = 300,
                                Hash = new byte[] { 255 }
                            }
                        }
                    }
                },
                ThumbnailImage = TestUtils.DummyImage()
            })).Value;

            Assert.That(content2, Is.Not.Null);
            Assert.That(content2.Id, Is.Not.Null);
            Assert.That(content2.Pages, Has.Exactly(1).Items);
            Assert.That(content2.Pages[0].Width, Is.EqualTo(5));
            Assert.That(content2.Pages[0].Pieces, Has.Exactly(1).Items);
            Assert.That(content2.Pages[0].Pieces[0].Size, Is.EqualTo(300));

            Assert.That((await controller.GetContentAsync(book.Id, content2.Id)).Value.Id, Is.EqualTo(content2.Id));

            await controller.DeleteAsync(book.Id, "cause why not");

            Assert.That((await controller.GetAsync(book.Id)).Result, Is.TypeOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task DeleteSoleContent()
        {
            var books = Services.GetService<IBookService>();

            var controller = Services.GetService<BookController>();

            var book = await books.CreateAsync(new BookBase(), new BookContentBase(), new BookImage[0]);

            await controller.DeleteContentAsync(book.Id, book.Contents[0].Id, "book should be deleted entirely when all of its contents are deleted.");

            book = await books.GetAsync(book.Id);

            Assert.That(book, Is.Null);
        }

        [Test]
        public async Task TestHistoryAsync()
        {
            var controller = Services.GetService<BookController>();

            var book = (await controller.CreateAsync(new NewBookRequest
            {
                Book = new BookBase
                {
                    PrimaryName = "name"
                },
                Content        = new BookContentBase(),
                Pages          = new BookImage[0],
                ThumbnailImage = TestUtils.DummyImage()
            })).Value;

            var snapshots = (await controller.SearchSnapshotsAsync(new SnapshotQuery
            {
                TargetId = book.Id,
                Limit    = 5,
                Sorting  = { (SnapshotSort.CreatedTime, SortDirection.Ascending) }
            })).Items;

            Assert.That(snapshots, Is.Not.Null.Or.Empty);
            Assert.That(snapshots, Has.Exactly(1).Items);
            Assert.That(snapshots[0].Type, Is.EqualTo(SnapshotType.Creation));
            Assert.That(snapshots[0].TargetId, Is.EqualTo(book.Id));
            Assert.That(snapshots[0].Target, Is.EqualTo(SnapshotTarget.Book));

            var bookId = book.Id;

            book = (await controller.UpdateAsync(book.Id, new BookBase
            {
                PrimaryName = "new name",
                EnglishName = "name 2",
                Category    = BookCategory.GameCg
            }, "update")).Value;

            Assert.That(book, Is.Not.Null);
            Assert.That(book.Id, Is.EqualTo(bookId));
            Assert.That(book.PrimaryName, Is.EqualTo("new name"));
            Assert.That(book.EnglishName, Is.EqualTo("name 2"));
            Assert.That(book.Category, Is.EqualTo(BookCategory.GameCg));

            snapshots = (await controller.SearchSnapshotsAsync(new SnapshotQuery
            {
                TargetId = book.Id,
                Limit    = 5,
                Sorting  = { (SnapshotSort.CreatedTime, SortDirection.Ascending) }
            })).Items;

            Assert.That(snapshots, Has.Exactly(2).Items);
            Assert.That(snapshots[1].Type, Is.EqualTo(SnapshotType.Modification));
            Assert.That(snapshots[1].TargetId, Is.EqualTo(book.Id));
            Assert.That(snapshots[1].Reason, Is.EqualTo("update"));

            book = (await controller.RollBackAsync(book.Id, new RollbackRequest
            {
                SnapshotId = snapshots[0].Id
            }, "rollback to create")).Value;

            Assert.That(book, Is.Not.Null);
            Assert.That(book.Id, Is.EqualTo(bookId));
            Assert.That(book.PrimaryName, Is.EqualTo("name"));
            Assert.That(book.EnglishName, Is.Null);
            Assert.That(book.Category, Is.Not.EqualTo(BookCategory.GameCg));

            snapshots = (await controller.SearchSnapshotsAsync(new SnapshotQuery
            {
                TargetId = book.Id,
                Limit    = 5,
                Sorting  = { (SnapshotSort.CreatedTime, SortDirection.Ascending) }
            })).Items;

            Assert.That(snapshots, Has.Exactly(3).Items);
            Assert.That(snapshots[2].Type, Is.EqualTo(SnapshotType.Rollback));
            Assert.That(snapshots[2].TargetId, Is.EqualTo(book.Id));
            Assert.That(snapshots[2].RollbackId, Is.EqualTo(snapshots[0].Id));
            Assert.That(snapshots[2].Reason, Is.EqualTo("rollback to create"));

            await controller.DeleteAsync(book.Id, "delete");

            Assert.That((await controller.GetAsync(book.Id)).Result, Is.TypeOf<NotFoundObjectResult>());

            snapshots = (await controller.SearchSnapshotsAsync(new SnapshotQuery
            {
                TargetId = book.Id,
                Limit    = 5,
                Sorting  = { (SnapshotSort.CreatedTime, SortDirection.Ascending) }
            })).Items;

            Assert.That(snapshots, Has.Exactly(4).Items);
            Assert.That(snapshots[3].Type, Is.EqualTo(SnapshotType.Deletion));
            Assert.That(snapshots[3].RollbackId, Is.Null);
            Assert.That(snapshots[3].TargetId, Is.EqualTo(book.Id));
            Assert.That(snapshots[3].Reason, Is.EqualTo("delete"));

            book = (await controller.RollBackAsync(book.Id, new RollbackRequest
            {
                SnapshotId = snapshots[1].Id
            }, "rollback to update")).Value;

            Assert.That(book, Is.Not.Null);
            Assert.That(book.Id, Is.EqualTo(bookId));
            Assert.That(book.PrimaryName, Is.EqualTo("new name"));
            Assert.That(book.EnglishName, Is.EqualTo("name 2"));
            Assert.That(book.Category, Is.EqualTo(BookCategory.GameCg));

            snapshots = (await controller.SearchSnapshotsAsync(new SnapshotQuery
            {
                TargetId = book.Id,
                Limit    = 5,
                Sorting  = { (SnapshotSort.CreatedTime, SortDirection.Ascending) }
            })).Items;

            Assert.That(snapshots, Has.Exactly(5).Items);

            book = (await controller.RollBackAsync(book.Id, new RollbackRequest
            {
                SnapshotId = snapshots[3].Id
            }, "rollback to delete")).Value;

            Assert.That(book, Is.Null);
            Assert.That((await controller.GetAsync(bookId)).Result, Is.TypeOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task Meta()
        {
            var controller = Services.GetService<BookController>();

            controller.User = await MakeUserAsync();

            var book = (await controller.CreateAsync(new NewBookRequest
            {
                Book           = new BookBase(),
                Content        = new BookContentBase(),
                Pages          = new BookImage[0],
                ThumbnailImage = TestUtils.DummyImage()
            })).Value;

            var meta = (await controller.GetMetaAsync(book.Id)).Value;

            Assert.That(meta.CreationSnapshot.TargetId, Is.EqualTo(book.Id));
            Assert.That(meta.Creator.Id, Is.EqualTo(controller.UserId));
            Assert.That(meta.EditSnapshot.TargetId, Is.EqualTo(book.Id));
            Assert.That(meta.Editor.Id, Is.EqualTo(controller.UserId));
            Assert.That(meta.TotalSnapshotCount, Is.EqualTo(1));
        }

        [Test]
        public async Task Snapshots()
        {
            var controller = Services.GetService<BookController>();

            var book = (await controller.CreateAsync(new NewBookRequest
            {
                Book = new BookBase
                {
                    PrimaryName = "name"
                },
                Content = new BookContentBase
                {
                    Language = LanguageType.French
                },
                Pages = new[]
                {
                    new BookImage
                    {
                        Width = 123,
                        Pieces = new[]
                        {
                            new Piece
                            {
                                Size = 321,
                                Hash = new byte[] { 3, 2, 1, 2, 3 }
                            }
                        }
                    }
                },
                ThumbnailImage = TestUtils.DummyImage()
            })).Value;

            var snapshot = (await controller.SearchSnapshotsAsync(new SnapshotQuery
            {
                TargetId = book.Id,
                Limit    = 1
            })).Items[0];

            var snapshotId = snapshot.Id;

            snapshot = (await controller.GetSnapshotAsync(snapshot.Id)).Value;

            Assert.That(snapshot.Id, Is.EqualTo(snapshotId));
            Assert.That(snapshot.Type, Is.EqualTo(SnapshotType.Creation));

            var snapshotBook = (await controller.GetSnapshotValueAsync(snapshot.Id)).Value;

            // current book and snapshot book should be exactly equal
            Assert.That(book.DeepEqualTo(snapshotBook), Is.True);

            Assert.That((await controller.GetSnapshotValueAsync("abcd")).Result, Is.TypeOf<NotFoundObjectResult>());
        }
    }
}