// <copyright file="PriceModule.cs" company="IOTA Content Creator DAO LLC">
// Copyright (c) IOTA Content Creator DAO LLC 2022. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using Discord;
using Discord.Commands;
using Discord.Rest;
using ICCD.UltimatePriceBot.App.Configuration;
using ICCD.UltimatePriceBot.App.Extensions;
using ICCD.UltimatePriceBot.App.Services;
using ICCD.UltimatePriceBot.App.Services.PriceData;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace ICCD.UltimatePriceBot.App.Modules;

/// <summary>
/// p.
/// </summary>
public class PriceModule : ModuleBase<SocketCommandContext>
{
    private static readonly Dictionary<string, DateTime> _lastPriceRequests = new(StringComparer.InvariantCultureIgnoreCase);
    private readonly string? _priceShimmerCommand = typeof(PriceModule).GetMethod(nameof(PriceModule.GetPriceForShimmerTokenAsync))?.GetCustomAttribute<CommandAttribute>()?.Text;
    private readonly string? _priceIotaCommand = typeof(PriceModule).GetMethod(nameof(PriceModule.GetPriceForIotaTokenAsync))?.GetCustomAttribute<CommandAttribute>()?.Text;
    private readonly string? _priceCombinedCommand = typeof(PriceModule).GetMethod(nameof(PriceModule.GetComboPriceAsync))?.GetCustomAttribute<CommandAttribute>()?.Text;
    private readonly PriceDataService _priceDataService;
    private readonly NameUpdateService _nameUpdateService;
    private readonly IOptionsMonitor<CommandOptions> _commandOptions;
    private bool _skip;

    /// <summary>
    /// Initializes a new instance of the <see cref="PriceModule"/> class.
    /// </summary>
    /// <param name="nameUpdateService">Bot name update service.</param>
    /// <param name="priceDataService">Price data service.</param>
    /// <param name="commandOptions">Command options service.</param>
    public PriceModule(PriceDataService priceDataService, NameUpdateService nameUpdateService, IOptionsMonitor<CommandOptions> commandOptions)
    {
        _priceDataService = priceDataService;
        _nameUpdateService = nameUpdateService;
        _commandOptions = commandOptions;
    }

    /// <summary>
    /// Gets the price for IOTA.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Command("pi", true)]
    [Alias("price", "rice", "🍚")]
    public async Task GetPriceForIotaTokenAsync()
    {
        var iotaPriceOptions = _commandOptions.Get(nameof(CommandOptions.PriceIota));
        if (iotaPriceOptions.Override)
        {
            await GetPriceForTokenAsync("miota", iotaPriceOptions.Concise, iotaPriceOptions.ShowRelations ? iotaPriceOptions.RelationsOverride.ToArray() : Array.Empty<string>());
        }
        else
        {
            await GetPriceForTokenAsync("miota", relations: "smr");
        }
    }

    /// <summary>
    /// Gets the price for IOTA and shimmer (concise).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Command("p")]
    public async Task GetComboPriceAsync()
    {
        var combinedPriceOptions = _commandOptions.Get(nameof(CommandOptions.PriceCombined));

        if (combinedPriceOptions.Override)
        {
            await GetPriceForTokenAsync("iotasmr", true, combinedPriceOptions.ShowRelations ? combinedPriceOptions.RelationsOverride.ToArray() : Array.Empty<string>());
        }
        else
        {
            await GetPriceForTokenAsync("iotasmr", true, "iota", "smr");
        }
    }

    /// <summary>
    /// Gets the price for shimmer.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Command("ps")]
    [Alias("pp", "ps", "sushi", "🍣")]
    public async Task GetPriceForShimmerTokenAsync()
    {
        var shimmerPriceOptions = _commandOptions.Get(nameof(CommandOptions.PriceShimmer));
        if (shimmerPriceOptions.Override)
        {
            await GetPriceForTokenAsync("smr", shimmerPriceOptions.Concise, shimmerPriceOptions.ShowRelations ? shimmerPriceOptions.RelationsOverride.ToArray() : Array.Empty<string>());
        }
        else
        {
            await GetPriceForTokenAsync("smr", relations: "miota");
        }
    }

    /// <summary>
    /// Gets the price of a specific token.
    /// </summary>
    /// <param name="tokenName">Token to request the price for.</param>
    /// <param name="concise">Whether the bot response should be concise.</param>
    /// <param name="relations">Relations to display to other tokens.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Command("!price", true)]
    public async Task GetPriceForTokenAsync(string? tokenName = null, bool concise = false, params string[] relations)
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
                var priceIota = await _priceDataService.GetPriceDataAsync("iota", relations);
                var priceSmr = await _priceDataService.GetPriceDataAsync("smr", relations);
                _ = ReplyAsync(embed: priceIota.ToConciseEmbed(priceSmr), messageReference: Context.Message.ToReference());
            }
            else
            {
                var priceData = await _priceDataService.GetPriceDataAsync(tokenName, relations);
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
        // Only allow explicit command for PriceCombo, PriceIota, PriceShimmer
        if ((_priceShimmerCommand != null && command.Name.Equals(_priceShimmerCommand)) ||
            (_priceIotaCommand != null && command.Name.Equals(_priceIotaCommand)) ||
            (_priceCombinedCommand != null && command.Name.Equals(_priceCombinedCommand)))
        {
            var combinedPriceOptions = _commandOptions.Get(nameof(CommandOptions.PriceCombined));
            var iotaPriceOptions = _commandOptions.Get(nameof(CommandOptions.PriceIota));
            var shimmerPriceOptions = _commandOptions.Get(nameof(CommandOptions.PriceShimmer));

            var allCommands = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            if (combinedPriceOptions.Override)
            {
                foreach (var overrideCommand in combinedPriceOptions.Commands)
                {
                    if (!allCommands.Contains(overrideCommand))
                    {
                        allCommands.Add(overrideCommand);
                    }
                }
            }

            if (iotaPriceOptions.Override)
            {
                foreach (var overrideCommand in iotaPriceOptions.Commands)
                {
                    if (!allCommands.Contains(overrideCommand))
                    {
                        allCommands.Add(overrideCommand);
                    }
                }
            }

            if (shimmerPriceOptions.Override)
            {
                foreach (var overrideCommand in shimmerPriceOptions.Commands)
                {
                    if (!allCommands.Contains(overrideCommand))
                    {
                        allCommands.Add(overrideCommand);
                    }
                }
            }

            if (!Context.Message.Content.Trim().Equals(command.Name, StringComparison.InvariantCultureIgnoreCase) && !allCommands.Contains(Context.Message.Content.Trim()) && !command.Aliases.Any(x => x.Equals(Context.Message.Content.Trim(), StringComparison.InvariantCultureIgnoreCase)))
            {
                _skip = true;
            }
        }

        return base.BeforeExecuteAsync(command);
    }
}