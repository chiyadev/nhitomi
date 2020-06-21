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
        static readonly Histogram _time = Metrics.CreateHistogram("http_time_milliseconds", "Time spent on performing HTTP requests.", new HistogramConfiguration
        {
            Buckets    = HistogramEx.ExponentialBuckets(100, 10000, 10),
            LabelNames = new[] { "host", "method" }
        });

        static readonly Counter _statuses = Metrics.CreateCounter("http_responses", "Number of HTTP responses.", new CounterConfiguration
        {
            LabelNames = new[] { "status" }
        });

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response;

            using (_time.Measure())
                response = await base.SendAsync(request, cancellationToken);

            _statuses.Labels(((int) response.StatusCode).ToString()).Inc();

            return response;
        }
    }
}