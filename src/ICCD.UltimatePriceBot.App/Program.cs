// <copyright file="Program.cs" company="IOTA Content Creator DAO LLC">
// Copyright (c) IOTA Content Creator DAO LLC 2022. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

using System.Data;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ICCD.UltimatePriceBot.App.Configuration;
using ICCD.UltimatePriceBot.App.Services;
using ICCD.UltimatePriceBot.App.Services.PriceData;
using ICCD.UltimatePriceBot.App.Services.PriceData.Source;
using ICCD.UltimatePriceBot.App.Services.PriceData.Source.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ICCD.UltimatePriceBot.App;

/// <summary>
/// Application Class.
/// </summary>
public sealed class Program
{
    private static string _appEnvironment = default!;

    private static bool? _isDevelopment;

    private readonly IConfiguration _configuration;

    private Program()
    {
        _appEnvironment = Environment.GetEnvironmentVariable("APP_ENVIRONMENT") ?? "Production";

        _configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", true)
            .AddJsonFile($"appsettings.{_appEnvironment}.json", true, true)
            .AddEnvironmentVariables("SETTING_")
            .Build();
    }

    /// <summary>
    /// Gets a value indicating whether the app is in development mode.
    /// </summary>
    public static bool IsDevelopment
    {
        get
        {
            if (_isDevelopment == null)
            {
                _isDevelopment = _appEnvironment.Equals("Development", StringComparison.InvariantCultureIgnoreCase);
            }

            return _isDevelopment.Value;
        }
    }

    private static void Main()
        => new Program().MainAsync().GetAwaiter().GetResult();

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
            .AddSingleton(typeof(IPriceDataSource), (x) =>
            {
                var dataProviderOptions = x.GetRequiredService<IOptionsSnapshot<DataProviderOptions>>();
                var coinGeckoOptions = dataProviderOptions.Get(DataProviderOptions.CoinGecko);
                var coinMarketCapOptions = dataProviderOptions.Get(DataProviderOptions.CoinMarketCap);

                if (coinGeckoOptions.Enabled)
                {
                    return new CoinGeckoDataSource();
                }
                else if (coinMarketCapOptions.Enabled)
                {
                    if (string.IsNullOrEmpty(coinMarketCapOptions.ApiKey))
                    {
                        throw new ApplicationException("API Key for CoinMarketCap is not specified, please specify an API key.");
                    }

                    return new CoinMarketCapDataSource(coinMarketCapOptions.ApiKey);
                }
                else
                {
                    throw new NotImplementedException($"No enabled data provider found. Please enable a DataProvider in the application settings.");
                }
            })
            .AddSingleton<PriceDataService>()
            .AddSingleton(_configuration)
            .AddOptions()
            .Configure<AppSettings>(_configuration)
            .Configure<CommandOptions>(CommandOptions.PriceCombined, _configuration.GetSection($"Commands:{CommandOptions.PriceCombined}"))
            .Configure<CommandOptions>(CommandOptions.PriceIota, _configuration.GetSection($"Commands:{CommandOptions.PriceIota}"))
            .Configure<CommandOptions>(CommandOptions.PriceShimmer, _configuration.GetSection($"Commands:{CommandOptions.PriceShimmer}"))
            .Configure<DataProviderOptions>(DataProviderOptions.CoinGecko, _configuration.GetSection($"DataProviders:{DataProviderOptions.CoinGecko}"))
            .Configure<DataProviderOptions>(DataProviderOptions.CoinMarketCap, _configuration.GetSection($"DataProviders:{DataProviderOptions.CoinMarketCap}"))
            .BuildServiceProvider();
    }

    private async Task MainAsync()
    {
        await using var services = ConfigureServices();
        var client = services.GetRequiredService<DiscordSocketClient>();

        client.Log += LogAsync;
        services.GetRequiredService<CommandService>().Log += LogAsync;

        await client.LoginAsync(TokenType.Bot, _configuration.GetValue<string>("BotToken"));
        await client.StartAsync();

        await services.GetRequiredService<CommandHandlingService>().InitializeAsync();

        if (_configuration.GetValue<bool>("EnableNameUpdateService"))
        {
            await services.GetRequiredService<NameUpdateService>().StartAsync();
        }

        await Task.Delay(Timeout.Infinite);
    }

    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log.ToString());

        return Task.CompletedTask;
    }
}