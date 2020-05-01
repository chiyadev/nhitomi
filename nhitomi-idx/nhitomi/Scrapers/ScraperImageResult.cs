using System;
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
        protected abstract string ReadFileName { get; }
        protected virtual string WriteFileName => ReadFileName;

        /// <summary>
        /// Implement to retrieve an image using a scraper if retrieval from storage failed.
        /// </summary>
        protected abstract Task<Stream> GetImageAsync(ActionContext context);

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            var readName = ReadFileName;

            var cancellationToken = context.HttpContext.RequestAborted;
            var storage           = context.HttpContext.RequestServices.GetService<IStorage>();

            var result = await storage.ReadAsync(readName, cancellationToken);

            retryResult:

            // file is already available in storage
            if (result.TryPickT0(out var file, out var error))
            {
                using (file)
                {
                    StorageFileResult.SetHeaders(context, file);

                    // write to response stream
                    await file.Stream.CopyToAsync(context.HttpContext.Response.Body, cancellationToken);
                }
            }

            else if (error.TryPickT0(out _, out var exception))
            {
                var writeName = WriteFileName;
                var buffer    = null as byte[];

                var locker = context.HttpContext.RequestServices.GetService<IResourceLocker>();

                // prevent concurrent downloads
                await using (await locker.EnterAsync($"scraper:image:{writeName}", cancellationToken))
                {
                    // file may have been added to storage while we were awaiting lock
                    result = await storage.ReadAsync(readName, cancellationToken);

                    if (result.TryPickT0(out file, out error) || !error.TryPickT0(out _, out exception))
                        goto retryResult;

                    // download to memory
                    await using (var stream = await GetImageAsync(context))
                    {
                        if (stream != null)
                            buffer = await stream.ToArrayAsync(CancellationToken.None); // don't cancel
                    }

                    if (buffer != null)
                    {
                        // detect format
                        var mediaType = context.HttpContext.RequestServices.GetService<IImageProcessor>().GetMediaType(buffer);

                        if (mediaType == null)
                            throw new FormatException($"Unrecognized image format ({readName}, {buffer.Length}).");

                        Task saveTask;

                        // save to storage
                        using (file = new StorageFile
                        {
                            Name      = writeName,
                            MediaType = mediaType,
                            Stream    = new MemoryStream(buffer)
                        })
                        {
                            StorageFileResult.SetHeaders(context, file);

                            saveTask = storage.WriteAsync(file, CancellationToken.None); // don't cancel
                        }

                        // postprocess while saving
                        buffer = await PostProcessAsync(context, buffer, CancellationToken.None); // don't cancel

                        await saveTask;
                    }
                }

                if (buffer == null)
                    await ResultUtilities.NotFound(readName).ExecuteResultAsync(context);

                // write to response outside lock
                else
                    await context.HttpContext.Response.BodyWriter.WriteAsync(buffer, cancellationToken);
            }

            else
            {
                ExceptionDispatchInfo.Throw(exception);
            }
        }

        protected virtual Task<byte[]> PostProcessAsync(ActionContext context, byte[] buffer, CancellationToken cancellationToken = default) => Task.FromResult(buffer);
    }
}