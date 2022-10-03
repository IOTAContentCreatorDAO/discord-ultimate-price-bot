// <copyright file="FunModule.cs" company="IOTA Content Creator DAO LLC">
// Copyright (c) IOTA Content Creator DAO LLC 2022. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

using System.Text.RegularExpressions;
using Discord;
using Discord.Commands;
using ICCD.UltimatePriceBot.App.Attributes;
using ICCD.UltimatePriceBot.App.Extensions;

namespace ICCD.UltimatePriceBot.App.Modules;

/// <summary>
/// A bunch of fun commands, also acts as test module.
/// </summary>
[CommandPrefix(CommandPrefixAttribute.CommandPrefixType.Text)]
[RequireContext(ContextType.Guild)]
public class FunModule : ModuleBase<SocketCommandContext>
{
    private static readonly HashSet<string> _validComplimentAdjectives = new(StringComparer.InvariantCultureIgnoreCase) { "Sexy", "Cute", "Good" };

    /// <summary>
    /// Get a compliment from the bot.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [CommandPrefix(CommandPrefixAttribute.CommandPrefixType.None)]
    [Command("good", true)]
    [Alias("cute", "sexy")]
    public async Task ComplimentBotAsync()
    {
        if (Context.Message.ReferencedMessage == null || !Context.Message.ReferencedMessage.Author.Id.Equals(Context.Client.CurrentUser.Id))
        {
            return;
        }

        var msg = Regex.Replace(Context.Message.Content.Trim(), @"\s+", " ");
        var msgSplt = msg.Split(' ');
        var adjective = msgSplt[0];
        var subject = msgSplt.Length > 1 ? msgSplt[1].Replace("!", string.Empty) : null;

        if (subject != null && subject.Equals("bot", StringComparison.InvariantCultureIgnoreCase))
        {
            _validComplimentAdjectives.TryGetValue(adjective, out var actualAdjective);
            if (actualAdjective == null)
            {
                return;
            }

            var response = $"{actualAdjective} Human!";
            if (actualAdjective.Equals("Good"))
            {
                response += " 😊";
            }

            if (actualAdjective.Equals("Cute"))
            {
                response += " ❤️";
            }

            if (actualAdjective.Equals("Sexy"))
            {
                response += " 😈";
            }

            await ReplyAsync(response, messageReference: Context.Message.ToReference());

            return;
        }
    }
}