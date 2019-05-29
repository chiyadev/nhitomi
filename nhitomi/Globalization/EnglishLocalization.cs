using System.Globalization;

namespace nhitomi.Globalization
{
    public class EnglishLocalization : Localization
    {
        protected override CultureInfo Culture { get; } = new CultureInfo("en");
        protected override CultureInfo FallbackCulture => null;

        protected override object CreateDefinition() => new
        {
            meta = new
            {
                translators = new[]
                {
                    "phosphene47"
                }
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
                langage = "Language",
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
                about = "a Discord bot for searching and downloading doujinshi, by **chiya.dev**.",
                invite = "Official server: <{invite}>",
                doujin = new
                {
                    heading = "Doujinshi",
                    get = "Displays doujin information from a source by its ID.",
                    from = "Displays all doujins from a source.",
                    search = "Searches for doujins that match your query.",
                    download = "Sends a download link for a doujin."
                },
                collection = new
                {
                    heading = "Collection Management",
                    list = "Lists all collections you own.",
                    view = "Displays the doujins in a collection.",
                    addRemove = "Adds or removes a doujin in a collection.",
                    sort = "Sorts a collection by a doujin's attribute.",
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