using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SkiaSharp;

namespace nhitomi
{
    /// <summary>
    /// <see cref="SkiaImageProcessor"/>
    /// </summary>
    [TestFixture(typeof(SkiaImageProcessor))]
    public class ImageProcessorTest<T> : TestBaseServices where T : IImageProcessor
    {
        [Test]
        public void Convert([Values] ImageFormat from, [Values] ImageFormat to)
        {
            var processor = ActivatorUtilities.CreateInstance<T>(Services);

            using var fromImage = SKImage.Create(new SKImageInfo(1000, 1000));
            using var fromData  = fromImage.Encode(SkiaImageProcessor.ConvertFormat(from), 1);

            using var toStream = processor.Convert(fromData.ToArray(), 1, to);
            using var toImage  = SKImage.FromEncodedData(toStream);

            Assert.That(fromImage.Width, Is.EqualTo(toImage.Width));
            Assert.That(fromImage.Height, Is.EqualTo(toImage.Height));
        }

        [Test]
        public void FormatDetect([Values] ImageFormat format)
        {
            var processor = ActivatorUtilities.CreateInstance<T>(Services);

            var buffer = TestUtils.DummyImage(1000, 1000, format);

            Assert.That(processor.GetFormat(buffer), Is.EqualTo(format));
        }

        [Test]
        public void Dimensions([Values] ImageFormat format)
        {
            var processor = ActivatorUtilities.CreateInstance<T>(Services);

            var buffer = TestUtils.DummyImage(123, 234, format);

            Assert.That(processor.GetDimensions(buffer), Is.EqualTo((123, 234)));
        }
    }
}