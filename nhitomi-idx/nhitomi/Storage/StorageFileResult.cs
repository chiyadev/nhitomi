using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace nhitomi.Storage
{
    /// <summary>
    /// An <see cref="ActionResult"/> that pipes a storage file stream to response body.
    /// </summary>
    public class StorageFileResult : ActionResult
    {
        readonly string _name;

        public StorageFileResult(string name)
        {
            _name = name;
        }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            var cancellationToken = context.HttpContext.RequestAborted;
            var storage           = context.HttpContext.RequestServices.GetService<IStorage>();

            var result = await storage.ReadAsync(_name, cancellationToken);

            if (!result.TryPickT0(out var file, out _))
            {
                await ResultUtilities.NotFound(_name).ExecuteResultAsync(context);
                return;
            }

            await using (file)
            {
                SetHeaders(context, file);

                await file.Stream.CopyToAsync(context.HttpContext.Response.Body, cancellationToken);
            }
        }

        public static void SetHeaders(ActionContext context, StorageFile file)
        {
            var headers = context.HttpContext.Response.GetTypedHeaders();

            // content-type
            headers.ContentType = new MediaTypeHeaderValue(file.MediaType);

            // content-length
            if (file.Stream.CanSeek)
                headers.ContentLength = file.Stream.Length;
        }
    }
}