using System.Globalization;

namespace nhitomi.Localization
{
    public class LocalizationEnglish : Localization
    {
        public override CultureInfo Culture => new CultureInfo("en");

        public override LocalizationDictionary Dictionary { get; } = new LocalizationDictionary(new
        {
            meta = new
            {
                translators = new[]
                {
                    "phosphene47"
                }
            },
            doujinMessage = new
            {
                langage = "Language",
                parodyOf = "Parody of",
                categories = "Categories",
                characters = "Characters",
                tags = "Tags",
                Contents = "Contents"
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
        });
    }
}