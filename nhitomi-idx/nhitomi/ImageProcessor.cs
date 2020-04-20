using System;
using System.IO;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace nhitomi
{
    public interface IImageProcessor
    {
        /// <summary>
        /// Gets the media type for an image format.
        /// </summary>
        string GetMediaType(ImageFormat format);

        /// <summary>
        /// Finds the image format of the given buffer using magic numbers.
        /// </summary>
        ImageFormat? GetFormat(byte[] buffer);

        /// <summary>
        /// Gets the width and height of the given image.
        /// </summary>
        (int width, int height)? GetDimensions(byte[] buffer);

        /// <summary>
        /// Converts an image to a different format.
        /// </summary>
        Stream Convert(byte[] buffer, int quality, ImageFormat format);

        /// <summary>
        /// Generates a thumbnail for an image.
        /// </summary>
        Stream GenerateThumbnail(byte[] buffer, ThumbnailConfig config);
    }

    public class ImageProcessor : IImageProcessor
    {
        readonly ILogger<ImageProcessor> _logger;

        public ImageProcessor(ILogger<ImageProcessor> logger)
        {
            _logger = logger;
        }

        public string GetMediaType(ImageFormat format) => format switch
        {
            ImageFormat.WebP => "image/webp",
            ImageFormat.Jpeg => "image/jpeg",
            ImageFormat.Png  => "image/png",

            _ => null
        };

        static readonly byte[] _webPPrefix1 = { 0x52, 0x49, 0x46, 0x46 };
        static readonly byte[] _webPPrefix2 = { 0x57, 0x45, 0x42, 0x50 };
        static readonly byte[] _jpegPrefix = { 0xFF, 0xD8, 0xFF };
        static readonly byte[] _pngPrefix = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        public ImageFormat? GetFormat(byte[] buffer)
        {
            if (buffer == null)
                return null;

            // ReSharper disable All
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
            // ReSharper enable All

            if (test(buffer, _jpegPrefix))
                return ImageFormat.Jpeg;

            if (test(buffer, _pngPrefix))
                return ImageFormat.Png;

            if (test(buffer, _webPPrefix1) && test(buffer, _webPPrefix2, 8))
                return ImageFormat.WebP;

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

        public Stream Convert(byte[] buffer, int quality, ImageFormat format)
        {
            using var image = SKImage.FromEncodedData(buffer);

            if (image == null)
                throw new FormatException($"Could not load image: bin({buffer.Length})");

            // reencode
            return EncodeImage(image, format, quality);
        }

        public Stream GenerateThumbnail(byte[] buffer, ThumbnailConfig config)
        {
            using var bitmap = SKBitmap.Decode(buffer);

            if (bitmap == null)
                throw new FormatException($"Could not load image: bin({buffer.Length})");

            // scale image proportionally to fit configured size
            var scale = Math.Min(
                (double) config.MaxWidth / bitmap.Width,
                (double) config.MaxHeight / bitmap.Height);

            // don't make it larger than it was
            if (scale >= 1)
                return new MemoryStream(buffer);

            var width  = (int) Math.Ceiling(bitmap.Width * scale);
            var height = (int) Math.Ceiling(bitmap.Height * scale);

            // resize and encode
            using var resized = bitmap.Resize(new SKImageInfo(width, height), SKFilterQuality.High);
            using var image   = SKImage.FromBitmap(resized);

            return EncodeImage(image, config.Format, config.Quality);
        }

        static Stream EncodeImage(SKImage image, ImageFormat format, int quality)
        {
            var data = image.Encode(ConvertFormat(format), quality);

            if (data == null)
                throw new FormatException($"Could not convert image {image.UniqueId} {image.Width}x{image.Height} ({image.ColorType}) to format {format}.");

            return data.AsStream(true);
        }

        public static SKEncodedImageFormat ConvertFormat(ImageFormat format) => format switch
        {
            ImageFormat.Jpeg => SKEncodedImageFormat.Jpeg,
            ImageFormat.Png  => SKEncodedImageFormat.Png,
            _                => SKEncodedImageFormat.Webp
        };
    }
}