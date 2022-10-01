// <copyright file="ValueTypeExtensions.cs" company="IOTA Content Creator DAO LLC">
// Copyright (c) IOTA Content Creator DAO LLC 2022. All rights reserved.
// Thanks to:
// Patrick -Pathin- Fischer (pfischer@daobee.org)
// Any illegal reproduction of this content will result in immediate legal action.
// </copyright>

namespace ICCD.UltimatePriceBot.App.Extensions
{
    /// <summary>
    /// Extensions for value types.
    /// </summary>
    public static class ValueTypeExtensions
    {
        public static string GetDisplayString(this uint? value)
        {
            return GetDisplayStringInternal((object?)value);
        }

        public static string GetDisplayString(this float? value, string format)
        {
            return GetDisplayStringInternal((object?)value, format);
        }

        public static string GetDisplayString(this decimal? value, string format)
        {
            return GetDisplayStringInternal((object?)value, format);
        }

        public static string GetDisplayString(this double? value, string format)
        {
            return GetDisplayStringInternal((object?)value, format);
        }

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
            if (value.GetType().IsGenericType && (value.GetType().GetGenericTypeDefinition() == typeof(Nullable<>))){
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
}