using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Prometheus;

namespace nhitomi
{
    /// <summary>
    /// Implements <see cref="HttpMessageHandler"/> with metrics collection.
    /// </summary>
    public class MetricsHttpMessageHandler : DelegatingHandler
    {
        // prefix should be "http_handler" because "http" usually relates to our kestrel server

        static readonly Histogram _time = Metrics.CreateHistogram("http_handler_time", "Time spent on performing HTTP requests.", new HistogramConfiguration
        {
            Buckets    = HistogramEx.ExponentialBuckets(0.1, 30, 10),
            LabelNames = new[] { "host", "method" }
        });

        static readonly Counter _statuses = Metrics.CreateCounter("http_handler_responses", "Number of HTTP responses.", new CounterConfiguration
        {
            LabelNames = new[] { "host", "status" }
        });

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var host   = request.RequestUri.Host;
            var method = request.Method.Method;

            HttpResponseMessage response;

            using (_time.Labels(host, method).Measure())
                response = await base.SendAsync(request, cancellationToken);

            _statuses.Labels(host, ((int) response.StatusCode).ToString()).Inc();

            return response;
        }
    }
}