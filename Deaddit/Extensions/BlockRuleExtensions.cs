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
            //Add comment specific here if needed in the future
            if (thing is RedditPost rp && rule.IsAllowed(rp))
            {
                return false;
            }
            
            if(!BlockListHelper.IsAllowed(rule.Author, thing.Author, StringMatchType.String))
            {
                return false;
            }

            if (!BlockListHelper.IsAllowed(rule.Body, thing.Body, StringMatchType.Regex))
            {
                return false;
            }

            return true;
        }

        private static bool IsAllowed(this BlockRule rule, RedditPost post)
        {
            if(!BlockListHelper.IsAllowed(rule.IsLocked, post.IsLocked))
            {
                return false;
            }

            if(!BlockListHelper.IsAllowed(rule.IsArchived, post.IsArchived))
            {
                return false;
            }

            if(!BlockListHelper.IsAllowed(rule.Flair, post.LinkFlairText, StringMatchType.String))
            {
                return false;
            }

            if(!BlockListHelper.IsAllowed(rule.SubReddit, post.SubReddit, StringMatchType.String))
            {
                return false;
            }

            if(!BlockListHelper.IsAllowed(rule.Title, post.Title, StringMatchType.Regex))
            {
                return false;
            }

            return true;
        }
    }
}
