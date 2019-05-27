using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using nhitomi.Interactivity.Triggers;

namespace nhitomi.Interactivity
{
    public abstract class ListInteractiveMessage<T> : InteractiveMessage
    {
        readonly EnumerableBrowser<T> _enumerable;

        protected T Current => _enumerable.Current;

        protected ListInteractiveMessage(EnumerableBrowser<T> enumerable)
        {
            _enumerable = enumerable;
        }

        protected override IEnumerable<ReactionTrigger> CreateTriggers()
        {
            yield return new ListTrigger<T>(MoveDirection.Left);
            yield return new ListTrigger<T>(MoveDirection.Right);
        }

        protected abstract Embed CreateEmbed(T value);
        protected abstract Embed CreateEmptyEmbed();

        /// <summary>
        /// This method is only called at the initialization stage.
        /// </summary>
        protected sealed override async Task UpdateViewAsync(CancellationToken cancellationToken = default)
        {
            // move initially if we haven't started enumerating
            if (_enumerable.Index == -1 && !await _enumerable.MoveNext(cancellationToken))
            {
                Dispose();

                // embed saying there is nothing in this list
                await SetViewAsync(CreateEmptyEmbed(), cancellationToken);
            }

            // show the first item
            await UpdateViewAsync(Current, cancellationToken);
        }

        public async Task NextAsync(CancellationToken cancellationToken = default)
        {
            if (!await _enumerable.MoveNext(cancellationToken))
            {
                //todo:
                return;
            }

            await UpdateViewAsync(Current, cancellationToken);
        }

        public async Task PreviousAsync(CancellationToken cancellationToken = default)
        {
            if (!_enumerable.MovePrevious())
            {
                return;
            }

            await UpdateViewAsync(Current, cancellationToken);
        }

        protected virtual Task UpdateViewAsync(T value, CancellationToken cancellationToken = default) =>
            SetViewAsync(CreateEmbed(value), cancellationToken);

        public override void Dispose()
        {
            base.Dispose();

            _enumerable.Dispose();
        }
    }
}