<!--
 Copyright (c) 2018-2019 chiya.dev

 This software is released under the MIT License.
 https://opensource.org/licenses/MIT
-->

# nhitomi ![Build status](https://ci.appveyor.com/api/projects/status/vtdjarua2c9i0k5t?svg=true)

![nhitomi](nhitomi.png)

nhitomi — a Discord bot for searching and downloading doujinshi by [chiya.dev](https://chiya.dev).

Join our [Discord server](https://discord.gg/JFNga7q) or [invite nhitomi](https://discordapp.com/oauth2/authorize?client_id=515386276543725568&scope=bot&permissions=347200) to your server.

## Commands

### Doujinshi

- n!get `source` `id` — Displays doujin information from a source by its ID.
- n!all `source` — Displays all doujins from a source uploaded recently.
- n!search `query` — Searches for doujins by the title and tags that satisfy your query.
- n!download `source` `id` — Sends a download link for a doujin by its ID.

### Tag subscriptions

- n!subscription — Lists all tags you are subscribed to.
- n!subscription add|remove `tag` — Adds or removes a tag subscription.
- n!subscription clear — Removes all tag subscriptions.

### Collection management

- n!collection — Lists all collections belonging to you.
- n!collection `name` — Displays doujins belonging to a collection.
- n!collection `name` add|remove `source` `id` — Adds or removes a doujin in a collection.
- n!collection `name` list — Lists all doujins belonging to a collection.
- n!collection `name` sort `attribute` — Sorts doujins in a collection by an attribute (`time`, `id`, `name`, `artist`).
- n!collection `name` delete — Deletes a collection, removing all doujins belonging to it.

Useful shortcuts to remember:
- `n!g` — `n!get`, `n!s` — `n!search`, `n!dl` — `n!download`
- `n!se`, `n!sj`, `n!sc` — `n!search` + `english`, `japanese`, `chinese`, respectively
- `source/id` — `n!get source id`, can specify multiple to show a list
- `n!sub` — `n!subscription`
- `n!c` — `n!collection`
- paste a doujin link

### Sources

- nhentai — `https://nhentai.net/`
- hitomi — `https://hitomi.la/`
- ~~tsumino — `https://tsumino.com/`~~
- ~~pururin — `https://pururin.io/`~~

## Running nhitomi

### Requirements

- [.NET Core 2.1 SDK](https://www.microsoft.com/net/learn/get-started) or higher.
- For development: a C# IDE with intellisense and syntax highlighting, such as [Visual Studio Code](https://code.visualstudio.com/) or [Jetbrains Rider](https://www.jetbrains.com/rider/).

## License

This project is licensed under the [MIT license](https://opensource.org/licenses/MIT). Please see the [license](LICENSE) for more information.
