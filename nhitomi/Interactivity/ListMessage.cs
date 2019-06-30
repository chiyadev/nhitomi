using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using nhitomi.Discord;
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

        public abstract class ListViewBase : ViewBase
        {
            new ListMessage<TView, TValue> Message => (ListMessage<TView, TValue>) base.Message;

            protected virtual bool ShowLoadingIndication => true;

            protected abstract Task<TValue[]> GetValuesAsync(int offset,
                                                             CancellationToken cancellationToken = default);

            enum Status
            {
                Start,
                End,
                Ok
            }

            async Task<(Status, TValue)> TryGetCurrentAsync(CancellationToken cancellationToken = default)
            {
                var cache = Message._valueCache;
                var index = Message.Position;

                if (index < 0)
                {
                    Message.Position = 0;

                    return (Status.Start, default);
                }

                // return cached value if possible
                if (index < cache.Count)
                    return (Status.Ok, cache[index]);

                if (Message._fullyLoaded)
                {
                    Message.Position = cache.Count - 1;

                    return (Status.End, default);
                }

                // show loading indication if we are triggered by a reaction
                if (ShowLoadingIndication && Context is IReactionContext)
                    await SetMessageAsync("listLoading", null, cancellationToken);

                // get new values
                var values = await GetValuesAsync(index, cancellationToken);

                if (values == null || values.Length == 0)
                {
                    // set fully loaded flag so we don't bother enumerating again
                    Message._fullyLoaded = true;

                    Message.Position = cache.Count - 1;

                    return (Status.End, default);
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

                if (Message._valueCache.Count == 0)
                {
                    // embed saying there is nothing in this list
                    await SetEmbedAsync(CreateEmptyEmbed(), cancellationToken);

                    return false;
                }

                // we reached the extremes
                switch (status)
                {
                    case Status.Start:
                        await SetMessageAsync("listBeginning", null, cancellationToken);
                        break;

                    case Status.End:
                        await SetMessageAsync("listEnd", null, cancellationToken);
                        break;
                }

                return false;
            }
        }

        public abstract class SynchronousListViewBase : ListViewBase
        {
            protected sealed override bool ShowLoadingIndication => false;

            protected sealed override Task<TValue[]> GetValuesAsync(int offset,
                                                                    CancellationToken cancellationToken = default) =>
                Task.FromResult(GetValues(offset));

            protected abstract TValue[] GetValues(int offset);
        }
    }
}