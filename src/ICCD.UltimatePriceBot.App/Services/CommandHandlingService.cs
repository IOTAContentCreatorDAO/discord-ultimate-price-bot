// <copyright file="CommandHandlingService.cs" company="IOTA Content Creator DAO LLC">
// Copyright (c) IOTA Content Creator DAO LLC 2022. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

using System.Collections.Immutable;
using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace ICCD.UltimatePriceBot.App.Services;

/// <summary>
/// Service handling the received text messages and executing the commands in the respective modules.
/// </summary>
public class CommandHandlingService
{
    private readonly CommandService _commands;
    private readonly DiscordSocketClient _discord;
    private readonly IServiceProvider _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandHandlingService"/> class.
    /// </summary>
    /// <param name="services">The service provider.</param>
    public CommandHandlingService(IServiceProvider services)
    {
        _commands = services.GetRequiredService<CommandService>();
        _discord = services.GetRequiredService<DiscordSocketClient>();
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

        await _commands.ExecuteAsync(context, argPos, _services);
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