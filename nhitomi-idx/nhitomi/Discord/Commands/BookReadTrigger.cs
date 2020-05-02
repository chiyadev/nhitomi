using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using nhitomi.Database;

namespace nhitomi.Discord.Commands
{
    public class BookReadTrigger : ReactionTrigger
    {
        readonly nhitomiCommandContext _context;
        readonly Func<(DbBook, DbBookContent)?> _getter;

        public override IEmote Emote => new Emoji("\uD83D\uDCD6");

        public BookReadTrigger(nhitomiCommandContext context, Func<(DbBook, DbBookContent)?> getter)
        {
            _context = context;
            _getter  = getter;
        }

        protected override async Task<bool> RunAsync(CancellationToken cancellationToken = default)
        {
            var value = _getter();

            if (value == null)
                return false;

            await _context.SendAsync<BookReadMessage>(m => m.Book = value.Value, cancellationToken);
            return true;
        }
    }
}