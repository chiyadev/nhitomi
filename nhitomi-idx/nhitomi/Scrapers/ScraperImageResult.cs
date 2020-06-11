using System;
using System.Buffers;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using nhitomi.Storage;

namespace nhitomi.Scrapers
{
    public abstract class ScraperImageResult : ActionResult
    {
        public abstract string Name { get; }

        /// <summary>
        /// Retrieves an image using a scraper when retrieval from storage failed.
        /// </summary>
        protected abstract Task<StorageFile> GetImageAsync(ActionContext context);

        const int _bufferSize = 32768;
        static readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            var name = Name;

            var cancellationToken = context.HttpContext.RequestAborted;
            var storage           = context.HttpContext.RequestServices.GetService<IStorage>();

            var result = await storage.ReadAsync(name, cancellationToken);

            determineResult:

            // file is already saved in storage
            if (result.TryPickT0(out var file, out var error))
            {
                await using (file)
                {
                    StorageFileResult.SetHeaders(context, file);

                    // write to response stream
                    await file.Stream.CopyToAsync(context.HttpContext.Response.Body, cancellationToken);
                }
            }

            // file is not found
            else if (error.TryPickT0(out _, out var exception))
            {
                var locker = context.HttpContext.RequestServices.GetService<IResourceLocker>();

                // prevent concurrent downloads
                await using (await locker.EnterAsync($"scraper:image:{name}", cancellationToken))
                {
                    // file may have been saved to storage while we were awaiting lock
                    result = await storage.ReadAsync(name, cancellationToken);

                    if (result.TryPickT0(out file, out error) || !error.TryPickT0(out _, out exception)) // found, or error
                        goto determineResult;

                    // retrieve image using scraper
                    file = await GetImageAsync(context);

                    if (file == null)
                    {
                        await ResultUtilities.NotFound(name).ExecuteResultAsync(context);
                        return;
                    }

                    await using (file)
                    {
                        StorageFileResult.SetHeaders(context, file);

                        // pipe data to response stream while buffering in memory
                        // buffering is required because certain storage implementations (s3) need to know stream length in advance
                        await using var memory = new MemoryStream();

                        var response = context.HttpContext.Response.BodyWriter;
                        var buffer   = _bufferPool.Rent(_bufferSize);

                        try
                        {
                            while (true)
                            {
                                var read = await file.Stream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);

                                if (read == 0)
                                {
                                    await response.CompleteAsync();
                                    break;
                                }

                                memory.Write(buffer, 0, read);

                                await response.WriteAsync(((ReadOnlyMemory<byte>) buffer).Slice(0, read), CancellationToken.None);
                            }
                        }
                        finally
                        {
                            _bufferPool.Return(buffer);

                            await file.DisposeAsync(); // opportunistic dispose
                        }

                        // save to storage
                        await storage.WriteAsync(new StorageFile
                        {
                            Name      = name,
                            MediaType = file.MediaType,
                            Stream    = memory
                        }, CancellationToken.None);
                    }
                }
            }

            // exception while requesting file from storage
            else
            {
                ExceptionDispatchInfo.Throw(exception);
            }
        }
    }
}