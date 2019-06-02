using System.Globalization;

namespace nhitomi.Globalization
{
    public class EnglishLocalization : Localization
    {
        public override CultureInfo Culture { get; } = new CultureInfo("en");
        protected override CultureInfo FallbackCulture => null;

        protected override object CreateDefinition() => new
        {
            meta = new
            {
            },
            messages = new
            {
                doujinNotFound = "**nhitomi**: No such doujin!",
                invalidQuery = "**nhitomi**: `{query}` is not valid.",
                joinForDownload = "**nhitomi**: Please join our server to enable downloading! <{invite}>",

                listBeginning = "**nhitomi**: Beginning of the list!",
                listEnd = "**nhitomi**: End of the list!",

                collectionNotFound = "**nhitomi**: No such collection!",
                collectionDeleted = "**nhitomi**: Deleted collection `{collection.Name}`.",
                collectionSorted = "**nhitomi**: Sorted collection `{collection.Name}` by `{attribute}`.",

                addedToCollection = "**nhitomi**: Added `{doujin.Name}` to `{collection.Name}`.",
                removedFromCollection = "**nhitomi**: Removed `{doujin.Name}` from `{collection.Name}`.",
                alreadyInCollection = "**nhitomi**: `{doujin.Name}` already exists in `{collection.Name}`.",
                notInCollection = "**nhitomi**: `{doujin.Name}` was not found in `{collection.Name}`."
            },
            doujinMessage = new
            {
                title = "{doujin.Name}",
                footer = "{doujin.Source}/{doujin.SourceId}",
                language = "Language",
                group = "Group",
                parody = "Parody of",
                categories = "Categories",
                characters = "Characters",
                tags = "Tags",
                contents = "Contents",
                contentsValue = "{doujin.PageCount} pages",
                sourceIcons = new
                {
                    nhentai = "https://cdn.cybrhome.com/media/website/live/icon/icon_nhentai.net_57f740.png",
                    hitomi = "https://ltn.hitomi.la/favicon-160x160.png"
                }
            },
            downloadMessage = new
            {
                title = "**{doujin.Source}**: {doujin.Name}",
                text = "Click the link above to start downloading `{doujin.Name}`."
            },
            collectionMessage = new
            {
                title = "**{context.User.Username}**: {collection.Name}",
                empty = "Empty collection",
                sort = "Sort",
                sortValues = new
                {
                    uploadTime = "Upload time",
                    processTime = "Process time",
                    identifier = "Identifier",
                    name = "Name",
                    artist = "Artist",
                    group = "Group",
                    scanlator = "Scanlator",
                    language = "Language",
                    parody = "Parody"
                },
                contents = "Contents",
                contentsValue = "{collection.Doujins.Count} doujins"
            },
            helpMessage = new
            {
                title = "**nhitomi**: Help",
                footer = "Powered by chiya.dev",
                about = "a Discord bot for searching and downloading doujinshi",
                invite = "Official server: <{invite}>",
                doujins = new
                {
                    heading = "Doujinshi",
                    get = "Displays full doujin information.",
                    from = "Displays all doujins from a source.",
                    search = "Searches for doujins by their title and tags.",
                    download = "Sends the download link for a doujin."
                },
                collections = new
                {
                    heading = "Collection Management",
                    list = "Shows all collections you own.",
                    view = "Shows the doujins in a collection.",
                    add = "Adds a doujin to a collection",
                    remove = "Removes a doujin from a collection",
                    sort = "Sorts a collection by a doujin attribute.",
                    delete = "Deletes a collection, removing all doujins in it."
                },
                sources = new
                {
                    heading = "Sources"
                },
                openSource = new
                {
                    heading = "Open Source",
                    license = "This project is licensed under the MIT License.",
                    contribution = "Contributions are welcome! <{repoUrl}>"
                }
            },
            errorMessage = new
            {
                title = "**nhitomi**: Error",
                text = "The error has been reported. For further assistance, please join <{invite}>."
            }
        };
    }
}