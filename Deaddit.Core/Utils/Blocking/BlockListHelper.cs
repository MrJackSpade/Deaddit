using Reddit.Api.Exceptions;
using System.Text.RegularExpressions;

namespace Deaddit.Core.Utils.Blocking
{
    public enum StringMatchType
    {
        String,

        Regex
    }

    public static partial class BlockListHelper
    {
        public static TriggerState TriggersOrSkip(bool ruleValue, bool? checkValue)
        {
            if (!ruleValue)
            {
                return TriggerState.Skip;
            }

            return checkValue == true ? TriggerState.Match : TriggerState.NoMatch;
        }

        public static TriggerState TriggersOrSkip(bool ruleValue, bool checkValue)
        {
            if (!ruleValue)
            {
                return TriggerState.Skip;
            }

            return checkValue ? TriggerState.Match : TriggerState.NoMatch;
        }

        public static TriggerState TriggersOrSkip(string? ruleValue, string? checkValue, StringMatchType matchType, bool partial = false, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
        {
            if (string.IsNullOrWhiteSpace(ruleValue))
            {
                return TriggerState.Skip;
            }

            if (string.IsNullOrWhiteSpace(checkValue))
            {
                return TriggerState.NoMatch;
            }

            if (matchType == StringMatchType.Regex)
            {
                ruleValue = ruleValue.Trim('/');
            }

            switch(matchType)
            {
                case StringMatchType.Regex:
                    switch (stringComparison)
                    {
                        case StringComparison.OrdinalIgnoreCase:
                            return Regex.IsMatch(checkValue, ruleValue, RegexOptions.IgnoreCase) ? TriggerState.Match : TriggerState.NoMatch;
                        case StringComparison.Ordinal:
                            return Regex.IsMatch(ruleValue, ruleValue) ? TriggerState.Match : TriggerState.NoMatch;
                        default:
                            throw new EnumNotImplementedException(stringComparison);
                    }
                case StringMatchType.String: 
                    bool match;
                    if (partial)
                    {
                        match = checkValue.Contains(ruleValue, stringComparison);
                    } else
                    {
                        match = string.Equals(ruleValue, checkValue, stringComparison);
                    }

                    return match ? TriggerState.Match : TriggerState.NoMatch;
                default:
                    throw new EnumNotImplementedException(matchType);
            }
        }
    }
}