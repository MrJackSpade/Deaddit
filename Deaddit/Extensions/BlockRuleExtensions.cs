using Deaddit.Configurations;
using Deaddit.Models.Json.Response;
using System.Text.RegularExpressions;

namespace Deaddit.Extensions
{
    public static class BlockRuleExtensions
    {
        public static bool IsBlocked(this IEnumerable<BlockRule> blockRules, RedditThing thing)
        {
            foreach (BlockRule br in blockRules)
            {
                if (br.IsBlocked(thing))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsBlocked(this BlockRule rule, RedditThing thing)
        {
            if(thing is RedditComment rc && rule.IsBlocked(rc))
            {
                return true;
            }

            if (thing is RedditPost rp && rule.IsBlocked(rp))
            {
                return true;
            }
            
            if (string.Equals(rule.Author, thing.Author, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(rule.Body) && Regex.IsMatch(thing.Body, rule.Body, RegexOptions.IgnoreCase))
            {
                return true;
            }

            return false;
        }

        private static bool IsBlocked(this BlockRule rule, RedditPost post)
        {
            if (rule.BlockType == BlockType.Comment)
            {
                return false;
            }

            if (rule.IsLocked && post.Locked == true)
            {
                return true;
            }

            if (rule.IsArchived && post.Archived == true)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(rule.Flair) && string.Equals(rule.Flair, post.LinkFlairText, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(rule.SubReddit, post.Subreddit, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(rule.Title) && Regex.IsMatch(post.Title, rule.Title, RegexOptions.IgnoreCase))
            {
                return true;
            }

            return false;
        }

        private static bool IsBlocked(this BlockRule rule, RedditComment comment)
        {
            if (rule.BlockType == BlockType.Post)
            {
                return false;
            }

            return false;
        }
    }
}
