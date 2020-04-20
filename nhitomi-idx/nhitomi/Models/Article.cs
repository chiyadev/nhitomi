using System.ComponentModel.DataAnnotations;
using nhitomi.Models.Validation;

namespace nhitomi.Models
{
    /// <summary>
    /// Represents an article in the wiki.
    /// </summary>
    public class Article : ArticleBase
    {
        [Required, NanokaId]
        public string Id { get; set; }
    }

    public class ArticleBase
    {
        [Required]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }
    }
}