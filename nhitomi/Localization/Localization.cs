using System.Globalization;

namespace nhitomi.Localization
{
    public abstract class Localization
    {
        public abstract CultureInfo Culture { get; }

        public abstract DoujinMessageLocalization DoujinMessage { get; }
        public abstract DownloadMessageLocalization DownloadMessage { get; }
        public abstract HelpMessageLocalization HelpMessage { get; }

        public abstract class DoujinMessageLocalization
        {
            public virtual string Language => "Language";
            public virtual string ParodyOf => "Parody of";
            public virtual string Categories => "Categories";
            public virtual string Characters => "Characters";
            public virtual string Tags => "Tags";
            public virtual string Contents => "Content";
        }

        public abstract class DownloadMessageLocalization
        {
            public virtual string Description => "Click the link above to start downloading `{doujin.Name}`.";
        }

        public abstract class HelpMessageLocalization
        {
            public virtual string Title => "Help";
            public virtual string About => "a Discord bot for searching and downloading doujinshi, by **chiya.dev**.";
            public virtual string OfficialServer => "Official server: <{invite}>";

            public virtual string DoujinsHeading => "Doujinshi";
            public virtual string DoujinsGet => "Displays doujin information from a source by its ID.";
            public virtual string DoujinsAll => "Displays all doujins from a source.";
            public virtual string DoujinsSearch => "Searches for doujins that match your query.";
            public virtual string DoujinsDownload => "Sends a download link for a doujin.";

            public virtual string CollectionsHeading => "Collection Management";
            public virtual string Collections => "Lists all collections you own.";
            public virtual string CollectionsShow => "Displays the doujins in a collection.";
            public virtual string CollectionsAddRemove => "Adds or removes a doujin in a collection.";
            public virtual string CollectionsList => "Lists all doujins in a collection.";
            public virtual string CollectionsSort => "Sorts a collection by a doujin's attribute.";
            public virtual string CollectionsDelete => "Deletes a collection, removing all doujins in it.";

            public virtual string SourcesHeading => "Sources";

            public virtual string OpenSourceHeading => "Open Source";
            public virtual string OpenSourceLicense => "This project is licensed under the MIT License.";
            public virtual string OpenSourceContribution => "Contributions are welcome! <{repoUrl}>";
        }
    }
}