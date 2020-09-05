using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using nhitomi.Models.Validation;
using nhitomi.Scrapers;

namespace nhitomi.Models
{
    /// <summary>
    /// Represents the contents of a book.
    /// </summary>
    public class BookContent : BookContentBase, IHasId
    {
        /// <summary>
        /// Content ID.
        /// </summary>
        [Required, nhitomiId]
        public string Id { get; set; }

        /// <summary>
        /// Number of pages in this content.
        /// </summary>
        [Required]
        public int PageCount { get; set; }

        /// <summary>
        /// Notes in this content.
        /// </summary>
        /// <remarks>
        /// Key is the index of the page to which notes are attached.
        /// </remarks>
        [Required, MaxLength(512)]
        public Dictionary<int, ImageNote[]> Notes { get; set; }

        /// <summary>
        /// Scraper used to index this content.
        /// </summary>
        [Required]
        public ScraperType Source { get; set; }

        /// <summary>
        /// URL from which this content was scraped.
        /// </summary>
        [Required]
        public string SourceUrl { get; set; }

        /// <summary>
        /// Time when this content was last refreshed from source.
        /// </summary>
        public DateTime? RefreshTime { get; set; }

        /// <summary>
        /// Whether this content is available for viewing.
        /// </summary>
        /// <remarks>
        /// Contents may become unavailable if they are deleted from the source website.
        /// </remarks>
        [Required]
        public bool IsAvailable { get; set; }
    }

    public class BookContentBase
    {
        /// <summary>
        /// Content language.
        /// </summary>
        [Required]
        public LanguageType Language { get; set; }
    }
}