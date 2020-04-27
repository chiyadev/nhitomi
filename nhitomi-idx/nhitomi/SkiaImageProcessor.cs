using System;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace nhitomi
{
    public interface IImageProcessor
    {
        ImageFormat MediaTypeToFormat(string mediaType) => mediaType?.ToLowerInvariant().Trim() switch
        {
            "image/jpeg" => ImageFormat.Jpeg,
            "image/png"  => ImageFormat.Png,
            "image/webp" => ImageFormat.WebP,

            _ => default
        };

        /// <summary>
        /// Finds the image format of the given buffer using magic numbers, and returns the format's media type.
        /// </summary>
        string GetMediaType(byte[] buffer);

        /// <summary>
        /// Gets the width and height of the given image.
        /// </summary>
        (int width, int height)? GetDimensions(byte[] buffer);

        /// <summary>
        /// Converts an image to a different format.
        /// </summary>
        byte[] Convert(byte[] buffer, int quality, ImageFormat format);

        /// <summary>
        /// Generates a thumbnail for an image.
        /// </summary>
        byte[] GenerateThumbnail(byte[] buffer, ThumbnailOptions options);
    }

    public class SkiaImageProcessor : IImageProcessor
    {
        readonly ILogger<SkiaImageProcessor> _logger;

        public SkiaImageProcessor(ILogger<SkiaImageProcessor> logger)
        {
            _logger = logger;
        }

        static readonly byte[] _jpegPrefix = { 0xFF, 0xD8, 0xFF };
        static readonly byte[] _pngPrefix = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        static readonly byte[] _webPPrefix1 = { 0x52, 0x49, 0x46, 0x46 };
        static readonly byte[] _webPPrefix2 = { 0x57, 0x45, 0x42, 0x50 };
        static readonly byte[] _gifPrefix1 = { 47, 49, 46, 38, 37, 61 };
        static readonly byte[] _gifPrefix2 = { 47, 49, 46, 38, 39, 61 };

        public string GetMediaType(byte[] buffer)
        {
            if (buffer == null)
                return null;

            static bool test(byte[] b, byte[] p, int offset = 0)
            {
                if (b.Length < p.Length + offset)
                    return false;

                for (var i = 0; i < p.Length; i++)
                {
                    if (b[offset + i] != p[i])
                        return false;
                }

                return true;
            }

            if (test(buffer, _jpegPrefix))
                return "image/jpeg";

            if (test(buffer, _pngPrefix))
                return "image/png";

            if (test(buffer, _webPPrefix1) && test(buffer, _webPPrefix2, 8))
                return "image/webp";

            if (test(buffer, _gifPrefix1) || test(buffer, _gifPrefix2))
                return "image/gif";

            return null;
        }

        public (int width, int height)? GetDimensions(byte[] buffer)
        {
            if (buffer == null)
                return null;

            try
            {
                using var image = SKImage.FromEncodedData(buffer);

                return (image.Width, image.Height);
            }
            catch (Exception e)
            {
                _logger.LogDebug(e, $"Skia exception while loading image data ({buffer.Length}).");

                return null;
            }
        }

        public byte[] Convert(byte[] buffer, int quality, ImageFormat format)
        {
            using var image = SKImage.FromEncodedData(buffer);

            if (image == null)
                throw new FormatException($"Could not load image: bin({buffer.Length})");

            return EncodeImage(image, format, quality);
        }

        public byte[] GenerateThumbnail(byte[] buffer, ThumbnailOptions options)
        {
            using var bitmap = SKBitmap.Decode(buffer);

            if (bitmap == null)
                throw new FormatException($"Could not load image: bin({buffer.Length})");

            // scale image proportionally to fit configured size
            var scale = Math.Min(
                (double) options.MaxWidth / bitmap.Width,
                (double) options.MaxHeight / bitmap.Height);

            // don't make it larger than it was
            if (scale >= 1 && !options.AllowLarger)
                return buffer;

            var width  = (int) Math.Ceiling(bitmap.Width * scale);
            var height = (int) Math.Ceiling(bitmap.Height * scale);

            // resize and encode
            using var resized = bitmap.Resize(new SKImageInfo(width, height), SKFilterQuality.High);
            using var image   = SKImage.FromBitmap(resized);

            return EncodeImage(image, options.Format, options.Quality);
        }

        static byte[] EncodeImage(SKImage image, ImageFormat format, int quality)
        {
            var data = image.Encode(ConvertFormat(format), quality);

            if (data == null)
                throw new FormatException($"Could not convert image {image.UniqueId} {image.Width}x{image.Height} ({image.ColorType}) to format {format}.");

            return data.ToArray();
        }

        public static SKEncodedImageFormat ConvertFormat(ImageFormat format) => format switch
        {
            ImageFormat.Jpeg => SKEncodedImageFormat.Jpeg,
            ImageFormat.Png  => SKEncodedImageFormat.Png,
            ImageFormat.WebP => SKEncodedImageFormat.Webp,

            _ => throw new ArgumentException($"'{format}' is not a valid {nameof(ImageFormat)}.")
        };
    }
}