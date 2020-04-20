using System;
using System.Collections.Generic;
using nhitomi.Models.Queries;

namespace nhitomi.Models
{
    public class BookQuery : QueryBase<BookSort>
    {
        public RangeQuery<DateTime> CreatedTime { get; set; }
        public RangeQuery<DateTime> UpdatedTime { get; set; }
        public TextQuery PrimaryName { get; set; }
        public TextQuery EnglishName { get; set; }
        public Dictionary<BookTag, TextQuery> Tags { get; set; }
        public FilterQuery<BookCategory> Category { get; set; }
        public FilterQuery<MaterialRating> Rating { get; set; }
        public RangeQuery<int> PageCount { get; set; }
        public FilterQuery<LanguageType> Language { get; set; }
        public FilterQuery<WebsiteSource> Sources { get; set; }
        public RangeQuery<int> Size { get; set; }
        public RangeQuery<double> Availability { get; set; }
        public RangeQuery<double> TotalAvailability { get; set; }
    }
}