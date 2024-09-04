﻿using Deaddit.Configurations.Models;
using Deaddit.Reddit.Models.Api;
using Deaddit.Utils;

namespace Deaddit.Extensions
{
    public static class BlockRuleExtensions
    {
        public static bool IsAllowed(this IEnumerable<BlockRule> blockRules, ApiThing thing)
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

        public static bool IsAllowed(this BlockRule rule, ApiThing thing)
        {
            bool blocked = true;

            //Add comment specific here if needed in the future
            blocked &= thing is ApiPost rp && !rule.IsAllowed(rp);
            blocked &= BlockListHelper.TriggersOrSkip(rule.Author, thing.Author, StringMatchType.String);
            blocked &= BlockListHelper.TriggersOrSkip(rule.Body, thing.Body, StringMatchType.Regex);

            return !blocked;
        }

        private static bool IsAllowed(this BlockRule rule, ApiPost post)
        {
            bool blocked = true;

            blocked &= BlockListHelper.TriggersOrSkip(rule.IsLocked, post.IsLocked);

            blocked &= BlockListHelper.TriggersOrSkip(rule.IsNsfw, post.IsNsfw);

            blocked &= BlockListHelper.TriggersOrSkip(rule.IsArchived, post.IsArchived);

            blocked &= BlockListHelper.TriggersOrSkip(rule.Flair, post.LinkFlairText, StringMatchType.String);

            blocked &= BlockListHelper.TriggersOrSkip(rule.SubReddit, post.SubReddit, StringMatchType.String);

            blocked &= BlockListHelper.TriggersOrSkip(rule.Title, post.Title, StringMatchType.Regex);

            return !blocked;
        }
    }
}