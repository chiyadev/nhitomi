using System;
using System.Collections.Generic;
using nhitomi.Models.Queries;

namespace nhitomi.Models
{
    public class ImageQuery : QueryBase
    {
        public RangeQuery<DateTime> CreatedTime { get; set; }
        public RangeQuery<DateTime> UpdatedTime { get; set; }
        public Dictionary<ImageTag, TextQuery> Tags { get; set; }
        public RangeQuery<int> TagCount { get; set; }
        public FilterQuery<MaterialRating> Rating { get; set; }
        public FilterQuery<WebsiteSource> Source { get; set; }
        public RangeQuery<int> SourceCount { get; set; }
        public RangeQuery<int> NoteCount { get; set; }
    }
}