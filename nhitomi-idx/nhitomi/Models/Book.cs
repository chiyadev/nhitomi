using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using nhitomi.Models.Validation;

namespace nhitomi.Models
{
    /// <summary>
    /// Represents a generic book.
    /// </summary>
    public class Book : BookBase, IHasId, IHasUpdatedTime
    {
        /// <summary>
        /// Book ID.
        /// </summary>
        [Required, nhitomiId]
        public string Id { get; set; }

        /// <summary>
        /// Time when this book was created.
        /// </summary>
        [Required]
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// Time when this book was updated.
        /// </summary>
        [Required]
        public DateTime UpdatedTime { get; set; }

        /// <summary>
        /// Contents in this book.
        /// </summary>
        [Required]
        public BookContent[] Contents { get; set; }
    }

    public class BookBase
    {
        /// <summary>
        /// Fully localized name of this book, which is usually in Japanese.
        /// </summary>
        [Required, MinLength(3)]
        public string PrimaryName { get; set; }

        /// <summary>
        /// Name of this book translated to English.
        /// This should be in plain comprehensible English, not a direct romanization of primary name.
        /// </summary>
        [MinLength(3)]
        public string EnglishName { get; set; }

        /// <summary>
        /// Tags on this book.
        /// </summary>
        [Required, SanitizedTags]
        public Dictionary<BookTag, string[]> Tags { get; set; } = new Dictionary<BookTag, string[]>();

        /// <summary>
        /// Book category.
        /// </summary>
        [Required]
        public BookCategory Category { get; set; }

        /// <summary>
        /// Material rating.
        /// </summary>
        [Required]
        public MaterialRating Rating { get; set; }
    }

    public enum BookTag
    {
        /// <summary>
        /// Tag is an artist.
        /// </summary>
        Artist = 0,

        /// <summary>
        /// Tag is a parody.
        /// </summary>
        /// <remarks>
        /// Not to be confused with series.
        /// </remarks>
        Parody = 1,

        /// <summary>
        /// Tag is a character.
        /// </summary>
        Character = 2,

        /// <summary>
        /// Tag is a convention.
        /// </summary>
        Convention = 3,

        /// <summary>
        /// Tag is a series.
        /// </summary>
        /// <remarks>
        /// Not to be confused with parody.
        /// </remarks>
        Series = 4,

        /// <summary>
        /// Tag is a circle.
        /// </summary>
        Circle = 5,

        /// <summary>
        /// Tag is metadata.
        /// </summary>
        Metadata = 6,

        /// <summary>
        /// Tag is generic.
        /// </summary>
        Tag = 7
    }

    public enum BookCategory
    {
        /// <summary>
        /// Book is a doujinshi.
        /// </summary>
        Doujinshi = 0,

        /// <summary>
        /// Book is a manga.
        /// </summary>
        Manga = 1,

        /// <summary>
        /// Book is a set of artist CG.
        /// </summary>
        ArtistCg = 2,

        /// <summary>
        /// Book is a set of game CG.
        /// </summary>
        GameCg = 3,

        /// <summary>
        /// Book is a light novel scan.
        /// </summary>
        LightNovel = 5
    }
}