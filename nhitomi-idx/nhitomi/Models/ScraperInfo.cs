using System.ComponentModel.DataAnnotations;
using nhitomi.Scrapers;

namespace nhitomi.Models
{
    /// <summary>
    /// Contains scraper information.
    /// </summary>
    public class ScraperInfo
    {
        /// <summary>
        /// Display name of scraper.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Unique type of scraper.
        /// </summary>
        [Required]
        public ScraperType Type { get; set; }

        /// <summary>
        /// True if this scraper is enabled.
        /// </summary>
        [Required]
        public bool Enabled { get; set; }

        /// <summary>
        /// URL of the website from which this scraper scrapes data.
        /// </summary>
        [Required]
        public string Url { get; set; }
    }
}