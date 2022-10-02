// <copyright file="ConvertibleExtensions.cs" company="IOTA Content Creator DAO LLC">
// Copyright (c) IOTA Content Creator DAO LLC 2022. All rights reserved.
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

namespace ICCD.UltimatePriceBot.App.Extensions;

/// <summary>
/// Converter extensions.
/// </summary>
public static class ConvertibleExtensions
{
    /// <summary>
    /// Converts one type to another.
    /// </summary>
    /// <param name="obj">The object to convert.</param>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <returns>The converted value or null.</returns>
    public static T? ConvertTo<T>(this IConvertible obj)
    {
        var t = typeof(T);

        if (t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(Nullable<>)))
        {
            if (obj == null)
            {
                return (T?)(object?)null;
            }
            else
            {
                return (T)Convert.ChangeType(obj, Nullable.GetUnderlyingType(t)!);
            }
        }
        else
        {
            return (T)Convert.ChangeType(obj, t);
        }
    }
}