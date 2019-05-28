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

        protected abstract Task<IEnumerable<TValue>> GetValuesAsync(TView view, int offset,
            CancellationToken cancellationToken = default);

        public abstract class ListViewBase : ViewBase
        {
            public ListMessage<TView, TValue> ListMessage;

            async Task<(bool, TValue)> TryGetCurrentAsync(CancellationToken cancellationToken = default)
            {
                var cache = ListMessage._valueCache;
                var index = ListMessage.Position;

                // return cached value if possible
                if (index < cache.Count)
                    return (true, cache[index]);

                if (ListMessage._fullyLoaded)
                {
                    // clamp position
                    ListMessage.Position = Math.Clamp(index, 0, cache.Count - 1);

                    return default;
                }

                var values = (await ListMessage.GetValuesAsync((TView) this, index, cancellationToken)).ToArray();

                if (values.Length == 0)
                {
                    // set fully loaded flag so we don't bother enumerating again
                    ListMessage._fullyLoaded = values.Length == 0;

                    // clamp position
                    ListMessage.Position = Math.Clamp(index, 0, cache.Count - 1);

                    return default;
                }

                // add new values to cache
                cache.AddRange(values);

                return (true, values[0]);
            }

            protected abstract Embed CreateEmbed(TValue value);
            protected abstract Embed CreateEmptyEmbed();

            public override async Task<bool> UpdateAsync(CancellationToken cancellationToken = default)
            {
                var (success, current) = await TryGetCurrentAsync(cancellationToken);

                if (success)
                {
                    // show the first item
                    await SetEmbedAsync(CreateEmbed(current), cancellationToken);

                    return true;
                }

                // embed saying there is nothing in this list
                if (ListMessage._valueCache.Count == 0)
                    await SetEmbedAsync(CreateEmptyEmbed(), cancellationToken);

                // reached start of list
                //todo: localize
                else if (ListMessage.Position <= 0)
                    await SetMessageAsync("start of list", cancellationToken);

                // reached end of list
                else
                    await SetMessageAsync("end of list", cancellationToken);

                return false;
            }
        }
    }
}