using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;

namespace nhitomi.Documentation
{
    public class ApiDocumentationTest : TestBaseHttpClient
    {
        [Test]
        public async Task Specs() => Console.WriteLine(JsonConvert.SerializeObject(await GetAsync<object>("docs.json"), Formatting.Indented));

        [Test]
        public async Task Docs() => (await RequestAsync(HttpMethod.Get, "index.html", null)).Dispose();
    }
}