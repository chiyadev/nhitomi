using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models
{
    public class SongLyrics
    {
        [Required]
        public Dictionary<double, string> Lines { get; set; }
    }
}