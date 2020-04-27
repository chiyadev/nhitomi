using System;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using nhitomi.Storage;
using OneOf;

namespace nhitomi.Scrapers
{
    public abstract class ScraperImageResult : ActionResult
    {
        protected abstract string FileName { get; }

        /// <summary>
        /// Implement to retrieve an image using a scraper if retrieval from storage failed.
        /// </summary>
        protected abstract Task<Stream> GetImageAsync(ActionContext context);

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            var name = FileName;

            var cancellationToken = context.HttpContext.RequestAborted;
            var storage           = context.HttpContext.RequestServices.GetService<IStorage>();

            var result = await storage.ReadAsync(name, cancellationToken);

            retryResult:

            // file is already available in storage
            if (result.TryPickT0(out var file, out var error))
            {
                using (file)
                {
                    StorageFileResult.SetHeaders(context, file);

                    // write to response stream
                    await using var src  = await ProcessStreamAsync(context, file.Stream);
                    await using var dest = context.HttpContext.Response.Body;

                    await src.CopyToAsync(dest, cancellationToken);
                }
            }

            else if (error.TryPickT0(out _, out var exception))
            {
                var buffer = null as byte[];

                var locker = context.HttpContext.RequestServices.GetService<IResourceLocker>();

                // use lock to prevent concurrent downloads
                await using (await locker.EnterAsync($"scraper:image:{name}", cancellationToken))
                {
                    // file may have been added to storage while we were awaiting lock
                    result = await storage.ReadAsync(name, cancellationToken);

                    if (result.TryPickT0(out file, out error) || !error.TryPickT0(out _, out exception))
                        goto retryResult;

                    // download to memory
                    await using (var stream = await GetImageAsync(context))
                    {
                        if (stream != null)
                            buffer = await stream.ToArrayAsync(CancellationToken.None); // don't cancel while downloading
                    }

                    if (buffer != null)
                    {
                        // detect format
                        var mediaType = context.HttpContext.RequestServices.GetService<IImageProcessor>().GetMediaType(buffer);

                        if (mediaType == null)
                            throw new FormatException($"Unrecognized image format ({name}, {buffer.Length}).");

                        using (file = new StorageFile
                        {
                            Name      = name,
                            MediaType = mediaType,
                            Stream    = new MemoryStream(buffer)
                        })
                        {
                            StorageFileResult.SetHeaders(context, file);

                            // save to storage
                            await storage.WriteAsync(file, CancellationToken.None); // don't cancel from saving
                        }
                    }
                }

                if (buffer == null)
                {
                    await ResultUtilities.NotFound(FileName).ExecuteResultAsync(context);
                }
                else
                {
                    // write to response stream
                    await using var src  = await ProcessStreamAsync(context, buffer);
                    await using var dest = context.HttpContext.Response.Body;

                    await src.CopyToAsync(dest, cancellationToken);
                }
            }

            else
            {
                ExceptionDispatchInfo.Throw(exception);
            }
        }

        public ThumbnailOptions Thumbnail { get; set; }

        protected async Task<Stream> ProcessStreamAsync(ActionContext context, OneOf<Stream, byte[]> data)
        {
            if (Thumbnail == null)
                return data.Match(
                    s => s,
                    b => new MemoryStream(b));

            var cancellationToken = context.HttpContext.RequestAborted;
            var processor         = context.HttpContext.RequestServices.GetService<IImageProcessor>();

            if (!data.TryPickT1(out var buffer, out var stream))
                buffer = await stream.ToArrayAsync(cancellationToken);

            // replace buffer with thumbnail
            buffer = processor.GenerateThumbnail(buffer, Thumbnail);

            // overwrite content-length with new thumbnail size
            context.HttpContext.Response.GetTypedHeaders().ContentLength = buffer.Length;

            return new MemoryStream(buffer);
        }
    }
}