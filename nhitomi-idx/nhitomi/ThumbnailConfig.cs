using System;

namespace nhitomi
{
    public class ThumbnailConfig
    {
        /// <summary>
        /// Format of images.
        /// </summary>
        public ImageFormat Format { get; set; } = ImageFormat.WebP;

        /// <summary>
        /// Quality of images.
        /// </summary>
        public int Quality { get; set; } = 80;

        /// <summary>
        /// Maximum width of images.
        /// Images will be scaled proportionally to fit within this width.
        /// </summary>
        public int MaxWidth { get; set; } = 800;

        /// <summary>
        /// Maximum height of images.
        /// Images will be scaled proportionally to fit within this height.
        /// </summary>
        public int MaxHeight { get; set; } = 800;

        /// <summary>
        /// Cache-Control max-age which controls how long clients should cache images.
        /// </summary>
        public TimeSpan CacheControl { get; set; } = TimeSpan.FromDays(30);
    }
}