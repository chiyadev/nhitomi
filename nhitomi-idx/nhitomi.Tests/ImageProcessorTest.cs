using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using SkiaSharp;

namespace nhitomi.Tests
{
    public class ImageProcessorTest
    {
        [Test]
        public void Convert([Values] ImageFormat from, [Values] ImageFormat to)
        {
            var processor = new ImageProcessor(NullLogger<ImageProcessor>.Instance);

            using var fromImage = SKImage.Create(new SKImageInfo(1000, 1000));
            using var fromData  = fromImage.Encode(ImageProcessor.ConvertFormat(from), 1);

            using var toStream = processor.Convert(fromData.ToArray(), 1, to);
            using var toImage  = SKImage.FromEncodedData(toStream);

            Assert.That(fromImage.Width, Is.EqualTo(toImage.Width));
            Assert.That(fromImage.Height, Is.EqualTo(toImage.Height));
        }

        [Test]
        public void FormatDetect([Values] ImageFormat format)
        {
            var processor = new ImageProcessor(NullLogger<ImageProcessor>.Instance);

            var buffer = TestUtils.DummyImage(1000, 1000, format);

            Assert.That(processor.GetFormat(buffer), Is.EqualTo(format));
        }

        [Test]
        public void Dimensions([Values] ImageFormat format)
        {
            var processor = new ImageProcessor(NullLogger<ImageProcessor>.Instance);

            var buffer = TestUtils.DummyImage(123, 234, format);

            Assert.That(processor.GetDimensions(buffer), Is.EqualTo((123, 234)));
        }
    }
}