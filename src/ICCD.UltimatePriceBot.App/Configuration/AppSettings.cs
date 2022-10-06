// <copyright file="AppSettings.cs" company="IOTA Content Creator DAO LLC">
// Copyright (c) IOTA Content Creator DAO LLC 2022. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

namespace ICCD.UltimatePriceBot.App.Configuration;

/// <summary>
/// Application Settings.
/// </summary>
public partial class AppSettings
{
    /// <summary>
    /// Gets or sets the development discord guild ID.
    /// </summary>
    public ulong? DevelopmentGuildId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable the name update service.
    /// </summary>
    public bool EnableNameUpdateService { get; set; }

    /// <summary>
    /// Gets or sets the discord bot token.
    /// </summary>
    public string? BotToken { get; set; }
}