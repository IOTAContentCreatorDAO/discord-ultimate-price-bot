// <copyright file="DataProviderOptions.cs" company="IOTA Content Creator DAO LLC">
// Copyright (c) IOTA Content Creator DAO LLC 2022. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

namespace ICCD.UltimatePriceBot.App.Configuration;

/// <summary>
/// Data provider settings.
/// </summary>
public class DataProviderOptions
{
    /// <summary>
    /// The CoinGecko provider settings key.
    /// </summary>
    public const string CoinGecko = nameof(CoinGecko);

    /// <summary>
    /// The CoinMarketCap provider settings key.
    /// </summary>
    public const string CoinMarketCap = nameof(CoinMarketCap);

    /// <summary>
    /// Gets or sets a value indicating whether the provider is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the API key.
    /// </summary>
    public string? ApiKey { get; set; }
}