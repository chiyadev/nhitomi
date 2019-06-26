using System.Globalization;

namespace nhitomi.Globalization
{
    public class EnglishLocalization : Localization
    {
        public override CultureInfo Culture { get; } = new CultureInfo("en");
        protected override CultureInfo FallbackCulture => null;

        protected override object CreateDefinition() => new
        {
            doujinNotFound = "No such doujin!",
            invalidQuery = "`{query}` is not valid.",

            listLoading = "Loading...",
            listBeginning = "Beginning of the list!",
            listEnd = "End of the list!",

            collectionNotFound = "No collection named `{name}`.",
            collectionDeleted = "Deleted collection `{collection.Name}`.",
            collectionSorted = "Sorted collection `{collection.Name}` by `{attribute}`.",

            addedToCollection = "Added `{doujin.OriginalName}` to `{collection.Name}`.",
            removedFromCollection = "Removed `{doujin.OriginalName}` from `{collection.Name}`.",
            alreadyInCollection = "`{doujin.OriginalName}` already exists in `{collection.Name}`.",
            notInCollection = "`{doujin.OriginalName}` was not found in `{collection.Name}`.",

            localizationChanged = "Server language is set to `{localization.Culture.NativeName}`!",
            localizationNotFound = "Language `{language}` is not supported.",

            qualityFilterChanged = "Search quality filter is now `{state:enabled|disabled}` by default.",

            commandInvokeNotInGuild = "You can only use this command in a server.",
            notGuildAdmin = "You must be a server administrator to use this command.",

            tagNotFound = "Tag `{tag}` was not found.",
            feedTagAdded = "`#{channel.Name}` is now subscribed to `{tag}`.",
            feedTagAlreadyAdded = "`#{channel.Name}` is already subscribed to `{tag}`.",
            feedTagRemoved = "`#{channel.Name}` is now unsubscribed from `{tag}`.",
            feedTagNotRemoved = "`#{channel.Name}` is not subscribed to `{tag}`.",
            feedModeChanged = "Changed feed mode of `#{channel.Name}` to `{type}`.",

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
                    title = "No doujins",
                    text = "There are no doujins in this list."
                }
            },

            doujinReadMessage = new
            {
                title = "{doujin.OriginalName}",
                text = "Page {page} of {doujin.PageCount}",
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
                title = "Help",
                footer = "v{version} {codename} — powered by chiya.dev",
                about = "a Discord bot for searching and downloading doujinshi",
                invite = "[Join]({guildInvite}) the official server or [invite nhitomi]({botInvite}) to your server.",
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
                    list = "Shows the collections you own.",
                    view = "Shows the doujins in a collection.",
                    add = "Adds a doujin to a collection.",
                    remove = "Removes a doujin from a collection.",
                    sort = "Sorts a collection by a doujin attribute.",
                    delete = "Deletes a collection, removing all doujins in it."
                },
                options = new
                {
                    heading = "Options",
                    language = "Changes the interface language within a server.",
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
                examples = new
                {
                    heading = "Examples",
                    doujins = "Finding doujins",
                    collections = "Managing collections",
                    language = "Setting the interface language"
                },
                sources = new
                {
                    heading = "Sources"
                },
                languages = new
                {
                    heading = "Languages"
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
                title = "Error",
                titleAuto = "Automatic error report",

                text = "The error has been reported. " +
                       "For further assistance, please join the [official server]({invite}).",

                missingPerms = "Could not respond in that channel due to the lack of permissions."
            }
        };
    }
}