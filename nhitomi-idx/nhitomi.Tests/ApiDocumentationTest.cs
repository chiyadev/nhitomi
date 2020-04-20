using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;

namespace nhitomi.Tests
{
    public class ApiDocumentationTest : TestBaseHttpClient
    {
        [Test]
        public Task Specs() => GetAsync<object>("docs/v1.json");

        [Test]
        public async Task Docs() => (await RequestAsync(HttpMethod.Get, "docs/index.html", null)).Dispose();
    }
}