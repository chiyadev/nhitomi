using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace nhitomi
{
    public static class TestUtils
    {
        public static IFormFile AsFormFile(this Stream stream, string name = null)
            => new FormFile(stream, 0, stream.Length, name, name);

        public static byte[] DummyImage(int width = 1000, int height = 1000, ImageFormat format = ImageFormat.Jpeg)
        {
            using var image = SKImage.Create(new SKImageInfo(width, height));
            using var data  = image.Encode(ImageProcessor.ConvertFormat(format), 1);

            return data.ToArray();
        }

        public static IOptions<TOptions> Options<TOptions>(TOptions options = null) where TOptions : class, new() => new OptionsWrapper<TOptions>(options ?? new TOptions());

        public static IServiceCollection RemoveLogging(this IServiceCollection services)
            => services.Replace(ServiceDescriptor.Singleton<ILoggerFactory, NullLoggerFactory>())
                       .Replace(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(NullLogger<>)));
    }
}