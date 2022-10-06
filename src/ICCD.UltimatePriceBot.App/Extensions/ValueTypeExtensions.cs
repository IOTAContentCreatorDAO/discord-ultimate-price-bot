// <copyright file="ValueTypeExtensions.cs" company="IOTA Content Creator DAO LLC">
// Copyright (c) IOTA Content Creator DAO LLC 2022. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

namespace ICCD.UltimatePriceBot.App.Extensions;

/// <summary>
/// Extensions for value types.
/// </summary>
public static class ValueTypeExtensions
{
    /// <summary>
    /// Converts a nullable uint to a display string.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The display string or "N/A" if conversion was not successful.</returns>
    public static string GetDisplayString(this uint? value)
    {
        return GetDisplayStringInternal((object?)value);
    }

    /// <summary>
    /// Converts a nullable float to a display string.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="format">The format string.</param>
    /// <returns>The display string or "N/A" if conversion was not successful.</returns>
    public static string GetDisplayString(this float? value, string? format = null)
    {
        return GetDisplayStringInternal((object?)value, format);
    }

    /// <summary>
    /// Converts a nullable decimal to a display string.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="format">The format string.</param>
    /// <returns>The display string or "N/A" if conversion was not successful.</returns>
    public static string GetDisplayString(this decimal? value, string? format = null)
    {
        return GetDisplayStringInternal((object?)value, format);
    }

    /// <summary>
    /// Converts a nullable double to a display string.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="format">The format string.</param>
    /// <returns>The display string or "N/A" if conversion was not successful.</returns>
    public static string GetDisplayString(this double? value, string? format = null)
    {
        return GetDisplayStringInternal((object?)value, format);
    }

    /// <summary>
    /// Converts a nullable DateTimeOffset to a display string.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The display string or "N/A" if conversion was not successful.</returns>
    public static string GetDisplayString(this DateTimeOffset? value)
    {
        return GetDisplayStringInternal((object?)value);
    }

    private static string GetDisplayStringInternal(this object? value, string? format = null)
    {
        if (value == null)
        {
            return "N/A";
        }

        Type? type;
        if (value.GetType().IsGenericType && (value.GetType().GetGenericTypeDefinition() == typeof(Nullable<>)))
        {
            type = Nullable.GetUnderlyingType(value.GetType());
        }
        else
        {
            type = value.GetType();
        }

        if (type == typeof(float) || type == typeof(decimal) || type == typeof(double))
        {
            return Convert.ToDecimal(value).ToString(format);
        }

        if (type == typeof(DateTimeOffset) && format == null)
        {
            return ((DateTimeOffset)value).LocalDateTime.ToUniversalTime().ToLongDateString();
        }

        return Convert.ToString(value)?.ToString() ?? "N/A";
    }
}