using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OneOf;
using OneOf.Types;
using Prometheus;

namespace nhitomi.Storage
{
    /// <summary>
    /// Root node in the storage implementation tree that collects some metrics.
    /// </summary>
    public class RootStorage : DefaultStorage
    {
        public RootStorage(IServiceProvider services, IOptionsMonitor<StorageOptions> options) : base(services, options.CurrentValue) { }

        static readonly Histogram _readTime = Metrics.CreateHistogram("storage_read_time_milliseconds", "Time spent on reading a file in the storage.", new HistogramConfiguration
        {
            Buckets = HistogramEx.ExponentialBuckets(1, 1000, 20)
        });

        static readonly Counter _readResults = Metrics.CreateCounter("storage_read_results", "Results of storage read operations.", new CounterConfiguration
        {
            LabelNames = new[] { "result" }
        });

        static readonly Counter _readSize = Metrics.CreateCounter("storage_read_size_bytes", "Size of storage files that were read.");

        public override async Task<OneOf<StorageFile, NotFound, Exception>> ReadAsync(string name, CancellationToken cancellationToken = default)
        {
            OneOf<StorageFile, NotFound, Exception> result;

            using (_readTime.Measure())
                result = await base.ReadAsync(name, cancellationToken);

            _readResults.Labels(result.Match(_ => "success", _ => "not found", _ => "error")).Inc();

            if (result.TryPickT0(out var file, out _))
                _readSize.Inc(file.Stream.Length);

            return result;
        }

        static readonly Histogram _writeTime = Metrics.CreateHistogram("storage_write_time_milliseconds", "Time spent on writing a file in the storage.", new HistogramConfiguration
        {
            Buckets = HistogramEx.ExponentialBuckets(1, 1000, 20)
        });

        static readonly Counter _writeResults = Metrics.CreateCounter("storage_write_results", "Results of storage write operations.", new CounterConfiguration
        {
            LabelNames = new[] { "result" }
        });

        static readonly Counter _writeSize = Metrics.CreateCounter("storage_write_size_bytes", "Size of storage files that were written.");

        public override async Task<OneOf<Success, Exception>> WriteAsync(StorageFile file, CancellationToken cancellationToken = default)
        {
            OneOf<Success, Exception> result;

            using (_writeTime.Measure())
                result = await base.WriteAsync(file, cancellationToken);

            _writeResults.Labels(result.Match(_ => "success", _ => "error")).Inc();

            try
            {
                _writeSize.Inc(file.Stream.Length);
            }
            catch (NotSupportedException)
            {
                // length may not be available
            }

            return result;
        }

        static readonly Histogram _deleteTime = Metrics.CreateHistogram("storage_delete_time_milliseconds", "Time spent on deleting files in the storage.", new HistogramConfiguration
        {
            Buckets = HistogramEx.ExponentialBuckets(1, 1000, 20)
        });

        static readonly Counter _deleteResults = Metrics.CreateCounter("storage_delete_results", "Number of files attempted deleted in the storage.");

        public override async Task DeleteAsync(string[] names, CancellationToken cancellationToken = default)
        {
            using (_deleteTime.Measure())
                await base.DeleteAsync(names, cancellationToken);

            _deleteResults.Inc(names.Length);
        }
    }
}