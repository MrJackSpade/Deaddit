using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils;

namespace Deaddit.Core.Extensions
{
    public static class BlockRuleExtensions
    {
        public static bool IsAllowed(this BlockConfiguration blockConfiguration, ApiThing thing, Dictionary<string, UserPartial>? userData = null)
        {
            foreach (BlockRule br in blockConfiguration.BlockRules)
            {
                if (!br.IsAllowed(thing))
                {
                    return false;
                }

                if (userData is not null && thing.AuthorFullName is not null && userData.TryGetValue(thing.AuthorFullName, out UserPartial user))
                {
                    if (blockConfiguration.MinAccountAgeDays > 0)
                    {
                        if ((DateTime.UtcNow - user.CreatedUtc).TotalDays < blockConfiguration.MinAccountAgeDays)
                        {
                            return false;
                        }
                    }

                    if (blockConfiguration.MinCommentKarma > 0 && blockConfiguration.MinCommentKarma > user.CommentKarma)
                    {
                        return false;
                    }

                    if (blockConfiguration.MaxLinkKarma > 0 && blockConfiguration.MaxLinkKarma < user.LinkKarma)
                    {
                        return false;
                    }

                    if (blockConfiguration.MaxLinkKarmaRatio > 0 && user.LinkKarma > 0 && (double)user.LinkKarma / user.CommentKarma > blockConfiguration.MaxLinkKarmaRatio)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static bool IsAllowed(this BlockRule rule, ApiThing thing)
        {
            bool blocked = true;

            //Add comment specific here if needed in the future
            blocked &= thing is ApiPost rp && !rule.IsAllowed(rp);

            blocked &= BlockListHelper.TriggersOrSkip(rule.Author, thing.Author, DynamicMatchType(rule.Author));

            blocked &= BlockListHelper.TriggersOrSkip(rule.Body, thing.Body, DynamicMatchType(rule.Body), true);

            return !blocked;
        }

        public static bool RequiresUserData(this BlockConfiguration blockConfiguration)
        {
            return blockConfiguration.MinAccountAgeDays > 0 || blockConfiguration.MinCommentKarma > 0 || blockConfiguration.MaxLinkKarma > 0;
        }

        private static StringMatchType DynamicMatchType(string? toMatch)
        {
            if (!string.IsNullOrWhiteSpace(toMatch) && toMatch.StartsWith('/') && toMatch.EndsWith('/'))
            {
                return StringMatchType.Regex;
            }
            else
            {
                return StringMatchType.String;
            }
        }

        private static bool IsAllowed(this BlockRule rule, ApiPost post)
        {
            bool blocked = true;

            blocked &= BlockListHelper.TriggersOrSkip(rule.Domain, post.Domain, StringMatchType.String);

            blocked &= BlockListHelper.TriggersOrSkip(rule.IsLocked, post.IsLocked);

            blocked &= BlockListHelper.TriggersOrSkip(rule.IsNsfw, post.IsNsfw);

            blocked &= BlockListHelper.TriggersOrSkip(rule.IsArchived, post.IsArchived);

            blocked &= BlockListHelper.TriggersOrSkip(rule.Flair, post.LinkFlairText, StringMatchType.String);

            blocked &= BlockListHelper.TriggersOrSkip(rule.SubReddit, post.SubReddit, StringMatchType.String);

            blocked &= BlockListHelper.TriggersOrSkip(rule.Title, post.Title, DynamicMatchType(rule.Title), true);

            return !blocked;
        }
    }
}