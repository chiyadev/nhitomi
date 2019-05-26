using Discord;
using nhitomi.Core;

namespace nhitomi.Interactivity
{
    public class DoujinListMessage : ListInteractiveMessage<Doujin>
    {
        public DoujinListMessage(EnumerableBrowser<Doujin> enumerable) : base(enumerable)
        {
        }

        protected override Embed CreateEmbed(Doujin value) => DoujinMessage.CreateEmbed(value);
        protected override Embed CreateEmptyEmbed() => throw new System.NotImplementedException();
    }
}