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
                invalidQuery = "**nhitomi**: Please specify your query.",
                joinForDownload = "**nhitomi**: Please join our server to enable downloading! <{invite}>",

                listBeginning = "**nhitomi**: Beginning of the list!",
                listEnd = "**nhitomi**: End of the list!",
                emptyList = "**nhitomi**: No results!",

                collectionNotFound = "**nhitomi**: No such collection!",
                collectionDeleted = "**nhitomi**: Deleted collection `{collection.Name}`.",
                collectionSorted = "**nhitomi**: Updated collection sorting attribute to `{attribute}`.",

                addedToCollection = "**nhitomi**: Added `{doujin.Name}` to collection `{collection.Name}`.",
                removedFromCollection = "**nhitomi**: Removed `{doujin.Name}` from collection `{collection.Name`.",
                invalidCollectionSort = "**nhitomi**: Could not sort collection by attribute `{attribute}`. " +
                                        "Please refer to **{prefix}help**."
            },
            doujinMessage = new
            {
                language = "Language",
                group = "Group",
                parody = "Parody of",
                categories = "Categories",
                characters = "Characters",
                tags = "Tags",
                contents = "Contents",
                sourceIcons = new
                {
                    nhentai = "https://cdn.cybrhome.com/media/website/live/icon/icon_nhentai.net_57f740.png",
                    hitomi = "https://ltn.hitomi.la/favicon-160x160.png"
                }
            },
            downloadMessage = new
            {
                description = "Click the link above to start downloading `{doujin.Name}`."
            },
            helpMessage = new
            {
                title = "Help",
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
            }
        };
    }
}