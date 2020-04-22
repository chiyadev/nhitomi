using System.ComponentModel.DataAnnotations;
using System.Net;

namespace nhitomi.Models
{
    public class Result<T>
    {
        /// <summary>
        /// Whether this result represents an error.
        /// </summary>
        [Required]
        public bool Error => !(200 <= Status && Status < 300);

        /// <summary>
        /// HTTP status code.
        /// </summary>
        [Required]
        public int Status { get; }

        /// <summary>
        /// Result message.
        /// </summary>
        [Required]
        public string Message { get; }

        /// <summary>
        /// Result value.
        /// </summary>
        [Required]
        public T Value { get; }

        public Result(HttpStatusCode status, string message, T value)
        {
            Value = value;

            Status  = (int) status;
            Message = message ?? status.ToString();
        }
    }
}