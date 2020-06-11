using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using nhitomi.Database;
using nhitomi.Storage;

namespace nhitomi.Scrapers
{
    public class BookScraperImageResult : ScraperImageResult
    {
        readonly DbBook _book;
        readonly DbBookContent _content;
        readonly int _index;

        public override string Name => $"books/{_book.Id}/contents/{_content.Id}/pages/{_index}";

        public BookScraperImageResult(DbBook book, DbBookContent content, int index)
        {
            _book    = book;
            _content = content;
            _index   = index;
        }

        protected override Task<StorageFile> GetImageAsync(ActionContext context)
        {
            var scrapers = context.HttpContext.RequestServices.GetService<IScraperService>();

            if (!scrapers.GetBook(_content.Source, out var scraper))
                throw new NotSupportedException($"Scraper {scraper} is not supported.");

            return scraper.GetImageAsync(_book, _content, _index, context.HttpContext.RequestAborted);
        }
    }
}