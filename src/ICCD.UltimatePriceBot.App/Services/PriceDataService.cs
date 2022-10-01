// <copyright file="PriceDataService.cs" company="IOTA Content Creators DAO LLC">
// Copyright (c) IOTA Content Creators DAO LLC 2022. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

using System.Reflection.Metadata.Ecma335;
using CoinGecko.Clients;
using ICCD.UltimatePriceBot.App.Extensions;
using ICCD.UltimatePriceBot.App.Models;
using Newtonsoft.Json;

namespace ICCD.UltimatePriceBot.App.Services;

/// <summary>
/// Gets price data from CoinGecko.
/// </summary>
public class PriceDataService
{
    private readonly CoinsClient _client;

    private readonly Dictionary<string, PriceDataViewModel> _cache = new();

    private readonly Dictionary<string, string> _tokenLookupTable = new(StringComparer.InvariantCultureIgnoreCase);

    private ReentrantAsyncLock.ReentrantAsyncLock _getPriceLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="PriceDataService"/> class.
    /// </summary>
    public PriceDataService()
    {
        _client = new CoinsClient(new HttpClient(), new JsonSerializerSettings());
        Init().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets the price data for a token.
    /// </summary>
    /// <param name="tokenId">The token ID.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<PriceDataViewModel> GetPriceDataAsync(string tokenId)
    {
        await using (await _getPriceLock.LockAsync(CancellationToken.None))
        {
            if (!_tokenLookupTable.ContainsKey(tokenId))
            {
                throw new ArgumentException("Token not found.", nameof(tokenId));
            }

            tokenId = _tokenLookupTable[tokenId];

            if (_cache.TryGetValue(tokenId, out var cachedEntry))
            {
                if (DateTime.Now - cachedEntry.CreatedDate < TimeSpan.FromSeconds(30))
                {
                    return cachedEntry;
                }
            }

            var data = await _client.GetAllCoinDataWithId(tokenId);
            var viewModel = new PriceDataViewModel(data);

            if (tokenId.Equals("iota") || tokenId.Equals("shimmer"))
            {
                string otherString;
                if (tokenId.Equals("iota"))
                {
                    otherString = "shimmer";
                }
                else
                {
                    otherString = "iota";
                }

                var other = new PriceDataViewModel(await _client.GetAllCoinDataWithId(otherString));
                viewModel.Relations.Add(other.Symbol.ToUpperInvariant(), viewModel.GetRelationValueTo(other));
            }

            _cache[tokenId] = viewModel;

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

        return _tokenLookupTable[tokenName];
    }

    private async Task Init()
    {
        _tokenLookupTable.Add("s", "shimmer");
        _tokenLookupTable.Add("shimmie", "shimmer");
        _tokenLookupTable.Add("i", "iota");

        var allCoins = await _client.GetCoinList();
        for (var i = 0; i < 3; i++)
        {
            foreach (var coin in allCoins)
            {
                _ = i switch
                {
                    0 => _tokenLookupTable.TryAdd(coin.Id, coin.Id),
                    1 => _tokenLookupTable.TryAdd(coin.Symbol, coin.Id),
                    2 => _tokenLookupTable.TryAdd(coin.Name, coin.Id),
                    _ => throw new NotImplementedException(),
                };
            }
        }
    }
}