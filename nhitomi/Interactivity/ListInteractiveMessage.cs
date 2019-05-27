using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using nhitomi.Interactivity.Triggers;

namespace nhitomi.Interactivity
{
    public abstract class ListInteractiveMessage<T> : InteractiveMessage
    {
        readonly AsyncEnumerableBrowser<T> _enumerable;

        protected T Current => _enumerable.Current;

        protected ListInteractiveMessage(IAsyncEnumerable<T> enumerable)
        {
            _enumerable = enumerable.CreateAsyncBrowser();
        }

        protected override IEnumerable<ReactionTrigger> CreateTriggers()
        {
            yield return new ListTrigger<T>(MoveDirection.Left);
            yield return new ListTrigger<T>(MoveDirection.Right);
        }

        protected abstract Embed CreateEmbed(IServiceProvider services, T value);
        protected abstract Embed CreateEmptyEmbed(IServiceProvider services);

        protected sealed override async Task<bool> InitializeViewAsync(IServiceProvider services,
            CancellationToken cancellationToken = default)
        {
            // move initially if we haven't started enumerating
            if (_enumerable.Index == -1 && !await _enumerable.MoveNext(cancellationToken))
            {
                Dispose();

                // embed saying there is nothing in this list
                await SetViewAsync(CreateEmptyEmbed(services), cancellationToken);

                return false;
            }

            // show the first item
            await UpdateViewAsync(services, Current, cancellationToken);

            return true;
        }

        protected virtual Task UpdateViewAsync(IServiceProvider services, T value,
            CancellationToken cancellationToken = default) =>
            SetViewAsync(CreateEmbed(services, value), cancellationToken);

        public async Task NextAsync(IServiceProvider services, CancellationToken cancellationToken = default)
        {
            if (!await _enumerable.MoveNext(cancellationToken))
            {
                //todo:
                return;
            }

            await UpdateViewAsync(services, Current, cancellationToken);
        }

        public async Task PreviousAsync(IServiceProvider services, CancellationToken cancellationToken = default)
        {
            if (!_enumerable.MovePrevious())
            {
                return;
            }

            await UpdateViewAsync(services, Current, cancellationToken);
        }

        public override void Dispose()
        {
            base.Dispose();

            _enumerable.Dispose();
        }
    }
}