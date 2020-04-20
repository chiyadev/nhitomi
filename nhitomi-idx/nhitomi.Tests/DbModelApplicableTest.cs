using System.Collections.Generic;
using Force.DeepCloner;
using nhitomi.Database;
using nhitomi.Models;
using NUnit.Framework;

namespace nhitomi.Tests
{
    [Parallelizable(ParallelScope.All)]
    public class DbModelApplicableTest
    {
        static readonly DbBook _book = new DbBook
        {
            PrimaryName = "test1",
            TagsArtist  = new[] { "artist222" },
            EnglishName = "test2",
            Category    = default
        };

        [Test]
        public void ShouldUpdate()
        {
            var book = _book.DeepClone();

            var bookBase = new BookBase
            {
                Tags = new Dictionary<BookTag, string[]>
                {
                    [BookTag.Artist] = new[] { "artist222", "artist 333" }
                }
            };

            Assert.That(book.TryApplyBase(bookBase), Is.True);

            Assert.That(book.PrimaryName, Is.Null);
            Assert.That(book.EnglishName, Is.Null);
            Assert.That(book.TagsArtist, Has.Exactly(2).Items);
            Assert.That(book.Category, Is.EqualTo(default(BookCategory)));
        }

        [Test]
        public void ShouldNotUpdate()
        {
            var book = _book.DeepClone();

            var bookBase = new BookBase
            {
                PrimaryName = "test1",
                Tags = new Dictionary<BookTag, string[]>
                {
                    [BookTag.Artist] = new[] { "artist222" }
                },
                EnglishName = "test2"
            };

            Assert.That(book.TryApplyBase(bookBase), Is.False);
        }
    }
}