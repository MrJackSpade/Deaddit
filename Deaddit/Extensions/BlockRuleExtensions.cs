using Deaddit.Configurations;
using Deaddit.Models.Json.Response;
using Deaddit.Utils;
using System.Text.RegularExpressions;

namespace Deaddit.Extensions
{
    public static class BlockRuleExtensions
    {
        public static bool IsAllowed(this IEnumerable<BlockRule> blockRules, RedditThing thing)
        {
            foreach (BlockRule br in blockRules)
            {
                if (!br.IsAllowed(thing))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsAllowed(this BlockRule rule, RedditThing thing)
        {
            bool blocked = true;

            //Add comment specific here if needed in the future
            blocked &= thing is RedditPost rp && !rule.IsAllowed(rp);
            blocked &= BlockListHelper.TriggersOrSkip(rule.Author, thing.Author, StringMatchType.String);
            blocked &= BlockListHelper.TriggersOrSkip(rule.Body, thing.Body, StringMatchType.Regex);

            return !blocked;
        }

        private static bool IsAllowed(this BlockRule rule, RedditPost post)
        {
            bool blocked = true;

            blocked &= BlockListHelper.TriggersOrSkip(rule.IsLocked, post.IsLocked);

            blocked &= BlockListHelper.TriggersOrSkip(rule.IsArchived, post.IsArchived);

            blocked &= BlockListHelper.TriggersOrSkip(rule.Flair, post.LinkFlairText, StringMatchType.String);

            blocked &= BlockListHelper.TriggersOrSkip(rule.SubReddit, post.SubReddit, StringMatchType.String);

            blocked &= BlockListHelper.TriggersOrSkip(rule.Title, post.Title, StringMatchType.Regex);

            return !blocked;
        }
    }
}
