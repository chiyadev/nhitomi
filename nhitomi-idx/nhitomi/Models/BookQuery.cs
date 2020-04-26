using System;
using System.Collections.Generic;
using nhitomi.Models.Queries;
using nhitomi.Scrapers;

namespace nhitomi.Models
{
    public class BookQuery : QueryBase<BookSort>
    {
        public RangeQuery<DateTime> CreatedTime { get; set; }
        public RangeQuery<DateTime> UpdatedTime { get; set; }
        public RangeQuery<int> PageCount { get; set; }
        public RangeQuery<int> NoteCount { get; set; }
        public TextQuery PrimaryName { get; set; }
        public TextQuery EnglishName { get; set; }
        public Dictionary<BookTag, FilterQuery<string>> Tags { get; set; }
        public RangeQuery<int> TagCount { get; set; }
        public FilterQuery<BookCategory> Category { get; set; }
        public FilterQuery<LanguageType> Language { get; set; }
        public FilterQuery<MaterialRating> Rating { get; set; }
        public FilterQuery<ScraperType> Sources { get; set; }
    }

    public enum BookSort
    {
        /// <summary>
        /// Sort by relevance.
        /// </summary>
        Relevance = 0,

        /// <summary>
        /// Sort by created time.
        /// </summary>
        CreatedTime = 1,

        /// <summary>
        /// Sort by updated time.
        /// </summary>
        UpdatedTime = 2,

        /// <summary>
        /// Sort by page count.
        /// </summary>
        PageCount = 3,

        /// <summary>
        /// Sort by note count.
        /// </summary>
        NoteCount = 4,

        /// <summary>
        /// Sort by tag count.
        /// </summary>
        TagCount = 5
    }
}