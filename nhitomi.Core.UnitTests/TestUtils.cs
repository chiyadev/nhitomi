using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;

namespace nhitomi.Core.UnitTests
{
    public static class TestUtils
    {
        public static ILogger<T> Logger<T>() => new NullLogger<T>();

        public static JsonSerializer Serializer => JsonSerializer.Create(new nhitomiSerializerSettings());
    }
}