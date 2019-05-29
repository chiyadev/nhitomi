using System;
using nhitomi.Core.Clients.Testing;

namespace nhitomi.Core.Clients.nhentai
{
    public class nhentai82843 : ClientTestCase
    {
        public override string DoujinId => "82843";
        public override Type ClientType => typeof(nhentaiClient);

        public override DoujinInfo KnownValue { get; } = new DoujinInfo
        {
            ParodyOf = "the world god only knows",
            Characters = new[]
            {
                "keima katsuragi"
            },
            Tags = new[]
            {
                "anal",
                "schoolgirl uniform",
                "glasses",
                "shotacon",
                "yaoi",
                "crossdressing",
                "chikan"
            },
            Artist = "tomekichi",
            Group = "tottototomekichi",
            Language = "japanese"
        };
    }
}
