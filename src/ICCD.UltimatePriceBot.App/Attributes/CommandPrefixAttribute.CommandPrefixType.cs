// <copyright file="CommandPrefixAttribute.CommandPrefixType.cs" company="IOTA Content Creator DAO LLC">
// Copyright (c) IOTA Content Creator DAO LLC 2022. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

namespace ICCD.UltimatePriceBot.App.Attributes;

/// <content />
public partial class CommandPrefixAttribute
{
    /// <summary>
    /// Command prefix type attribute.
    /// </summary>
    public enum CommandPrefixType
    {
        /// <summary>
        /// No prefix.
        /// </summary>
        None,

        /// <summary>
        /// The prefix is a mention.
        /// </summary>
        Mention,

        /// <summary>
        /// The prefix is a text.
        /// </summary>
        Text,
    }
}