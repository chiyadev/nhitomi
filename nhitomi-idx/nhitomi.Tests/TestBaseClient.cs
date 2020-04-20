using System;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using nhitomi.Models;
using NUnit.Framework;

namespace nhitomi.Tests
{
    public abstract class TestBaseClient : TestBaseServices
    {
        protected TestServer Server { get; private set; }

        protected virtual string RequestPathPrefix => Startup.ApiBasePath.Trim('/') + "/";

        protected override IServiceProvider SetUpServices()
        {
            var endpoint = $"http://localhost:{Extensions.NextTcpPort()}";

            Server = new TestServer(Program.CreateWebHostBuilder(null)
                                           .UseEnvironment(Environments.Development)
                                           .UseUrls(endpoint)
                                           .ConfigureServices(ConfigureServices))
            {
                BaseAddress = new Uri(endpoint)
            };

            return Server.Services;
        }

        public override async Task TearDownAsync()
        {
            // services need to be disposed before server
            try
            {
                await base.TearDownAsync();
            }
            finally
            {
                Server.Dispose();
            }
        }
    }

    public abstract class TestBaseHttpClient : TestBaseClient
    {
        protected HttpClient Client { get; private set; }

        public override async Task SetUpAsync()
        {
            await base.SetUpAsync();

            Client = Server.CreateClient();
        }

        public override async Task TearDownAsync()
        {
            try
            {
                Client.Dispose();
            }
            finally
            {
                await base.TearDownAsync();
            }
        }

        protected async Task<HttpContent> RequestAsync(HttpMethod method, string path, HttpContent content, Action<HttpRequestMessage> configure = null)
        {
            var request = new HttpRequestMessage(method, $"{RequestPathPrefix.Trim('/')}/{path}")
            {
                Content = content
            };

            configure?.Invoke(request);

            var response = await Client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                string readContent;

                try
                {
                    readContent = await response.Content.ReadAsStringAsync();
                }
                catch
                {
                    readContent = null;
                }

                if (response.Content.Headers.ContentType?.MediaType == "application/json")
                    readContent = JsonConvert.DeserializeObject<Result<object>>(readContent)?.Message ?? readContent;

                else
                    throw new Exception($"Expected a JSON response when request fails. Raw response:\n{readContent}");

                throw new RequestException(response, readContent);
            }

            return response.Content;
        }

        [Serializable]
        public class RequestException : Exception
        {
            public HttpResponseMessage Response { get; }

            internal RequestException(HttpResponseMessage response, string readContent)
                : base($"Server returned {(int) response.StatusCode} ({response.StatusCode}) \"{response.ReasonPhrase}\": {readContent ?? "<no content>"}")
            {
                Response = response;
            }

            protected RequestException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        }

        protected async Task<T> GetAsync<T>(string path)
        {
            using var response = await RequestAsync(HttpMethod.Get, path, null);
            return JsonConvert.DeserializeObject<T>(await response.ReadAsStringAsync());
        }

        protected async Task<T> PostAsync<T>(string path, object value)
        {
            using var response = await RequestAsync(HttpMethod.Post, path, new StringContent(JsonConvert.SerializeObject(value), Encoding.Default, "application/json"));
            return JsonConvert.DeserializeObject<T>(await response.ReadAsStringAsync());
        }

        protected async Task<T> PutAsync<T>(string path, object value)
        {
            using var response = await RequestAsync(HttpMethod.Put, path, new StringContent(JsonConvert.SerializeObject(value), Encoding.Default, "application/json"));
            return JsonConvert.DeserializeObject<T>(await response.ReadAsStringAsync());
        }

        protected async Task<T> DeleteAsync<T>(string path)
        {
            using var response = await RequestAsync(HttpMethod.Delete, path, null);
            return JsonConvert.DeserializeObject<T>(await response.ReadAsStringAsync());
        }

        /// <summary>
        /// Makes request and checks for request exceptions with the specified status.
        /// </summary>
        protected static async Task ThrowsStatusAsync(HttpStatusCode status, Func<Task> request)
        {
            try
            {
                await request();

                Assert.Fail("Request succeeded.");
            }
            catch (RequestException e)
            {
                Assert.That(e.Response.StatusCode, Is.EqualTo(status), e.Message);
            }
        }
    }
}