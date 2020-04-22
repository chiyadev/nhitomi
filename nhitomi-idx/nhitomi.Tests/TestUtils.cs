using System;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using RichardSzalay.MockHttp;
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
            using var data  = image.Encode(SkiaImageProcessor.ConvertFormat(format), 1);

            return data.ToArray();
        }

        public static IServiceCollection RemoveLogging(this IServiceCollection services)
            => services.Replace(ServiceDescriptor.Singleton<ILoggerFactory, NullLoggerFactory>())
                       .Replace(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(NullLogger<>)));

        public static IHttpClientFactory HttpClient(params Action<MockHttpMessageHandler>[] configure) => new MockHttpClientFactory(configure);

        sealed class MockHttpClientFactory : IHttpClientFactory
        {
            readonly Action<MockHttpMessageHandler>[] _configure;

            public MockHttpClientFactory(params Action<MockHttpMessageHandler>[] configure)
            {
                _configure = configure;
            }

            public HttpClient CreateClient(string name)
            {
                var handler = new MockHttpMessageHandler();

                foreach (var configure in _configure)
                    configure(handler);

                return handler.ToHttpClient();
            }
        }

        public static MockedRequest RespondJson(this MockedRequest source, object obj)
            => source.Respond("application/json", JsonConvert.SerializeObject(obj));
    }
}