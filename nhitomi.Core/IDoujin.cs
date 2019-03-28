// Copyright (c) 2018-2019 fate/loli
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;

namespace nhitomi.Core
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

        int PageCount { get; }
        IEnumerable<PageInfo> Pages { get; }

        object GetSourceObject();
    }
}
