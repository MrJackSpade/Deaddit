﻿using System.Text.RegularExpressions;

namespace Deaddit.Utils
{
    public enum StringMatchType
    {
        String,
        Regex
    }

    public static class BlockListHelper
    {
        public static bool IsAllowed(bool ruleValue, bool? checkValue)
        {
            if (!ruleValue)
            {
                return true;
            }

            return checkValue != true;
        }

        public static bool IsAllowed(bool ruleValue, bool checkValue)
        {
            if (!ruleValue)
            {
                return true;
            }

            return !checkValue;
        }

        public static bool IsAllowed(string? ruleValue, string? checkValue, StringMatchType matchType, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
        {
            if (string.IsNullOrWhiteSpace(ruleValue))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(checkValue))
            {
                return true;
            }

            return matchType switch
            {
                StringMatchType.Regex => !string.Equals(ruleValue, checkValue, stringComparison),
                StringMatchType.String => stringComparison switch
                {
                    StringComparison.OrdinalIgnoreCase => !Regex.IsMatch(checkValue, ruleValue, RegexOptions.IgnoreCase),
                    StringComparison.Ordinal => !Regex.IsMatch(ruleValue, ruleValue),
                    _ => throw new NotImplementedException(),
                },
                _ => throw new NotImplementedException(),
            };
        }
    }
}
