<!--
 Copyright (c) 2018-2019 phosphene47

 This software is released under the MIT License.
 https://opensource.org/licenses/MIT
-->

# nhitomi ![Build status](https://ci.appveyor.com/api/projects/status/vtdjarua2c9i0k5t?svg=true)

![nhitomi](nhitomi.png)

nhitomi — a Discord bot for searching and downloading doujinshi.

Join our [Discord server](https://discord.gg/JFNga7q) or invite [nhitomi](https://discordapp.com/oauth2/authorize?client_id=515386276543725568&scope=bot&permissions=347200) to your server.

### Commands

- **n!get** source id — Retrieves doujin information from the specified source.
- **n!all** source — Displays all doujins from the specified source uploaded recently.
- **n!search** query — Searches for doujins by the title and tags across the supported sources that match the specified query.
- **n!download** source id — Sends a download link for the specified doujin.
- **n!help** — Shows the help message.

Useful shortcuts to remember:
- `n!s` — `n!search`
- `n!se`, `n!sj`, `n!sc` — `n!search` + `english`, `japanese`, `chinese`, respectively
- `n!dl` — `n!download`
- `source/id` — `n!get source id`, can specify multiple to show a list
- or seriously just paste a doujin link and see if nhitomi can detect it

### Sources

- nhentai — `https://nhentai.net/`
- hitomi — `https://hitomi.la/` search is broken due to recent changes
- ~~tsumino — `https://tsumino.com/`~~ disabled until they provide us an official API.
- ~~pururin — `https://pururin.io/`~~ disabled because it's not working

## Running nhitomi

### Requirements

TODO: This section needs to be updated.

- [.NET Core 2.1 SDK](https://www.microsoft.com/net/learn/get-started) or higher.
- For development: a C# IDE with intellisense and syntax highlighting, such as [Visual Studio Code](https://code.visualstudio.com/) or [Jetbrains Rider](https://www.jetbrains.com/rider/).

### Building

Create a file named `appsecrets.json` alongside `appsettings.json`. This file was intentionally omitted from source control. Then paste the following code, replacing the token string with your own.

```json
{
  "discord": {
    "token": "..."
  }
}
```

Then run the following commands:

1. `dotnet restore` — resolves NuGet dependencies.
2. `dotnet build` — builds the bot.
3. `dotnet run` — runs the bot.

## License

This project is licensed under the [MIT license](https://opensource.org/licenses/MIT). Please see the [license](LICENSE) for more information.
