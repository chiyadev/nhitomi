using System.Globalization;

namespace nhitomi.Localization
{
    public abstract class Localization
    {
        public abstract CultureInfo Culture { get; }

        public virtual DoujinMessageLocalization DoujinMessage { get; } = new DoujinMessageLocalization();
        public virtual DownloadMessageLocalization DownloadMessage { get; } = new DownloadMessageLocalization();
        public virtual HelpMessageLocalization HelpMessage { get; } = new HelpMessageLocalization();

        public class DoujinMessageLocalization
        {
            public virtual string Language => "Language";
            public virtual string ParodyOf => "Parody of";
            public virtual string Categories => "Categories";
            public virtual string Characters => "Characters";
            public virtual string Tags => "Tags";
            public virtual string Contents => "Content";
        }

        public class DownloadMessageLocalization
        {
            public virtual string Description(string name) =>
                $"Click the link above to start downloading `{name}`.";
        }

        public class HelpMessageLocalization
        {
            public virtual string Title => "Help";

            public virtual string AboutNhitomi =>
                "nhitomi — a Discord bot for searching and downloading doujinshi, by **chiya.dev**.";

            public virtual string OfficialServer(string url) =>
                $"Official server: <{url}>";

            public virtual string DoujinsHeading => "Doujinshi";
            public virtual string DoujinsGet => "Displays doujin information from a source by its ID.";
            public virtual string DoujinsAll => "Displays all doujins from a source.";
            public virtual string DoujinsSearch => "Searches for doujins that match your query.";
            public virtual string DoujinsDownload => "Sends a download link for a doujin.";

            public virtual string CollectionsHeading => "Collection management";
            public virtual string Collections => "Lists all collections you own.";
            public virtual string CollectionsShow => "Displays the doujins in a collection.";
            public virtual string CollectionsAddRemove => "Adds or removes a doujin in a collection.";
            public virtual string CollectionsList => "Lists all doujins in a collection.";
            public virtual string CollectionsSort => "Sorts a collection by a doujin's attribute.";
            public virtual string CollectionsDelete => "Deletes a collection, removing all doujins in it.";

            public virtual string SourcesHeading => "Sources";

            public virtual string ContributionHeading => "Contribution";
            public virtual string ContributionLicense => "This project is licensed under the MIT License.";
            public virtual string ContributionMessage(string url) => $"Contributions are welcome! <{url}>";
        }
    }
}