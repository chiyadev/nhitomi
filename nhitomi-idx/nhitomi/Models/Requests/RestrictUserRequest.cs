using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models.Requests
{
    public class RestrictUserRequest
    {
        /// <summary>
        /// Duration of the user restriction in minutes.
        /// </summary>
        /// <remarks>
        /// If specified, duration must be at least 10 minutes.
        /// </remarks>
        [Range(10, int.MaxValue)]
        public double? DurationMins { get; set; } = 60;
    }
}