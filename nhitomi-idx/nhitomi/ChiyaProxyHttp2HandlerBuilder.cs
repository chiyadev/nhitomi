using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace nhitomi
{
    public enum ProxyType
    {
        Sockets,
        ChiyaHttp2
    }

    public class ProxyOptions
    {
        /// <summary>
        /// Type of HTTP client to use.
        /// </summary>
        public ProxyType Type { get; set; } = ProxyType.Sockets;

        /// <summary>
        /// URL or IP address to the proxy server.
        /// </summary>
        public string ProxyUrl { get; set; }

        /// <summary>
        /// List of services that should be proxied.
        /// </summary>
        public HashSet<string> ProxiedServices = new HashSet<string> { "Scraper" };
    }

    public class ChiyaProxyHttp2HandlerBuilder : HttpMessageHandlerBuilder
    {
        readonly IServiceProvider _services;
        readonly IOptionsMonitor<ProxyOptions> _options;
        readonly ILogger<ChiyaProxyHttp2HandlerBuilder> _logger;

        public override string Name { get; set; }
        public override HttpMessageHandler PrimaryHandler { get; set; }
        public override IList<DelegatingHandler> AdditionalHandlers { get; } = new List<DelegatingHandler>();

        public ChiyaProxyHttp2HandlerBuilder(IServiceProvider services, IOptionsMonitor<ProxyOptions> options, ILogger<ChiyaProxyHttp2HandlerBuilder> logger)
        {
            _services = services;
            _options  = options;
            _logger   = logger;
        }

        public override HttpMessageHandler Build()
        {
            var handler = PrimaryHandler;

            if (handler == null)
            {
                var options = _options.CurrentValue;

                foreach (var keyword in options.ProxiedServices)
                {
                    if (Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        handler = options.Type switch
                        {
                            ProxyType.Sockets    => CreateSocketsHandler(options),
                            ProxyType.ChiyaHttp2 => CreateChiyaHttp2Handler(options, _services),

                            _ => throw new NotSupportedException($"Unsupported proxy type '{options.Type}'.")
                        };

                        break;
                    }
                }

                handler ??= new SocketsHttpHandler();
            }

            _logger.LogDebug($"Created HTTP client implementation for {Name}: {handler.GetType().Name}");

            return CreateHandlerPipeline(handler, AdditionalHandlers);
        }

        static SocketsHttpHandler CreateSocketsHandler(ProxyOptions options)
        {
            var handler = new SocketsHttpHandler();

            if (options.ProxyUrl != null)
                handler.Proxy = new WebProxy(options.ProxyUrl);

            return handler;
        }

        static ChiyaProxyHttp2Handler CreateChiyaHttp2Handler(ProxyOptions options, IServiceProvider services)
        {
            var parts = options.ProxyUrl?.Split(':', 2);
            var host  = parts?[0];
            var port  = parts?.Length == 2 ? int.Parse(parts[1]) : 80;

            if (string.IsNullOrEmpty(host))
                throw new FormatException("Must specify a proxy URL for Chiya HTTP2 proxy.");

            if (!IPAddress.TryParse(host, out var address))
                address = Dns.GetHostEntry(host).AddressList[0];

            var endPoint = new IPEndPoint(address, port);
            var handler  = new ChiyaProxyHttp2Handler(endPoint, services.GetService<ILogger<ChiyaProxyHttp2Handler>>());

            return handler;
        }
    }
}