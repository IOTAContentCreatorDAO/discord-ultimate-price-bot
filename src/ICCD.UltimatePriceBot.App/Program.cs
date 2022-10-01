// <copyright file="Program.cs" company="IOTA Content Creator DAO LLC">
// Copyright (c) IOTA Content Creator DAO LLC 2022. All rights reserved.
// Thanks to:
// Patrick -Pathin- Fischer (pfischer@daobee.org)
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ICCD.UltimatePriceBot.App.Services;
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

    private static void Main()
        => new Program().MainAsync().GetAwaiter().GetResult();

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

    private ServiceProvider ConfigureServices()
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
            .AddSingleton<PriceDataService>()
            .BuildServiceProvider();
    }
}