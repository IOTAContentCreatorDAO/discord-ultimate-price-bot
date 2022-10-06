// <copyright file="CommandOptions.cs" company="IOTA Content Creator DAO LLC">
// Copyright (c) IOTA Content Creator DAO LLC 2022. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

namespace ICCD.UltimatePriceBot.App.Configuration;

/// <summary>
/// Commands settings.
/// </summary>
public class CommandOptions
{
    /// <summary>
    /// The Shimmer command setting key.
    /// </summary>
    public const string PriceShimmer = nameof(PriceShimmer);

    /// <summary>
    /// The IOTA command setting key.
    /// </summary>
    public const string PriceIota = nameof(PriceIota);

    /// <summary>
    /// The Combo command setting key.
    /// </summary>
    public const string PriceCombined = nameof(PriceCombined);

    /// <summary>
    /// Gets or sets a value indicating whether to override the command defaults.
    /// </summary>
    public bool Override { get; set; }

    /// <summary>
    /// Gets or sets the overriden chat command strings.
    /// </summary>
    public ICollection<string> Commands { get; set; } = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

    /// <summary>
    /// Gets or sets a value indicating whether to show the command output concise.
    /// </summary>
    public bool Concise { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to show relations.
    /// </summary>
    public bool ShowRelations { get; set; } = true;

    /// <summary>
    /// Gets or sets the overriden relation assets.
    /// </summary>
    public ICollection<string> RelationsOverride { get; set; } = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
}