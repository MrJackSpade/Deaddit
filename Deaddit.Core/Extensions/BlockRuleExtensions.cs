using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Utils.Blocking;
using Reddit.Api.Models;
using Reddit.Api.Models.Api;
using static Deaddit.Core.Utils.Blocking.BlockListHelper;

namespace Deaddit.Core.Extensions
{
    public static partial class BlockRuleExtensions
    {
        public static bool IsAllowed(this BlockConfiguration blockConfiguration, ApiThing thing, Dictionary<string, UserPartial>? userData = null)
        {
            foreach (BlockRule br in blockConfiguration.WhiteList.Rules)
            {
                if (br.IsMatch(thing))
                {
                    return true;
                }
            }

            foreach (BlockRule br in blockConfiguration.BlackList.Rules)
            {
                if (br.IsMatch(thing))
                {
                    return false;
                }
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

            return true;
        }

        public static bool IsMatch(this BlockRule rule, ApiThing thing)
        {
            MatchResult result = new();

            if (thing is ApiPost rp)
            {
                if (rule.BlockType == BlockType.Comment)
                {
                    return false;
                }

                result.Apply(rule.PostIsMatch(rp));
            }

            if (thing is ApiComment rc)
            {
                if (rule.BlockType == BlockType.Post)
                {
                    return false;
                }
            }

            result.Apply(BlockListHelper.TriggersOrSkip(rule.Author, thing.Author, DynamicMatchType(rule.Author)));

            result.Apply(BlockListHelper.TriggersOrSkip(rule.Body, thing.Body, DynamicMatchType(rule.Body), true));

            return result.IsMatch;
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

        private static TriggerState PostIsMatch(this BlockRule rule, ApiPost post)
        {
            MatchResult result = new();

            result.Apply(BlockListHelper.TriggersOrSkip(rule.Domain, post.Domain, StringMatchType.String));

            result.Apply(BlockListHelper.TriggersOrSkip(rule.IsLocked, post.IsLocked));

            result.Apply(BlockListHelper.TriggersOrSkip(rule.IsNsfw, post.IsNsfw));

            result.Apply(BlockListHelper.TriggersOrSkip(rule.IsArchived, post.IsArchived));

            result.Apply(BlockListHelper.TriggersOrSkip(rule.Flair, post.LinkFlairText, StringMatchType.String));

            result.Apply(BlockListHelper.TriggersOrSkip(rule.SubReddit, post.SubRedditName, StringMatchType.String));

            result.Apply(BlockListHelper.TriggersOrSkip(rule.Title, post.Title, DynamicMatchType(rule.Title), true));

            return result.State;
        }
    }
}