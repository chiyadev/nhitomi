using System;
using System.Buffers;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IO;
using nhitomi.Controllers;
using nhitomi.Storage;
using Prometheus;

namespace nhitomi.Scrapers
{
    public abstract class ScraperImageResult : ActionResult
    {
        public abstract string Name { get; }

        /// <summary>
        /// Download session ID.
        /// </summary>
        public string SessionId { get; set; }

        static readonly Histogram _responseTime = Metrics.CreateHistogram("scraper_image_response_time", "Time spent on writing scraper image result to response body.", new HistogramConfiguration
        {
            Buckets    = HistogramEx.ExponentialBuckets(0.01, 30, 20),
            LabelNames = new[] { "source" }
        });

        static readonly Histogram _retrieveTime = Metrics.CreateHistogram("scraper_image_retrieve_time", "Time spent on retrieving a scraper image.", new HistogramConfiguration
        {
            Buckets    = HistogramEx.ExponentialBuckets(0.01, 30, 20),
            LabelNames = new[] { "source" }
        });

        static readonly Counter _resultSize = Metrics.CreateCounter("scraper_image_result_size_bytes", "Size of scraper image results that were downloaded.", new CounterConfiguration
        {
            LabelNames = new[] { "source" }
        });

        static readonly Counter _errors = Metrics.CreateCounter("scraper_image_errors", "Number of errors while returning a scraper image.", new CounterConfiguration
        {
            LabelNames = new[] { "source" }
        });

        const int _bufferSize = 65536;
        static readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            var cancellationToken = context.HttpContext.RequestAborted;
            var storage           = context.HttpContext.RequestServices.GetService<IStorage>();
            var downloads         = context.HttpContext.RequestServices.GetService<IDownloadService>();
            var memoryManager     = context.HttpContext.RequestServices.GetService<RecyclableMemoryStreamManager>();

            var sessionId       = SessionId;
            var resourceContext = null as IAsyncDisposable;

            if (sessionId != null)
            {
                var sessionResult = await downloads.GetResourceContextAsync(sessionId, cancellationToken);

                if (!sessionResult.TryPickT0(out resourceContext, out var sessionError))
                {
                    await sessionError.Match(
                        _ => ResultUtilities.NotFound($"Session '{sessionId}' not found."),
                        _ => ResultUtilities.BadRequest("Cannot exceed download concurrency limit.")).ExecuteResultAsync(context);

                    return;
                }
            }

            await using var __ = resourceContext;

            var result = await storage.ReadAsync(Name, cancellationToken);

            determineResult:

            var source = nameof(Storage);

            // file is already saved in storage
            if (result.TryPickT0(out var file, out var error))
            {
                try
                {
                    await using (file)
                    {
                        StorageFileResult.SetHeaders(context, file);

                        var response = context.HttpContext.Response;

                        // write to response stream
                        using (_responseTime.Labels(source).Measure())
                        {
                            await file.Stream.CopyToAsync(response.Body, cancellationToken);
                            await response.CompleteAsync();
                        }
                    }
                }
                catch
                {
                    _errors.Labels(source).Inc();
                    throw;
                }
            }

            // file is not found
            else if (error.TryPickT0(out _, out var exception))
            {
                var locker = context.HttpContext.RequestServices.GetService<IResourceLocker>();

                // prevent concurrent downloads
                await using (await locker.EnterAsync($"scraper:image:{Name}", cancellationToken))
                {
                    // file may have been saved to storage while we were awaiting lock
                    result = await storage.ReadAsync(Name, cancellationToken);

                    if (result.TryPickT0(out file, out error) || !error.TryPickT0(out _, out exception)) // found, or error
                        goto determineResult;

                    // retrieve image using scraper
                    var (scraper, fileTask) = GetImageAsync(context);

                    source = scraper.Type.ToString();

                    try
                    {
                        using (_retrieveTime.Labels(source).Measure())
                            file = await fileTask;

                        if (file == null)
                        {
                            await ResultUtilities.NotFound(Name).ExecuteResultAsync(context);
                            return;
                        }

                        await using (file)
                        {
                            StorageFileResult.SetHeaders(context, file);

                            // pipe data to response stream while buffering in memory
                            // buffering is required because certain storage implementations (s3) need to know stream length in advance
                            await using var memory = memoryManager.GetStream();

                            var response = context.HttpContext.Response.BodyWriter;
                            var buffer   = _bufferPool.Rent(_bufferSize);

                            try
                            {
                                var resultSize = _resultSize.Labels(source);

                                using (_responseTime.Labels(source).Measure())
                                {
                                    while (true)
                                    {
                                        var read = await file.Stream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);

                                        if (read == 0)
                                        {
                                            await response.CompleteAsync();
                                            break;
                                        }

                                        resultSize.Inc(read);

                                        memory.Write(buffer, 0, read);

                                        await response.WriteAsync(((ReadOnlyMemory<byte>) buffer).Slice(0, read), CancellationToken.None);
                                    }
                                }
                            }
                            finally
                            {
                                _bufferPool.Return(buffer);

                                await file.DisposeAsync(); // opportunistic dispose

                                if (resourceContext != null)
                                    await resourceContext.DisposeAsync(); // background storage upload outside session concurrency
                            }

                            // save to storage
                            await storage.WriteAsync(new StorageFile
                            {
                                Name      = Name,
                                MediaType = file.MediaType,
                                Stream    = memory
                            }, CancellationToken.None);
                        }
                    }
                    catch
                    {
                        _errors.Labels(source).Inc();
                        throw;
                    }
                }
            }

            // exception while requesting file from storage
            else
            {
                _errors.Labels(source).Inc();

                ExceptionDispatchInfo.Throw(exception);
            }
        }

        /// <summary>
        /// Retrieves an image using a scraper when retrieval from storage failed.
        /// </summary>
        protected abstract (IScraper, Task<StorageFile>) GetImageAsync(ActionContext context);
    }
}