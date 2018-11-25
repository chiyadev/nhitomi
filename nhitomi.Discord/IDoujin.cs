using System;
using System.Collections.Generic;

namespace nhitomi
{
    public interface IDoujin
    {
        string Id { get; }

        string PrettyName { get; }
        string OriginalName { get; }

        DateTime UploadTime { get; }
        DateTime ProcessTime { get; }

        IDoujinClient Source { get; }
        string SourceUrl { get; }

        string Scanlator { get; }
        string Language { get; }
        string ParodyOf { get; }

        IEnumerable<string> Characters { get; }
        IEnumerable<string> Categories { get; }
        IEnumerable<string> Artists { get; }
        IEnumerable<string> Tags { get; }

        IEnumerable<string> PageUrls { get; }
    }
}
