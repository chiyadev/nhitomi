using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using nhitomi.Models.Validation;

namespace nhitomi.Models
{
    /// <summary>
    /// Represents a generic book.
    /// </summary>
    public class Book : BookBase, INanokaObject, IHasUpdatedTime
    {
        /// <summary>
        /// Book ID.
        /// </summary>
        [Required, NanokaId]
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

        /// <inheritdoc cref="BookContent.Availability"/>
        [Required]
        public double Availability => Contents.Average(c => c.Availability);

        /// <inheritdoc cref="BookContent.TotalAvailability"/>
        [Required]
        public double TotalAvailability => Contents.Average(c => c.TotalAvailability);

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
        /// </summary>
        [MinLength(3)]
        public string EnglishName { get; set; }

        /// <summary>
        /// Tags on this book.
        /// </summary>
        [Required]
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
}