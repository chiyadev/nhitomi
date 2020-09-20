using System;
using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models
{
    /// <summary>
    /// Represents a download session.
    /// </summary>
    /// <remarks>
    /// Download sessions can be used to retrieve resources in bulks, such as book images.
    /// Endpoints provided with a download session are exempt from rate limits but have an upper request concurrency limit instead.
    /// </remarks>
    public class DownloadSession : IHasId, IHasCreatedTime
    {
        /// <summary>
        /// Session ID.
        /// </summary>
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// Time when this session was created.
        /// </summary>
        [Required]
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// Maximum concurrency of requests.
        /// </summary>
        [Required]
        public int Concurrency { get; set; }
    }
}