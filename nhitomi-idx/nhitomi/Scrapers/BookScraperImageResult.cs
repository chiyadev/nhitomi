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
        readonly DbBookContent _content;
        readonly int _index;

        public override string Name { get; }

        public BookScraperImageResult(DbBook book, DbBookContent content, int index)
        {
            _content = content;
            _index   = index;

            Name = $"books/{book.Id}/contents/{content.Id}/pages/{index}";
        }

        protected override (IScraper, Task<StorageFile>) GetImageAsync(ActionContext context)
        {
            var scrapers = context.HttpContext.RequestServices.GetService<IScraperService>();

            if (!scrapers.GetBook(_content.Source, out var scraper))
                throw new NotSupportedException($"Scraper {scraper} is not supported.");

            return (scraper, scraper.GetImageAsync(_content, _index, context.HttpContext.RequestAborted));
        }
    }
}