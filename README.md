# nhitomi

WIP Discord Bot for doujinshi

Join the [Discord server](https://discord.gg/bf3q7RM) or invite [nhitomi](https://discordapp.com/oauth2/authorize?client_id=515386276543725568&scope=bot&permissions=347200).

# Setup

Requirements:

- [.NET Core SDK 2.1](https://www.microsoft.com/net/learn/get-started) or higher.
- For development: a text editor or a C# IDE. [Visual Studio Code](https://code.visualstudio.com) is recommended to greatly simplify the development process.

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

# Licence

This project is licensed under the [MIT licence](https://opensource.org/licenses/MIT). Please see the [licence](LICENCE) for more information.
