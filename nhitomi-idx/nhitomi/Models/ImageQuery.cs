using System;
using System.Collections.Generic;
using nhitomi.Models.Queries;

namespace nhitomi.Models
{
    public class ImageQuery : QueryBase
    {
        public RangeQuery<DateTime> CreatedTime { get; set; }
        public RangeQuery<DateTime> UpdatedTime { get; set; }
        public RangeQuery<int> Width { get; set; }
        public RangeQuery<int> Height { get; set; }
        public RangeQuery<int> NoteCount { get; set; }
        public Dictionary<ImageTag, TextQuery> Tags { get; set; }
        public FilterQuery<WebsiteSource> Source { get; set; }
        public FilterQuery<MaterialRating> Rating { get; set; }
    }
}