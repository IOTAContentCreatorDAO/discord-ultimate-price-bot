// <copyright file="CommandPrefixAttribute.cs" company="IOTA Content Creator DAO LLC">
// Copyright (c) IOTA Content Creator DAO LLC 2022. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

namespace ICCD.UltimatePriceBot.App.Attributes;

/// <summary>
/// The command prefix.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public partial class CommandPrefixAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandPrefixAttribute"/> class.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    public CommandPrefixAttribute(CommandPrefixType commandType)
    {
        PrefixType = commandType;
    }

    /// <summary>
    /// Gets the command prefix.
    /// </summary>
    /// <returns>Null, if no prefix was specified.</returns>
    public string? Prefix { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether the prefix is a mention.
    /// </summary>
    public bool IsMentionPrefix => PrefixType == CommandPrefixType.Mention;

    /// <summary>
    /// Gets the command type.
    /// </summary>
    public CommandPrefixType PrefixType { get; }
}