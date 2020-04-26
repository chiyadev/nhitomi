using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using nhitomi.Models.Queries;

namespace nhitomi.Models
{
    public class BookSuggestResult : SuggestResult
    {
        [Required]
        public List<SuggestItem> PrimaryName { get; set; }

        [Required]
        public List<SuggestItem> EnglishName { get; set; }

        [Required]
        public Dictionary<BookTag, List<SuggestItem>> Tags { get; set; }
    }
}