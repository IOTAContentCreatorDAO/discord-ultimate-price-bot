using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ICCD.UltimatePriceBot.App.Extensions
{
    public static class StringExtensions
    {
        public static string Truncate(this string value, int maxChars, string suffix = "…")
        {
            return value.Length <= maxChars ? value : value[..maxChars] + suffix;
        }
    }
}