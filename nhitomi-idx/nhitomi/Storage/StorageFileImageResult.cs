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
    /// <see cref="ActionResult"/> that downloads an image from storage and writes image data.
    /// </summary>
    public class StorageFileImageResult : StorageFileResult
    {
        public ImageFormat Format { get; set; }
        public int Quality { get; set; } = 80;

        public StorageFileImageResult(string name) : base(name) { }

        protected override async Task<Stream> ProcessStreamAsync(ActionContext context, Stream stream, CancellationToken cancellationToken = default)
        {
            var processor = context.HttpContext.RequestServices.GetService<IImageProcessor>();

            // overwrite content-type with new format type
            var headers = context.HttpContext.Response.GetTypedHeaders();

            headers.ContentType = new MediaTypeHeaderValue(processor.GetMediaType(Format));

            // convert
            var buffer = await stream.ToArrayAsync(cancellationToken);

            return processor.Convert(buffer, Quality, Format);
        }
    }
}