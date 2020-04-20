using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models
{
    /// <summary>
    /// Represents a problem during request validation.
    /// </summary>
    public class ValidationProblem
    {
        /// <summary>
        /// Field where this problem originated.
        /// </summary>
        [Required]
        public string Field { get; set; }

        /// <summary>
        /// Messages describing this problem.
        /// </summary>
        [Required]
        public string[] Messages { get; set; }
    }
}