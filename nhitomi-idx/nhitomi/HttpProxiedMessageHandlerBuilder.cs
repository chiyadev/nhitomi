using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace nhitomi
{
    public class HttpMessageHandlerOptions
    {
        /// <summary>
        /// URL of the proxy server.
        /// </summary>
        public string ProxyUrl { get; set; }

        /// <summary>
        /// List of services that should be proxied.
        /// Services whose name contains any of these words will be proxied.
        /// </summary>
        public HashSet<string> ProxiedServices { get; } = new HashSet<string> { "Scraper" };

        /// <summary>
        /// Whether to force HTTP instead of HTTPS for proxied requests.
        /// This is useful for proxies that do not support tunneling.
        /// </summary>
        public bool ProxyForceInsecure { get; set; } = true;

        /// <summary>
        /// Maximum number of retries for proxied requests.
        /// </summary>
        public bool ProxyHandleTransientHttpErrors { get; set; } = true;

        /// <summary>
        /// Number of retries for <see cref="ProxyHandleTransientHttpErrors"/>.
        /// </summary>
        public int ProxyRetryCount { get; set; } = 5;
    }

    public class HttpProxiedMessageHandlerBuilder : HttpMessageHandlerBuilder
    {
        readonly IServiceProvider _services;
        readonly IOptionsMonitor<HttpMessageHandlerOptions> _options;
        readonly ILogger<HttpProxiedMessageHandlerBuilder> _logger;

        public override string Name { get; set; }
        public override HttpMessageHandler PrimaryHandler { get; set; }
        public override IList<DelegatingHandler> AdditionalHandlers { get; } = new List<DelegatingHandler>();

        public HttpProxiedMessageHandlerBuilder(IServiceProvider services, IOptionsMonitor<HttpMessageHandlerOptions> options, ILogger<HttpProxiedMessageHandlerBuilder> logger)
        {
            _services = services;
            _options  = options;
            _logger   = logger;
        }

        public override HttpMessageHandler Build()
        {
            AdditionalHandlers.Add(new MetricsHttpMessageHandler());

            var handler = new SocketsHttpHandler();
            var options = _options.CurrentValue;

            foreach (var keyword in options.ProxiedServices)
            {
                if (Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    if (options.ProxyUrl != null)
                        handler.Proxy = new WebProxy(options.ProxyUrl);

                    if (options.ProxyForceInsecure)
                        AdditionalHandlers.Add(new InsecureHttpMessageHandler());

                    if (options.ProxyHandleTransientHttpErrors)
                        AdditionalHandlers.Add(new PolicyHttpMessageHandler(HttpPolicyExtensions.HandleTransientHttpError().WaitAndRetryAsync(Enumerable.Range(0, options.ProxyRetryCount).Select(x => TimeSpan.FromSeconds(Math.Pow(2, x) * 0.2)))));

                    break;
                }
            }

            return CreateHandlerPipeline(PrimaryHandler = handler, AdditionalHandlers);
        }

        sealed class InsecureHttpMessageHandler : DelegatingHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (request.RequestUri?.Scheme == "https")
                    request.RequestUri = new UriBuilder(request.RequestUri) { Scheme = "http", Port = -1 }.Uri;

                return base.SendAsync(request, cancellationToken);
            }
        }
    }
}