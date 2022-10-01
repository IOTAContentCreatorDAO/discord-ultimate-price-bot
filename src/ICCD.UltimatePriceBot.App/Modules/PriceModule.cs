// <copyright file="PriceModule.cs" company="IOTA Content Creator DAO LLC">
// Copyright (c) IOTA Content Creator DAO LLC 2022. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

using System.Linq.Expressions;
using System.Reflection.Emit;
using Discord;
using Discord.Commands;
using Discord.Rest;
using ICCD.UltimatePriceBot.App.Services;
using Newtonsoft.Json;

namespace ICCD.UltimatePriceBot.App.Modules;

/// <summary>
/// p.
/// </summary>
public class PriceModule : ModuleBase<SocketCommandContext>
{
    private const int _var = 0;
    private static readonly Dictionary<string, DateTime> _lastPriceRequests = new();
    private static readonly Dictionary<string, ICollection<string>> _aliases = new() {
        { "iota", new string[] { "p", "pi", "rice", "🍚" } },
        { "shimmer", new string[] { "pp", "ps", "sushi", "🍣" } },
    };

    private readonly PriceDataService _priceDataService;
    private readonly NameUpdateService _nameUpdateService;
    private string? _tokenOverride;
    private bool _ignorePriceRequestLimit;
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

    [Command("pi", true)]
    [Alias("p", "pp", "pi", "price", "rice", "🍚")]
    public async Task GetPriceForIotaTokenAsync()
    {
        await GetPriceForTokenAsync("iota");
    }

    [Command("ps")]
    [Alias("ps", "sushi", "🍣")]
    public async Task GetPriceForShimmerTokenAsync()
    {
        await GetPriceForTokenAsync("shimmer");
    }

    /// <summary>
    /// p.
    /// </summary>
    /// <param name="tokenName">Token to request the price for.</param>
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
            _ = await ReplyAsync($"<@{Context.User.Id}>, please provide a token name.");
            return;
        }

        var options = new RequestOptions
        {
            Timeout = 50,
        };

        if (_tokenOverride != null)
        {
            tokenName = _tokenOverride;
        }

        string tokenId;
        if (tokenName == null)
        {
            tokenId = "iotasmr";
        }
        else
        {
            if (!_priceDataService.TokenExists(tokenName))
            {
                _lastPriceRequests.TryGetValue(tokenName, out var lastPriceRequest);
                if (DateTime.Now - lastPriceRequest < TimeSpan.FromSeconds(60) && !_ignorePriceRequestLimit)
                {
                    _ = Context.Message.AddReactionAsync(!Context.Message.Author.Id.Equals(189498611690766336) ? new Emoji("😡") : new Emoji("❤️"), options);
                    return;
                }

                _lastPriceRequests[tokenName] = DateTime.Now;
                _ = ReplyAsync($"<@{Context.User.Id}>", embed: new EmbedBuilder().WithTitle("Unknown Token").WithDescription("Could not find data for the requested token.").WithCurrentTimestamp().WithColor(Color.Red).Build());
                return;
            }

            tokenId = _priceDataService.GetTokenId(tokenName);
        }

        _lastPriceRequests.TryGetValue(tokenId, out var lastPriceRequest2);

        if (DateTime.Now - lastPriceRequest2 < TimeSpan.FromSeconds(30) && !_ignorePriceRequestLimit)
        {
            if (!Context.Message.Author.Id.Equals(189498611690766336))
            {
                await Context.Message.AddReactionAsync(new Emoji("😡"), options);
            }
            else
            {
                await Context.Message.AddReactionAsync(new Emoji("❤️"), options);
            }

            return;
        }

        if (!_ignorePriceRequestLimit)
        {
            _lastPriceRequests[tokenId] = DateTime.Now;
        }

        // token is "iotasmr" let's display a concise combo embed.
        if (tokenName == "iotasmr")
        {
            _ignorePriceRequestLimit = true;
            await GetPriceForTokenAsync("iota");
            await GetPriceForTokenAsync("shimmer");
            return;
        }

        var priceData = await _priceDataService.GetPriceDataAsync(tokenName);

        _ = ReplyAsync(embed: priceData.ToEmbed());

        if (Context.Message.Author.Id.Equals(189498611690766336))
        {
            _ = Context.Message.AddReactionAsync(new Emoji("❤️"), options);
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