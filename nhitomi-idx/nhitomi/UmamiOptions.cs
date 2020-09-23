using System;

namespace nhitomi
{
    /// <summary>
    /// Umami information for nhitomi-web frontend.
    /// </summary>
    public class UmamiOptions
    {
        /// <summary>
        /// Umami URL.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Umami website ID.
        /// </summary>
        public Guid WebsiteId { get; set; }
    }
}