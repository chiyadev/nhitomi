using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using nhitomi.Models;
using NUnit.Framework;

namespace nhitomi
{
    public abstract class TestBaseClient : TestBaseServices
    {
        protected TestServer Server { get; private set; }

        protected virtual string RequestPathPrefix => Startup.ApiBasePath.Trim('/') + "/";

        protected override IServiceProvider SetUpServices()
        {
            var endpoint = $"http://localhost:{GetPort()}";

            Server = new TestServer(Program.CreateWebHostBuilder(null)
                                           .UseEnvironment(Environments.Development)
                                           .UseUrls(endpoint)
                                           .ConfigureServices(ConfigureServices))
            {
                BaseAddress = new Uri(endpoint)
            };

            return Server.Services;
        }

        static int GetPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);

            listener.Start();

            try
            {
                return ((IPEndPoint) listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
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

    public abstract class TestBaseHttpClient<TController> : TestBaseHttpClient
    {
        protected override string RequestPathPrefix => $"{base.RequestPathPrefix}{typeof(TController).Name}/";
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

        protected async Task<HttpResponseMessage> RequestAsync(HttpMethod method, string path, HttpContent content, Action<HttpRequestMessage> configure = null)
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

            return response;
        }

        protected async Task<T> RequestAsync<T>(HttpMethod method, string path, HttpContent content, Action<HttpRequestMessage> configure = null)
        {
            using var response = await RequestAsync(method, path, content, configure);

            var data = await response.Content.ReadAsStringAsync();

            // if expecting string, use message
            if (typeof(T) == typeof(string))
                return (T) (object) JsonConvert.DeserializeObject<Result<T>>(data).Message;

            return JsonConvert.DeserializeObject<T>(data);
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

        protected Task<T> GetAsync<T>(string path)
            => RequestAsync<T>(HttpMethod.Get, path, null);

        protected Task<T> PostAsync<T>(string path, object value)
            => RequestAsync<T>(HttpMethod.Post, path, new StringContent(JsonConvert.SerializeObject(value), Encoding.Default, "application/json"));

        protected Task<T> PutAsync<T>(string path, object value)
            => RequestAsync<T>(HttpMethod.Put, path, new StringContent(JsonConvert.SerializeObject(value), Encoding.Default, "application/json"));

        protected Task<T> DeleteAsync<T>(string path)
            => RequestAsync<T>(HttpMethod.Delete, path, null);

        /// <summary>
        /// Makes request and checks for request exceptions with the specified status.
        /// </summary>
        protected static async Task<RequestException> ThrowsStatusAsync(HttpStatusCode status, Func<Task> request)
        {
            try
            {
                await request();

                Assert.Fail("Request was expected to fail, but succeeded.");

                return null;
            }
            catch (RequestException e)
            {
                Assert.That(e.Response.StatusCode, Is.EqualTo(status), e.Message);

                return e;
            }
        }
    }
}