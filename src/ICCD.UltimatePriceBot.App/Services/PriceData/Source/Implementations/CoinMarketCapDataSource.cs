// <copyright file="CoinMarketCapDataSource.cs" company="IOTA Content Creator DAO LLC">
// Copyright (c) IOTA Content Creator DAO LLC 2022. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoinMarketCap;
using CoinMarketCap.Models;
using CoinMarketCap.Models.Cryptocurrency;
using ICCD.UltimatePriceBot.App.Extensions;

namespace ICCD.UltimatePriceBot.App.Services.PriceData.Source.Implementations
{
    /// <summary>
    /// Implementation of <see cref="IPriceDataSource" /> for CoinMarketCap.
    /// </summary>
    public class CoinMarketCapDataSource : IPriceDataSource
    {
        private readonly Dictionary<string, List<Tuple<DateTime, decimal>>> _priceDataList = new();

        private readonly CoinMarketCapClient _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="CoinMarketCapDataSource"/> class.
        /// </summary>
        /// <param name="apiKey">The CMC API key.</param>
        public CoinMarketCapDataSource(string apiKey)
        {
            _client = new CoinMarketCapClient(apiKey);
        }

        /// <inheritdoc/>
        public string Name => "CoinMarketCap";

        /// <inheritdoc/>
        public string? Link => "https://coinmarketcap.com";

        /// <inheritdoc/>
        public async Task<TokenPriceData> GetPriceForTokenAsync(SourceTokenInfo tokenInfo, string priceCurrency = "USD")
        {
            var currencyKey = priceCurrency.ToUpperInvariant();
            var latestQuote = await _client.GetLatestQuoteAsync(new LatestQuoteParameters() { Id = Convert.ToInt32(tokenInfo.Id), Convert = priceCurrency }, CancellationToken.None);
            if (latestQuote == null || latestQuote.Data == null || !latestQuote.Data.TryGetValue(tokenInfo.Id.ToString(), out var quoteInfo) || !quoteInfo.Quote.TryGetValue(currencyKey, out var priceQuote))
            {
                throw new ApplicationException("Could not get token quote.");
            }

            if (!_priceDataList.ContainsKey(tokenInfo.Id))
            {
                _priceDataList[tokenInfo.Id] = new List<Tuple<DateTime, decimal>>();
            }

            if (priceQuote?.Price != null)
            {
                _priceDataList[tokenInfo.Id].Add(new Tuple<DateTime, decimal>(DateTime.Now, priceQuote.Price.ConvertTo<decimal>()));
            }

            var twentyFourHourPrices = _priceDataList[tokenInfo.Id].Where(x => x.Item1 >= DateTime.Now - TimeSpan.FromDays(1)).ToList();
            var res = new TokenPriceData(tokenInfo, priceCurrency, this)
            {
                MarketCapRank = quoteInfo?.CmcRank?.ConvertTo<uint>(),
                CurrentPrice = priceQuote?.Price?.ConvertTo<decimal>(),
                MarketCap = priceQuote?.MarketCap,
                PriceChangePercentage1Hour = priceQuote?.PercentChange1H,
                PriceChangePercentage24Hours = priceQuote?.PercentChange24H,
                TotalVolume = priceQuote?.Volume24H,
                HighestPrice24H = twentyFourHourPrices.Count > 0 ? twentyFourHourPrices.Max(x => x.Item2) : null,
                LowestPrice24H = twentyFourHourPrices.Count > 0 ? twentyFourHourPrices.Min(x => x.Item2) : null,
            };

            return res;
        }

        /// <inheritdoc/>
        public string? GetTokenLink(SourceTokenInfo tokenInfo) => tokenInfo.Slug != null ? string.Format(Link + "/currencies/{0}", tokenInfo.Slug) : null;

        /// <inheritdoc/>
        public async Task<ICollection<SourceTokenInfo>> GetTokensAsync()
        {
            var res = new List<SourceTokenInfo>();
            var cryptoCurrencyIdMap = await _client.GetCryptocurrencyIdMapAsync(new IdMapParameters(), CancellationToken.None);
            if (cryptoCurrencyIdMap == null || cryptoCurrencyIdMap.Data == null)
            {
                throw new ApplicationException("Could not get token list.");
            }

            foreach (var cryptoCurrencyIdMapEntry in cryptoCurrencyIdMap.Data)
            {
                res.Add(new SourceTokenInfo(cryptoCurrencyIdMapEntry.Id.ToString(), cryptoCurrencyIdMapEntry.Name, cryptoCurrencyIdMapEntry.Symbol, cryptoCurrencyIdMapEntry.Slug));
            }

            return res;
        }
    }
}