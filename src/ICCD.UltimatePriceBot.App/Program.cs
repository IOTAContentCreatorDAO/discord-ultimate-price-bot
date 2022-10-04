// <copyright file="Program.cs" company="IOTA Content Creator DAO LLC">
// Copyright (c) IOTA Content Creator DAO LLC 2022. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ICCD.UltimatePriceBot.App.Services;
using ICCD.UltimatePriceBot.App.Services.PriceData;
using ICCD.UltimatePriceBot.App.Services.PriceData.Source;
using ICCD.UltimatePriceBot.App.Services.PriceData.Source.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace ICCD.UltimatePriceBot.App;

/// <summary>
/// Application Class.
/// </summary>
public sealed class Program
{
    private Program()
    {
    }

    /// <summary>
    /// Gets a value indicating whether the app environment is development.
    /// </summary>
    public static bool IsDevelopment => false;

    private static void Main()
        => new Program().MainAsync().GetAwaiter().GetResult();

    private static ServiceProvider ConfigureServices()
    {
        return new ServiceCollection()
            .AddSingleton(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent,
                AlwaysDownloadUsers = true,
            })
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton<CommandService>()
            .AddSingleton<CommandHandlingService>()
            .AddSingleton<HttpClient>()
            .AddSingleton<NameUpdateService>()
            .AddSingleton(typeof(IPriceDataSource), (x) =>
            {
                _ = uint.TryParse(Environment.GetEnvironmentVariable("DATA_SOURCE_ID"), out var dataSourceId);
                var apiKey = Environment.GetEnvironmentVariable("DATA_SOURCE_API_KEY");

                // CoinGecko
                if (dataSourceId == 0)
                {
                    return new CoinGeckoDataSource();
                }

                // CoinMarketCap
                else if (dataSourceId == 1)
                {
                    if (string.IsNullOrEmpty(apiKey))
                    {
                        throw new ApplicationException("API Key is not specified.");
                    }

                    return new CoinMarketCapDataSource(apiKey);
                }
                else
                {
                    throw new NotImplementedException($"Data Source with ID {dataSourceId} is not implemented.");
                }
            })
            .AddSingleton<PriceDataService>()
            .BuildServiceProvider();
    }

    private async Task MainAsync()
    {
        await using var services = ConfigureServices();
        var client = services.GetRequiredService<DiscordSocketClient>();

        client.Log += LogAsync;
        services.GetRequiredService<CommandService>().Log += LogAsync;

        await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("TOKEN"));
        await client.StartAsync();

        await services.GetRequiredService<CommandHandlingService>().InitializeAsync();

        await services.GetRequiredService<NameUpdateService>().StartAsync();

        await Task.Delay(Timeout.Infinite);
    }

    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log.ToString());

        return Task.CompletedTask;
    }
}