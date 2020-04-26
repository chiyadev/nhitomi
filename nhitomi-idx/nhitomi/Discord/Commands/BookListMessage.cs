using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using nhitomi.Database;
using nhitomi.Localization;

namespace nhitomi.Discord.Commands
{
    public class BookListMessage : InteractiveMessage, IListTriggerTarget
    {
        /// <summary>
        /// Content will be selected automatically if null.
        /// </summary>
        public INavigableAsyncEnumerator<(DbBook, DbBookContent)> Enumerator { get; set; }

        readonly ILocale _l;
        readonly ILinkGenerator _link;

        public BookListMessage(nhitomiCommandContext context, ILinkGenerator link)
        {
            _l    = context.Locale.Sections["get.book"];
            _link = link;
        }

        protected override IEnumerable<ReactionTrigger> CreateTriggers()
        {
            foreach (var trigger in base.CreateTriggers())
                yield return trigger;

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

        protected override ReplyContent Render()
        {
            // todo: no results message
            if (!_valid)
                return null;

            var (book, content) = Enumerator.Current;

            // todo: smarter content selection based on user locale
            content ??= book.Contents[0];

            return BookMessage.Render(book, content, _l, _link);
        }

        int IListTriggerTarget.Position => Enumerator.Position;

        public Task<bool> SetPositionAsync(int position, CancellationToken cancellationToken = default) => Enumerator.MoveToAsync(position).AsTask();

        protected override async ValueTask OnDisposeAsync()
        {
            await base.OnDisposeAsync();

            await Enumerator.DisposeAsync();
        }
    }
}