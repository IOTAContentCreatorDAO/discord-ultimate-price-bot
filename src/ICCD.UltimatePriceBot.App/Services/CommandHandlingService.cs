// <copyright file="CommandHandlingService.cs" company="IOTA Content Creator DAO LLC">
// Copyright (c) IOTA Content Creator DAO LLC 2022. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ICCD.UltimatePriceBot.App.Configuration;
using ICCD.UltimatePriceBot.App.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ICCD.UltimatePriceBot.App.Services;

/// <summary>
/// Service handling the received text messages and executing the commands in the respective modules.
/// </summary>
public class CommandHandlingService
{
    private readonly CommandService _commands;
    private readonly DiscordSocketClient _discord;
    private readonly IServiceProvider _services;
    private readonly IOptionsMonitor<CommandOptions> _commandOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandHandlingService"/> class.
    /// </summary>
    /// <param name="services">The service provider.</param>
    /// <param name="commandOptions">The command options monitor.</param>
    public CommandHandlingService(IServiceProvider services, IOptionsMonitor<CommandOptions> commandOptions)
    {
        _commands = services.GetRequiredService<CommandService>();
        _discord = services.GetRequiredService<DiscordSocketClient>();
        _commandOptions = commandOptions;
        _services = services;

        _commands.CommandExecuted += CommandExecutedAsync;
        _discord.MessageReceived += MessageReceivedAsync;
    }

    /// <summary>
    /// Initialises the <see cref="CommandHandlingService"/>.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task InitializeAsync()
    {
        await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
    }

    /// <summary>
    /// Called when a message is received.
    /// </summary>
    /// <param name="rawMessage">The received message.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private async Task MessageReceivedAsync(SocketMessage rawMessage)
    {
        if (rawMessage is not SocketUserMessage message)
        {
            return;
        }

        if (message.Source != MessageSource.User)
        {
            return;
        }

        var argPos = 0;

        var context = new SocketCommandContext(_discord, message);

        await _commands.ExecuteAsync(context, GetRealCommandString(rawMessage.Content)[argPos..], _services);
    }

    private string GetRealCommandString(string message)
    {
        // If a command is specified via options
        var combinedPriceOptions = _commandOptions.Get(nameof(CommandOptions.PriceCombined));
        var iotaPriceOptions = _commandOptions.Get(nameof(CommandOptions.PriceIota));
        var shimmerPriceOptions = _commandOptions.Get(nameof(CommandOptions.PriceShimmer));

        var command = message.Split(' ')[0];

        if (combinedPriceOptions.Override && combinedPriceOptions.Commands.Contains(command))
        {
            var combinedPriceOptionsCommand = typeof(PriceModule).GetMethod(nameof(PriceModule.GetComboPriceAsync))?.GetCustomAttribute<CommandAttribute>()?.Text;
            if (combinedPriceOptionsCommand == null)
            {
                return message;
            }
            else
            {
                return combinedPriceOptionsCommand + message[command.Length..];
            }
        }

        if (iotaPriceOptions.Override && iotaPriceOptions.Commands.Contains(command))
        {
            var iotaPriceOptionsCommand = typeof(PriceModule).GetMethod(nameof(PriceModule.GetPriceForIotaTokenAsync))?.GetCustomAttribute<CommandAttribute>()?.Text;
            if (iotaPriceOptionsCommand == null)
            {
                return message;
            }
            else
            {
                return iotaPriceOptionsCommand + message[command.Length..];
            }
        }

        if (shimmerPriceOptions.Override && shimmerPriceOptions.Commands.Contains(command))
        {
            var shimmerPriceOptionsCommand = typeof(PriceModule).GetMethod(nameof(PriceModule.GetPriceForShimmerTokenAsync))?.GetCustomAttribute<CommandAttribute>()?.Text;
            if (shimmerPriceOptionsCommand == null)
            {
                return message;
            }
            else
            {
                return shimmerPriceOptionsCommand + message[command.Length..];
            }
        }

        return command;
    }

    private async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
    {
        if (!command.IsSpecified)
        {
            return;
        }

        if (result.IsSuccess)
        {
            return;
        }
        else
        {
            if (Program.IsDevelopment)
            {
                Console.WriteLine($"Error: {result.ErrorReason}");
                await context.Channel.SendMessageAsync(embed: new EmbedBuilder().WithTitle("Error").WithDescription(result.ErrorReason).Build());
            }
        }
    }
}