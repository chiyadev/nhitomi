using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace nhitomi.Storage
{
    /// <summary>
    /// <see cref="ActionResult"/> that downloads a file from storage and writes file data.
    /// </summary>
    public class StorageFileResult : ActionResult
    {
        public TimeSpan? CacheControl { get; set; }

        readonly string _name;

        public StorageFileResult(string name)
        {
            _name = name;
        }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            var cancellationToken = context.HttpContext.RequestAborted;
            var storage           = context.HttpContext.RequestServices.GetService<IStorage>();

            var readResult = await storage.ReadAsync(_name, cancellationToken);

            if (!readResult.TryPickT0(out var file, out _))
            {
                await ResultUtilities.NotFound<StorageFile>(_name).ExecuteResultAsync(context);
                return;
            }

            using (file)
            {
                var headers = context.HttpContext.Response.GetTypedHeaders();

                // content-type
                headers.ContentType = new MediaTypeHeaderValue(file.MediaType);

                // cache-control
                if (CacheControl != null)
                    headers.CacheControl = new CacheControlHeaderValue { MaxAge = CacheControl };

                await using var stream = await ProcessStreamAsync(context, file.Stream, cancellationToken);

                // content-length
                if (stream.CanSeek)
                    headers.ContentLength = stream.Length;

                // write to response
                await stream.CopyToAsync(context.HttpContext.Response.Body, cancellationToken);
            }
        }

        protected virtual Task<Stream> ProcessStreamAsync(ActionContext context, Stream stream, CancellationToken cancellationToken = default) => Task.FromResult(stream);
    }
}