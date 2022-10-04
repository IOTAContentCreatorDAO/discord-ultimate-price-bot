// <copyright file="PriceDataService.cs" company="IOTA Content Creator DAO LLC">
// Copyright (c) IOTA Content Creator DAO LLC 2022. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

using ICCD.UltimatePriceBot.App.Models;
using ICCD.UltimatePriceBot.App.Services.PriceData.Source;

namespace ICCD.UltimatePriceBot.App.Services.PriceData;

/// <summary>
/// Gets price data from CoinGecko.
/// </summary>
public class PriceDataService
{
    private readonly Dictionary<string, PriceDataViewModel> _cache = new();

    private readonly Dictionary<string, SourceTokenInfo> _tokenLookupTable = new(StringComparer.InvariantCultureIgnoreCase);

    private readonly ReentrantAsyncLock.ReentrantAsyncLock _getPriceLock = new();

    private readonly IPriceDataSource _priceDataSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="PriceDataService"/> class.
    /// </summary>
    /// <param name="priceDataSource">The price data source implementation.</param>
    public PriceDataService(IPriceDataSource priceDataSource)
    {
        _priceDataSource = priceDataSource;
        Init().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets the price data for a token.
    /// </summary>
    /// <param name="tokenKey">The token key (id, name, symbol or slug).</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<PriceDataViewModel> GetPriceDataAsync(string tokenKey)
    {
        await using (await _getPriceLock.LockAsync(CancellationToken.None))
        {
            if (!TokenExists(tokenKey))
            {
                throw new ArgumentException("Token not found.", nameof(tokenKey));
            }

            var tokenInfoObj = _tokenLookupTable[tokenKey];

            if (_cache.TryGetValue(tokenInfoObj.Id, out var cachedEntry))
            {
                if (DateTime.Now - cachedEntry.CreatedDate < TimeSpan.FromSeconds(60))
                {
                    return cachedEntry;
                }
            }

            var data = await _priceDataSource.GetPriceForTokenAsync(tokenInfoObj);
            var viewModel = new PriceDataViewModel(data);

            if (tokenInfoObj.Symbol.Equals("iota", StringComparison.InvariantCultureIgnoreCase) || tokenInfoObj.Symbol.Equals("miota", StringComparison.InvariantCultureIgnoreCase) || tokenInfoObj.Symbol.Equals("smr", StringComparison.InvariantCultureIgnoreCase))
            {
                string otherTokenName;
                if (tokenInfoObj.Symbol.Equals("iota", StringComparison.InvariantCultureIgnoreCase) || tokenInfoObj.Symbol.Equals("miota", StringComparison.InvariantCultureIgnoreCase))
                {
                    otherTokenName = "smr";
                }
                else
                {
                    otherTokenName = "miota";
                }

                var otherTokenInfoObj = _tokenLookupTable[otherTokenName];

                var other = new PriceDataViewModel(await _priceDataSource.GetPriceForTokenAsync(otherTokenInfoObj));
                viewModel.Relations.Add(other.Symbol.ToUpperInvariant(), viewModel.GetRelationValueTo(other));
            }

            _cache[tokenInfoObj.Id] = viewModel;

            return viewModel;
        }
    }

    /// <summary>
    /// Checks wheter or not a token exists.
    /// </summary>
    /// <param name="tokenName">The token name.</param>
    /// <returns>Whether a token exists or not.</returns>
    public bool TokenExists(string tokenName) => _tokenLookupTable.ContainsKey(tokenName);

    /// <summary>
    /// Gets a token ID by name.
    /// </summary>
    /// <param name="tokenName">The token name.</param>
    /// <returns>The token ID.</returns>
    public string GetTokenId(string tokenName)
    {
        if (!TokenExists(tokenName))
        {
            throw new ArgumentException("The token doesn't exist.", nameof(tokenName));
        }

        return _tokenLookupTable[tokenName].Id;
    }

    private async Task Init()
    {
        var allTokenInfos = await _priceDataSource.GetTokensAsync();
        for (var i = 0; i < 4; i++)
        {
            foreach (var tokenInfo in allTokenInfos)
            {
                if (i == 3 && tokenInfo.Slug == null)
                {
                    continue;
                }

                _ = i switch
                {
                    0 => _tokenLookupTable.TryAdd(tokenInfo.Id, tokenInfo),
                    1 => _tokenLookupTable.TryAdd(tokenInfo.Symbol, tokenInfo),
                    2 => _tokenLookupTable.TryAdd(tokenInfo.Name, tokenInfo),
                    3 => _tokenLookupTable.TryAdd(tokenInfo.Slug!, tokenInfo),
                    _ => throw new NotImplementedException(),
                };
            }
        }
    }
}