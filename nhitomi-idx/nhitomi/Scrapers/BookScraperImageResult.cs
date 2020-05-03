using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using nhitomi.Controllers;
using nhitomi.Database;
using nhitomi.Storage;

namespace nhitomi.Scrapers
{
    public class BookScraperImageResult : ScraperImageResult
    {
        readonly DbBook _book;
        readonly DbBookContent _content;
        readonly int _index;

        /// <summary>
        /// True to generate thumbnail.
        /// </summary>
        public bool Thumbnail { get; set; }

        protected string FileNamePrefix => $"books/{_book.Id}/contents/{_content.Id}";
        protected override string ReadFileName => Thumbnail ? $"{FileNamePrefix}/thumbs/{_index}" : WriteFileName;
        protected override string WriteFileName => $"{FileNamePrefix}/pages/{_index}";

        public BookScraperImageResult(DbBook book, DbBookContent content, int index)
        {
            _book    = book;
            _content = content;
            _index   = index;
        }

        protected override Task<Stream> GetImageAsync(ActionContext context)
        {
            var scrapers = context.HttpContext.RequestServices.GetService<IScraperService>();

            if (!scrapers.GetBook(_content.Source, out var scraper))
                throw new NotSupportedException($"Scraper {scraper} is not supported.");

            return scraper.GetImageAsync(_book, _content, _index, context.HttpContext.RequestAborted);
        }

        protected override Task<byte[]> PostProcessAsync(ActionContext context, byte[] buffer, CancellationToken cancellationToken = default)
        {
            if (Thumbnail)
                return PostProcessGenerateThumbnailAsync(context, buffer, cancellationToken);

            return base.PostProcessAsync(context, buffer, cancellationToken);
        }

        protected async Task<byte[]> PostProcessGenerateThumbnailAsync(ActionContext context, byte[] buffer, CancellationToken cancellationToken = default)
        {
            buffer = await base.PostProcessAsync(context, buffer, cancellationToken);

            var storage   = context.HttpContext.RequestServices.GetService<IStorage>();
            var processor = context.HttpContext.RequestServices.GetService<IImageProcessor>();
            var options   = context.HttpContext.RequestServices.GetService<IOptionsSnapshot<BookServiceOptions>>().Value.CoverThumbnail;

            // generate thumbnail
            buffer = await Task.Run(() => processor.GenerateThumbnail(buffer, options), cancellationToken);

            // save to storage
            using (var file = new StorageFile
            {
                Name      = ReadFileName,
                MediaType = processor.FormatToMediaType(options.Format),
                Stream    = new MemoryStream(buffer)
            })
            {
                StorageFileResult.SetHeaders(context, file);

                await storage.WriteAsync(file, cancellationToken);
            }

            return buffer;
        }
    }
}