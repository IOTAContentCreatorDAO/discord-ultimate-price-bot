# Ultimate Token Price Bot

A simple discord bot to display the price of crypto tokens, written in C#.

## **Running the bot**
Docker is the only supported way to deploy the bot.
### Requirements
- [docker](https://docs.docker.com/engine/install/)
- [docker compose](https://docs.docker.com/compose/install/)

### How to run
1. Clone this repository recursively: ``git clone --recursive --depth 1 https://github.com/icc-dao/discord-ultimate-price-bot``
2. Create an ``appsettings.json`` file in the bot's root directory, you can find and example in the bot's root directory or here: [appsettings.Example.json](https://raw.githubusercontent.com/icc-dao/discord-ultimate-price-bot/main/appsettings.Example.json).
3. Configure ``appsettings.json`` to fit your needs.
4. Build and run the bot by issuing ``docker-compose up -d --build`` in the bot's root directory.

### How to stop
1. To stop the bot and issue ``docker-compose down`` in the bot's root directory.

### How to update
1. Update your git repository: ``git pull``
2. Build and run the bot again: ``docker-compose up -d --build``

## **Developing the bot**
### Requirements
- Visual Studio Code
- dotnet SDK 6.0

### Working on the project
1. Open the code-workspace file with Visual Studio Code
2. Install the recommended plugins
3. Work on
4. ???
5. Profit

Use ``appsettings.Development.json`` to set development specific overrides.  
**Please help us to make the developer experience work in other IDE's, it is greatly appreciated!**

## Caveats, Known Issues, and Limitations
- The bot's error handling needs a bit of work, logging is not implemented and the bot might just crash on configuration error. Make sure your Discord bot token and price data source API key is correct.
- The bot's price requesting implementation needs some work and it might trigger the CoinMarketCap API limit very fast, do use CoinGecko if you expect frequent price requests.
- The price cache and command cooldown is currently hardcoded, if you need to change it, you can find the respective lines in ``PriceService.cs`` and ``PriceModule.cs``
- The bot currently uses the [Discord.Net Text Command Service](https://discordnet.dev/guides/text_commands/intro.html), which is fine, however it does not support SlashCommand or interactions, so we might need to implement the [Interaction Framework](https://discordnet.dev/guides/int_framework/intro.html).

**Please submit a pull request to fix any of the issues!**