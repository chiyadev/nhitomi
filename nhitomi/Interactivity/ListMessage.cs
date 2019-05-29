using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using nhitomi.Interactivity.Triggers;

namespace nhitomi.Interactivity
{
    public interface IListMessage : IInteractiveMessage
    {
        int Position { get; set; }
    }

    public abstract class ListMessage<TView, TValue> : InteractiveMessage<TView>, IListMessage
        where TView : ListMessage<TView, TValue>.ListViewBase
    {
        readonly List<TValue> _valueCache = new List<TValue>();
        bool _fullyLoaded;

        public int Position { get; set; }

        protected override IEnumerable<IReactionTrigger> CreateTriggers()
        {
            yield return new ListTrigger(MoveDirection.Left);
            yield return new ListTrigger(MoveDirection.Right);
        }

        protected override void InitializeView(TView view)
        {
            base.InitializeView(view);

            view.ListMessage = this;
        }

        protected abstract IAsyncEnumerable<TValue> GetValuesAsync(TView view, int offset,
            CancellationToken cancellationToken = default);

        public abstract class ListViewBase : ViewBase
        {
            public ListMessage<TView, TValue> ListMessage;

            enum Status
            {
                Start,
                End,
                Ok
            }

            async Task<(Status, TValue)> TryGetCurrentAsync(CancellationToken cancellationToken = default)
            {
                var cache = ListMessage._valueCache;
                var index = ListMessage.Position;

                if (index < 0)
                {
                    ListMessage.Position = 0;

                    return (Status.Start, default);
                }

                // return cached value if possible
                if (index < cache.Count)
                    return (Status.Ok, cache[index]);

                if (ListMessage._fullyLoaded)
                {
                    ListMessage.Position = Math.Min(index, cache.Count);

                    return (Status.End, default);
                }

                var values = await ListMessage
                    .GetValuesAsync((TView) this, index, cancellationToken)
                    .ToArray(cancellationToken);

                if (values.Length == 0)
                {
                    // set fully loaded flag so we don't bother enumerating again
                    ListMessage._fullyLoaded = values.Length == 0;

                    ListMessage.Position = Math.Min(index, cache.Count);

                    return default;
                }

                // add new values to cache
                cache.AddRange(values);

                return (Status.Ok, values[0]);
            }

            protected abstract Embed CreateEmbed(TValue value);
            protected abstract Embed CreateEmptyEmbed();

            public override async Task<bool> UpdateAsync(CancellationToken cancellationToken = default)
            {
                var (status, current) = await TryGetCurrentAsync(cancellationToken);

                if (status == Status.Ok)
                {
                    // show the first item
                    await SetEmbedAsync(CreateEmbed(current), cancellationToken);

                    return true;
                }

                // embed saying there is nothing in this list
                if (ListMessage._valueCache.Count == 0)
                {
                    await SetEmbedAsync(CreateEmptyEmbed(), cancellationToken);

                    return false;
                }

                // reached start of list
                //todo: localize

                switch (status)
                {
                    // reached end of list
                    case Status.Start:
                        await SetMessageAsync("start of list", cancellationToken);
                        break;
                    case Status.End:
                        await SetMessageAsync("end of list", cancellationToken);
                        break;
                }

                return false;
            }
        }
    }
}