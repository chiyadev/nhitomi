using System;

namespace nhitomi.Discord
{
    public class InteractiveOptions
    {
        /// <summary>
        /// Maximum number of interactive messages to keep in memory.
        /// </summary>
        public int MaxMessages { get; set; } = 512;

        /// <summary>
        /// Time after which an interactive message will be expired.
        /// </summary>
        public TimeSpan Expiry { get; set; } = TimeSpan.FromHours(6);

        /// <summary>
        /// True to allow once expired interactive messages to be resurrected.
        /// This is only supported on stateless interactive messages.
        /// </summary>
        public bool AllowResurrection { get; set; } = true;

        /// <summary>
        /// Interval of interactive rerenders.
        /// </summary>
        public TimeSpan RenderInterval { get; set; } = TimeSpan.FromSeconds(1);
    }
}