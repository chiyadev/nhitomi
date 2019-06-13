using System.Globalization;

namespace nhitomi.Globalization
{
    public class EnglishLocalization : Localization
    {
        public override CultureInfo Culture { get; } = new CultureInfo("en");
        protected override CultureInfo FallbackCulture => null;

        protected override object CreateDefinition() => new
        {
            messages = new
            {
                doujinNotFound = "**nhitomi**: No such doujin!",
                invalidQuery = "**nhitomi**: `{query}` is not valid.",
                joinForDownload = "**nhitomi**: Please join our server to enable downloading! <{invite}>",

                listLoading = "**nhitomi**: Loading...",
                listBeginning = "**nhitomi**: Beginning of the list!",
                listEnd = "**nhitomi**: End of the list!",

                collectionNotFound = "**nhitomi**: No such collection!",
                collectionDeleted = "**nhitomi**: Deleted collection `{collection.Name}`.",
                collectionSorted = "**nhitomi**: Sorted collection `{collection.Name}` by `{attribute}`.",

                addedToCollection = "**nhitomi**: Added `{doujin.OriginalName}` to `{collection.Name}`.",
                removedFromCollection = "**nhitomi**: Removed `{doujin.OriginalName}` from `{collection.Name}`.",
                alreadyInCollection = "**nhitomi**: `{doujin.OriginalName}` already exists in `{collection.Name}`.",
                notInCollection = "**nhitomi**: `{doujin.OriginalName}` was not found in `{collection.Name}`.",

                localizationChanged = "**nhitomi**: Guild language is set to `{localization.Culture.NativeName}`!",
                localizationNotFound = "**nhitomi**: Language `{language}` is not supported.",

                qualityFilterChanged = "**nhitomi**: Search quality filter is now `{state:enabled|disabled}` " +
                                       "by default.",

                commandInvokeNotInGuild = "**nhitomi**: You can only use this command in a guild.",
                notGuildAdmin = "**nhitomi**: You must be a guild administrator to use this command.",

                tagNotFound = "**nhitomi**: `{tag}` was not found.",
                feedTagAdded = "**nhitomi**: `#{channel.Name}` is now subscribed to `{tag}`.",
                feedTagAlreadyAdded = "**nhitomi**: `#{channel.Name}` is already subscribed to `{tag}`.",
                feedTagRemoved = "**nhitomi**: `#{channel.Name}` is now unsubscribed from `{tag}`.",
                feedTagNotRemoved = "**nhitomi**: `#{channel.Name}` is not subscribed to `{tag}`.",
                feedModeChanged = "**nhitomi**: Changed feed mode of `#{channel.Name}` to `{type}`."
            },
            doujinMessage = new
            {
                title = "{doujin.OriginalName}",
                footer = "{doujin.Source}/{doujin.SourceId}",
                language = "Language",
                group = "Group",
                parody = "Parody of",
                categories = "Categories",
                characters = "Characters",
                tags = "Tags",
                contents = "Contents",
                contentsValue = "{doujin.PageCount} pages",
                emptyList = new
                {
                    title = "**nhitomi**: No doujins",
                    text = "There are no doujins in this list."
                }
            },
            doujinReadMessage = new
            {
                title = "{doujin.OriginalName}",
                text = "You are reading page {page} of {doujin.PageCount}.",
                footer = "{doujin.Source}/{doujin.SourceId}"
            },
            downloadMessage = new
            {
                title = "{doujin.OriginalName}",
                text = "Click the link above to start downloading `{doujin.OriginalName}`."
            },
            collectionMessage = new
            {
                title = "**{context.User.Username}**: {collection.Name}",
                emptyCollection = "Empty collection",
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
                contentsValue = "{collection.Doujins.Count} doujins",
                empty = new
                {
                    title = "**{context.User.Username}**: No collections",
                    text = "You have no collections."
                }
            },
            helpMessage = new
            {
                title = "**nhitomi**: Help",
                footer = "v{version} {codename} — powered by chiya.dev",
                about = "a Discord bot for searching and downloading doujinshi",
                invite = "Official server: <{invite}>",
                doujins = new
                {
                    heading = "Doujinshi",
                    get = "Displays full doujin information.",
                    from = "Displays all doujins from a source.",
                    read = "Shows the pages in a doujin.",
                    download = "Sends the download link for a doujin.",
                    search = "Searches for doujins by their title and tags."
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
                options = new
                {
                    heading = "Options",
                    language = "Changes the interface language within a guild.",
                    filter = "Toggles the search quality filter.",
                    feed = new
                    {
                        add = "Subscribes a channel to the given tag.",
                        remove = "Unsubscribes a channel from the given tag.",
                        mode = "Changes the tag whitelist mode of a channel between `any` and `all`."
                    }
                },
                aliases = new
                {
                    heading = "Command Aliases"
                },
                sources = new
                {
                    heading = "Sources"
                },
                openSource = new
                {
                    heading = "Open Source",
                    license = "This project is licensed under the MIT License.",
                    contribution = "Contributions are welcome!\n<{repoUrl}>"
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