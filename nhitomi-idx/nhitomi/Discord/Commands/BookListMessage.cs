using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using nhitomi.Database;
using nhitomi.Localization;
using nhitomi.Scrapers;

namespace nhitomi.Discord.Commands
{
    public class BookListMessage : InteractiveMessage, IListTriggerTarget
    {
        public INavigableAsyncEnumerator<DbBook> Enumerator { get; set; }
        public (DbBook book, DbBookContent content)? Current { get; private set; }

        readonly nhitomiCommandContext _context;
        readonly ILocale _l;
        readonly ILinkGenerator _link;
        readonly IBookContentSelector _selector;
        readonly IScraperService _scrapers;

        public BookListMessage(nhitomiCommandContext context, ILinkGenerator link, IBookContentSelector selector, IScraperService scrapers)
        {
            _context  = context;
            _l        = context.Locale.Sections["get.book"];
            _link     = link;
            _selector = selector;
            _scrapers = scrapers;
        }

        protected override IEnumerable<ReactionTrigger> CreateTriggers()
        {
            foreach (var trigger in base.CreateTriggers())
                yield return trigger;

            yield return new BookReadTrigger(_context, () => Current);
            yield return new ListTrigger(this, ListTriggerDirection.Left);
            yield return new ListTrigger(this, ListTriggerDirection.Right);
            yield return new DestroyTrigger(this);
        }

        bool _valid;

        protected override async Task OnInitialize(CancellationToken cancellationToken = default)
        {
            await base.OnInitialize(cancellationToken);

            _valid = await Enumerator.MoveNextAsync();
        }

        protected override async Task<ReplyContent> RenderAsync(CancellationToken cancellationToken = default)
        {
            // todo: no results message
            if (!_valid)
                return null;

            var (book, content) = Current ??= (Enumerator.Current, await _selector.SelectAsync(Enumerator.Current, _context, cancellationToken));

            return BookMessage.Render(book, content, _l, _link, _scrapers);
        }

        int IListTriggerTarget.Position => Enumerator.Position;

        public async Task<bool> SetPositionAsync(int position, CancellationToken cancellationToken = default)
        {
            if (await Enumerator.MoveToAsync(position))
            {
                Current = null;
                return true;
            }

            return false;
        }

        protected override async ValueTask OnDisposeAsync()
        {
            await base.OnDisposeAsync();
            await Enumerator.DisposeAsync();
        }
    }
}