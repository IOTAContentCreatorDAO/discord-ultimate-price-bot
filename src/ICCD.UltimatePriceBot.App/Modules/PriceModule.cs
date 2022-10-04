// <copyright file="PriceModule.cs" company="IOTA Content Creator DAO LLC">
// Copyright (c) IOTA Content Creator DAO LLC 2022. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

using System.Linq.Expressions;
using System.Reflection.Emit;
using Discord;
using Discord.Commands;
using Discord.Rest;
using ICCD.UltimatePriceBot.App.Extensions;
using ICCD.UltimatePriceBot.App.Services;
using ICCD.UltimatePriceBot.App.Services.PriceData;
using Newtonsoft.Json;

namespace ICCD.UltimatePriceBot.App.Modules;

/// <summary>
/// p.
/// </summary>
public class PriceModule : ModuleBase<SocketCommandContext>
{
    private static readonly Dictionary<string, DateTime> _lastPriceRequests = new(StringComparer.InvariantCultureIgnoreCase);
    private readonly PriceDataService _priceDataService;
    private readonly NameUpdateService _nameUpdateService;
    private bool _skip;

    /// <summary>
    /// Initializes a new instance of the <see cref="PriceModule"/> class.
    /// </summary>
    /// <param name="nameUpdateService">Bot name update service.</param>
    /// <param name="priceDataService">Price data service.</param>
    public PriceModule(PriceDataService priceDataService, NameUpdateService nameUpdateService)
    {
        _priceDataService = priceDataService;
        _nameUpdateService = nameUpdateService;
    }

    /// <summary>
    /// Command to start or top the bot Name Update Service.
    /// </summary>
    /// <param name="verb">Either "start" or "stop".</param>
    /// <exception cref="ArgumentException">Thrown when verb is neither "start" nor "stop".</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RequireContext(ContextType.DM)]
    [Command("!NameUpdate")]
    public async Task ManageNameUpdateService(string verb)
    {
        if (!Context.User.Id.Equals(189498611690766336))
        {
            return;
        }

        switch (verb)
        {
            case "start":
                await _nameUpdateService.StartAsync();
                break;
            case "stop":
                await _nameUpdateService.StopAsync();
                break;
            default:
                return;
        }
    }

    /// <summary>
    /// Gets the price for IOTA.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Command("pi", true)]
    [Alias("pi", "price", "rice", "🍚")]
    public async Task GetPriceForIotaTokenAsync()
    {
        await GetPriceForTokenAsync("miota");
    }

    /// <summary>
    /// Gets the price for IOTA and shimmer (concise).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Command("p")]
    public async Task GetComboPriceAsync()
    {
        await GetPriceForTokenAsync("iotasmr");
    }

    /// <summary>
    /// Gets the price for shimmer.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Command("ps")]
    [Alias("pp", "ps", "sushi", "🍣")]
    public async Task GetPriceForShimmerTokenAsync()
    {
        await GetPriceForTokenAsync("smr");
    }

    /// <summary>
    /// Gets the price of a specific token.
    /// </summary>
    /// <param name="tokenName">Token to request the price for.</param>
    /// <param name="concise">Whether the bot response should be concise.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Command("!price", true)]
    public async Task GetPriceForTokenAsync(string? tokenName = null, bool concise = false)
    {
        if (_skip || Context.Channel.GetChannelType() != ChannelType.Text)
        {
            return;
        }

        if (string.IsNullOrEmpty(tokenName))
        {
            _ = ReplyAsync($"Please provide a token name.", messageReference: Context.Message.ToReference());
            _ = Context.Message.AddReactionAsync(Emoji.Parse("❌"));
            return;
        }

        try
        {
            var options = new RequestOptions
            {
                Timeout = 50,
            };

            if (!_priceDataService.TokenExists(tokenName) && !tokenName.Equals("iotasmr", StringComparison.InvariantCultureIgnoreCase))
            {
                _lastPriceRequests[tokenName] = DateTime.Now;
                _ = ReplyAsync(embed: new EmbedBuilder().WithTitle("Unknown Token").WithDescription("Could not find data for the requested token.").WithCurrentTimestamp().WithColor(Color.Red).Build(), messageReference: Context.Message.ToReference());
                _ = Context.Message.AddReactionAsync(Emoji.Parse("❌"));
                return;
            }

            string tokenId;
            if (tokenName.Equals("iotasmr", StringComparison.InvariantCultureIgnoreCase))
            {
                tokenId = "iotasmr";
            }
            else
            {
                tokenId = _priceDataService.GetTokenId(tokenName);
            }

            _lastPriceRequests.TryGetValue(tokenId, out var lastPriceRequest2);

            if (DateTime.Now - lastPriceRequest2 < TimeSpan.FromSeconds(30))
            {
                await Context.Message.AddReactionAsync(new Emoji("😡"), options);
                return;
            }

            _lastPriceRequests[tokenId] = DateTime.Now;

            if (Context.Message.Author.Id.Equals(189498611690766336))
            {
                await Context.Message.AddReactionAsync(new Emoji("❤️"), options);
            }
            else
            {
                await Context.Message.AddReactionAsync(Emoji.Parse("✅"));
            }

            if (tokenId == "iotasmr")
            {
                var priceIota = await _priceDataService.GetPriceDataAsync("iota");
                var priceSmr = await _priceDataService.GetPriceDataAsync("smr");
                _ = ReplyAsync(embed: priceIota.ToConciseEmbed(priceSmr), messageReference: Context.Message.ToReference());
            }
            else
            {
                var priceData = await _priceDataService.GetPriceDataAsync(tokenName);
                var embed = concise ? priceData.ToConciseEmbed() : priceData.ToEmbed();
                _ = ReplyAsync(embed: embed, messageReference: Context.Message.ToReference());
            }
        }
        catch (Exception)
        {
            await Context.Message.RemoveReactionAsync(Emoji.Parse("✅"), Context.Client.CurrentUser);
            await Context.Message.AddReactionAsync(Emoji.Parse("❌"));
            throw;
        }
    }

    /// <inheritdoc/>
    protected override Task BeforeExecuteAsync(CommandInfo command)
    {
        if (command.Name.Equals("ps") || command.Name.Equals("pi"))
        {
            if (!Context.Message.Content.Trim().Equals(command.Name, StringComparison.InvariantCultureIgnoreCase) && !command.Aliases.Any(x => x.Equals(Context.Message.Content.Trim(), StringComparison.InvariantCultureIgnoreCase)))
            {
                _skip = true;
            }
        }

        return base.BeforeExecuteAsync(command);
    }
}