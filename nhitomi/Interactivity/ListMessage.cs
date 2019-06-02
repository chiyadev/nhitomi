using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using nhitomi.Globalization;
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

        public abstract class ListViewBase : ViewBase
        {
            public ListMessage<TView, TValue> ListMessage;

            protected abstract Task<TValue[]> GetValuesAsync(int offset, CancellationToken cancellationToken = default);

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
                    ListMessage.Position = cache.Count - 1;

                    return (Status.End, default);
                }

                var values = await GetValuesAsync(index, cancellationToken);

                if (values.Length == 0)
                {
                    // set fully loaded flag so we don't bother enumerating again
                    ListMessage._fullyLoaded = values.Length == 0;

                    ListMessage.Position = cache.Count - 1;

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

                if (ListMessage._valueCache.Count == 0)
                {
                    // embed saying there is nothing in this list
                    await SetEmbedAsync(CreateEmptyEmbed(), cancellationToken);

                    return false;
                }

                // we reached the extremes
                var path = new LocalizationPath("messages");

                switch (status)
                {
                    case Status.Start:
                        await SetMessageAsync(path["listBeginning"][Context](), cancellationToken);
                        break;

                    case Status.End:
                        await SetMessageAsync(path["listEnd"][Context](), cancellationToken);
                        break;
                }

                return false;
            }
        }
    }
}