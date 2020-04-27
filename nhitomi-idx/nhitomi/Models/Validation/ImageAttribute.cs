using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;

namespace nhitomi.Models.Validation
{
    public class ImageAttribute : ValidationAttribute
    {
        public override bool RequiresValidationContext => true;

        ImageFormat? _format;

        /// <summary>
        /// Required format of image.
        /// </summary>
        public ImageFormat Format
        {
            get => _format ?? default;
            set => _format = value;
        }

        public bool IsFormatSpecified => _format != null;

        /// <summary>
        /// Minimum image size in bytes.
        /// </summary>
        public int MinLength { get; set; }

        /// <summary>
        /// Maximum image size in bytes.
        /// </summary>
        public int MaxLength { get; set; } = int.MaxValue;

        public int MinWidth { get; set; }
        public int MaxWidth { get; set; } = int.MaxValue;

        public int MinHeight { get; set; }
        public int MaxHeight { get; set; } = int.MaxValue;

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return null;

            if (!(value is byte[] buffer))
                throw new ArgumentException("Value is not byte[].");

            if (buffer.Length < MinLength || buffer.Length > MaxLength)
                return new ValidationResult($"Image size must be between [{MinLength}, {MaxLength}].");

            var processor = validationContext.GetService<IImageProcessor>();

            var mediaType = processor.GetMediaType(buffer);

            if (mediaType == null)
                return new ValidationResult("Unknown image format.");

            if (IsFormatSpecified && processor.MediaTypeToFormat(mediaType) != Format)
                return new ValidationResult($"Image format '{mediaType}' is not supported. Image must be {Format}.");

            var dimensions = processor.GetDimensions(buffer);

            if (dimensions == null)
                return new ValidationResult("Image content is corrupted.");

            var (width, height) = dimensions.Value;

            if (width < MinWidth || width > MaxWidth)
                return new ValidationResult($"Image width must be between [{MinWidth}, {MaxWidth}].");

            if (height < MinHeight || height > MaxHeight)
                return new ValidationResult($"Image width must be between [{MinHeight}, {MaxHeight}].");

            return ValidationResult.Success;
        }
    }
}