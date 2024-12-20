﻿using Reddit.Api.Exceptions;
using System.Text.RegularExpressions;

namespace Deaddit.Core.Utils.Blocking
{
    public enum StringMatchType
    {
        String,

        Regex
    }

    public static class BlockListHelper
    {
        public static bool TriggersOrSkip(bool ruleValue, bool? checkValue)
        {
            if (!ruleValue)
            {
                return true;
            }

            return checkValue == true;
        }

        public static bool TriggersOrSkip(bool ruleValue, bool checkValue)
        {
            if (!ruleValue)
            {
                return true;
            }

            return checkValue;
        }

        public static bool TriggersOrSkip(string? ruleValue, string? checkValue, StringMatchType matchType, bool partial = false, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
        {
            if (string.IsNullOrWhiteSpace(ruleValue))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(checkValue))
            {
                return true;
            }

            if (matchType == StringMatchType.Regex)
            {
                ruleValue = ruleValue.Trim('/');
            }

            return matchType switch
            {
                StringMatchType.String => partial ? checkValue.Contains(ruleValue, stringComparison)
                                                  : string.Equals(ruleValue, checkValue, stringComparison),
                StringMatchType.Regex => stringComparison switch
                {
                    StringComparison.OrdinalIgnoreCase => Regex.IsMatch(checkValue, ruleValue, RegexOptions.IgnoreCase),
                    StringComparison.Ordinal => Regex.IsMatch(ruleValue, ruleValue),
                    _ => throw new EnumNotImplementedException(stringComparison),
                },
                _ => throw new EnumNotImplementedException(matchType),
            };
        }
    }
}