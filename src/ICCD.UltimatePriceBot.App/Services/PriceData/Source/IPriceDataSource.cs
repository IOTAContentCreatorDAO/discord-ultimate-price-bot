// <copyright file="IPriceDataSource.cs" company="IOTA Content Creator DAO LLC">
// Copyright (c) IOTA Content Creator DAO LLC 2022. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

namespace ICCD.UltimatePriceBot.App.Services.PriceData.Source;

/// <summary>
/// Interface for the price data source implementations.
/// </summary>
public interface IPriceDataSource
{
    /// <summary>
    /// Gets the source name.
    /// For example CoinGecko or CoinMarketCap.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets all of the endpoint's available tokens.
    /// </summary>
    /// <returns>A list of endpoint available tokens.</returns>
    public Task<ICollection<SourceTokenInfo>> GetTokensAsync();

    /// <summary>
    /// Gets the price data for a token.
    /// </summary>
    /// <param name="tokenInfo">The token info.</param>
    /// <param name="priceCurrency">The currency to get the prices in. Default = USD.</param>
    /// <returns>The token's price data.</returns>
    public Task<TokenPriceData> GetPriceForTokenAsync(SourceTokenInfo tokenInfo, string priceCurrency = "USD");
}