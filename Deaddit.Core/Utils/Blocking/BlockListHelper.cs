using Deaddit.Core.Reddit.Exceptions;
using System.Text.RegularExpressions;

namespace Deaddit.Core.Utils.Blocking
{
    public enum StringMatchType
    {
        String,

        Regex
    }

    /// <summary>
    /// Low-level matching helpers used by BlockRuleExtensions.
    ///
    /// Each method returns a TriggerState:
    ///   - Skip:    Rule field was not configured (null/empty/false) — don't participate in matching.
    ///   - Match:   Rule field was configured and the thing's value matched it.
    ///   - NoMatch: Rule field was configured and the thing's value did NOT match.
    ///
    /// These get fed into MatchResult.Apply() which implements the AND logic.
    /// </summary>
    public static partial class BlockListHelper
    {
        /// <summary>
        /// Boolean overload (nullable). If the rule doesn't require this flag (ruleValue=false),
        /// skip it. Otherwise check if the thing's value matches.
        /// Used for: IsNsfw, IsLocked, IsArchived.
        /// </summary>
        public static TriggerState TriggersOrSkip(bool ruleValue, bool? checkValue)
        {
            // Rule doesn't care about this flag — skip
            if (!ruleValue)
            {
                return TriggerState.Skip;
            }

            return checkValue == true ? TriggerState.Match : TriggerState.NoMatch;
        }

        /// <summary>
        /// Boolean overload (non-nullable). Same as above but for non-nullable thing values.
        /// </summary>
        public static TriggerState TriggersOrSkip(bool ruleValue, bool checkValue)
        {
            if (!ruleValue)
            {
                return TriggerState.Skip;
            }

            return checkValue ? TriggerState.Match : TriggerState.NoMatch;
        }

        /// <summary>
        /// String overload — the workhorse. Compares a rule's string field against a thing's value.
        ///
        /// If ruleValue is null/empty → Skip (field not configured on rule).
        /// If checkValue is null/empty → NoMatch (rule wants something, thing has nothing).
        ///
        /// Matching modes:
        ///   - String + partial=false: exact match (case-insensitive). Used for flair, domain, subreddit.
        ///   - String + partial=true:  substring match (case-insensitive). Used for title, body.
        ///   - Regex:  regex pattern match. The /slashes/ are stripped before matching.
        ///             Used when the user wraps their rule value in /slashes/.
        /// </summary>
        public static TriggerState TriggersOrSkip(string? ruleValue, string? checkValue, StringMatchType matchType, bool partial = false, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
        {
            // Rule field is empty — this field doesn't participate in matching
            if (string.IsNullOrWhiteSpace(ruleValue))
            {
                return TriggerState.Skip;
            }

            // Rule wants to match something but the thing has no value for this field
            if (string.IsNullOrWhiteSpace(checkValue))
            {
                return TriggerState.NoMatch;
            }

            // Strip the /slashes/ wrapper from regex patterns before matching
            if (matchType == StringMatchType.Regex)
            {
                ruleValue = ruleValue.Trim('/');
            }

            switch (matchType)
            {
                case StringMatchType.Regex:
                    return stringComparison switch
                    {
                        StringComparison.OrdinalIgnoreCase => Regex.IsMatch(checkValue, ruleValue) ? TriggerState.Match : TriggerState.NoMatch,
                        StringComparison.Ordinal => Regex.IsMatch(ruleValue, ruleValue) ? TriggerState.Match : TriggerState.NoMatch,
                        _ => throw new EnumNotImplementedException(stringComparison),
                    };

                case StringMatchType.String:
                    bool match;
                    if (partial)
                    {
                        // Substring search — "foo" matches "contains foo here"
                        match = checkValue.Contains(ruleValue, stringComparison);
                    }
                    else
                    {
                        // Exact match — "foo" only matches "foo", not "foobar"
                        match = string.Equals(ruleValue, checkValue, stringComparison);
                    }

                    return match ? TriggerState.Match : TriggerState.NoMatch;

                default:
                    throw new EnumNotImplementedException(matchType);
            }
        }
    }
}