using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace nhitomi.Documentation
{
    public class ApiDocumentationTest : TestBaseHttpClient
    {
        [Test]
        public Task Specs() => GetAsync<JObject>("docs/v1.json");

        [Test]
        public async Task Docs() => (await RequestAsync(HttpMethod.Get, "docs/index.html", null)).Dispose();
    }
}