namespace nhitomi
{
    public class ThumbnailConfig
    {
        /// <summary>
        /// Image format.
        /// </summary>
        public ImageFormat Format { get; set; } = ImageFormat.Jpeg;

        /// <summary>
        /// Image quality in [0, 100].
        /// </summary>
        public int Quality { get; set; } = 80;

        /// <summary>
        /// Maximum image width in pixels.
        /// Image will be scaled proportionally to fit within this width.
        /// </summary>
        public int MaxWidth { get; set; } = 600;

        /// <summary>
        /// Maximum image height in pixels.
        /// Image will be scaled proportionally to fit within this height.
        /// </summary>
        public int MaxHeight { get; set; } = 600;

        /// <summary>
        /// If the image is already smaller then thumbnail size, true to scale it up.
        /// </summary>
        public bool AllowLarger { get; set; }
    }
}