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
                using (file)
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

                    // save to storage while writing to response stream
                    file.Name   = name;
                    file.Stream = new ReadableAndConcurrentlyWritingStream(file.Stream, context.HttpContext.Response.Body);

                    StorageFileResult.SetHeaders(context, file);

                    await storage.WriteAsync(file, CancellationToken.None); // don't cancel
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