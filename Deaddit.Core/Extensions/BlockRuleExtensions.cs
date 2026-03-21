using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Reddit.Models.Api;
using Deaddit.Core.Utils.Blocking;
using Reddit.Api.Models.Json.Users;
using static Deaddit.Core.Utils.Blocking.BlockListHelper;

namespace Deaddit.Core.Extensions
{
    public static partial class BlockRuleExtensions
    {
        /// <summary>
        /// The main entry point for filtering. Determines if a thing (post/comment)
        /// should be shown or hidden.
        ///
        /// Evaluation order:
        ///   1. Whitelist — if ANY whitelist rule matches, the thing is allowed immediately
        ///      (short-circuits, skips all other checks)
        ///   2. Blacklist — if ANY blacklist rule matches, the thing is blocked immediately
        ///   3. User-level filters — account age, karma thresholds, karma ratios
        ///   4. If nothing matched, the thing is allowed by default
        ///
        /// Whitelist takes priority over blacklist. This lets you do things like
        /// "block all of /r/pics except posts by /u/someone".
        /// </summary>
        public static bool IsAllowed(this BlockConfiguration blockConfiguration, ApiThing thing, Dictionary<string, UserPartialData>? userData = null)
        {
            // Whitelist wins — if any whitelist rule matches, allow unconditionally
            foreach (BlockRule br in blockConfiguration.WhiteList.Rules)
            {
                if (br.IsMatch(thing))
                {
                    return true;
                }
            }

            // Blacklist — if any blacklist rule matches, block it
            foreach (BlockRule br in blockConfiguration.BlackList.Rules)
            {
                if (br.IsMatch(thing))
                {
                    return false;
                }
            }

            // User-level filtering (account age, karma). Only runs if user data
            // was fetched (requires an extra API call, so it's opt-in).
            if (userData is not null && thing.AuthorFullName is not null && userData.TryGetValue(thing.AuthorFullName, out UserPartialData? user))
            {
                if (blockConfiguration.MinAccountAgeDays > 0 && user.CreatedUtc.HasValue)
                {
                    if ((DateTime.UtcNow - (DateTime)user.CreatedUtc).TotalDays < blockConfiguration.MinAccountAgeDays)
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

            // Nothing matched — default to allowed
            return true;
        }

        /// <summary>
        /// Checks if a single block rule matches a thing. A rule matches when ALL of its
        /// populated fields match (AND logic). Empty/null fields on the rule are ignored
        /// (they don't participate in matching at all).
        ///
        /// Example: A rule with Flair="Meme" and SubReddit="pics" matches a post that
        /// has flair "Meme" AND is in /r/pics. It won't match a "Meme" post in /r/funny.
        ///
        /// Fields are checked in two groups:
        ///   1. Post-specific fields (flair, domain, subreddit, title, nsfw, locked, archived)
        ///      — only checked if the thing is a post. Bundled into PostIsMatch() which
        ///      returns a single TriggerState that's folded into the outer result.
        ///   2. Common fields (author, body) — checked for both posts and comments.
        ///
        /// The BlockType field acts as an early-out: a Post-only rule won't even attempt
        /// to match comments, and vice versa.
        /// </summary>
        public static bool IsMatch(this BlockRule rule, ApiThing thing)
        {
            MatchResult result = new();

            if (thing is ApiPost rp)
            {
                // Rule is comment-only — can never match a post
                if (rule.BlockType == BlockType.Comment)
                {
                    return false;
                }

                // Check all post-specific fields (flair, domain, subreddit, etc.)
                // and fold the combined result into our outer result
                result.Apply(rule.PostIsMatch(rp));
            }

            if (thing is ApiComment rc)
            {
                // Rule is post-only — can never match a comment
                if (rule.BlockType == BlockType.Post)
                {
                    return false;
                }
            }

            // Common fields — apply to both posts and comments.
            // Author and Body support regex if wrapped in /slashes/.
            result.Apply(BlockListHelper.TriggersOrSkip(rule.Author, thing.Author, DynamicMatchType(rule.Author)));

            // Body uses partial matching — the rule value just needs to appear
            // somewhere in the body, not be an exact match
            result.Apply(BlockListHelper.TriggersOrSkip(rule.Body, thing.Body, DynamicMatchType(rule.Body), true));

            return result.IsMatch;
        }

        /// <summary>
        /// Returns true if any user-level filtering is configured, meaning we need
        /// to fetch user data (extra API calls) to evaluate the block rules.
        /// </summary>
        public static bool RequiresUserData(this BlockConfiguration blockConfiguration)
        {
            return blockConfiguration.MinAccountAgeDays > 0 || blockConfiguration.MinCommentKarma > 0 || blockConfiguration.MaxLinkKarma > 0;
        }

        /// <summary>
        /// If the match string is wrapped in /slashes/, treat it as a regex pattern.
        /// Otherwise, do a plain string comparison.
        /// </summary>
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

        /// <summary>
        /// Checks all post-specific fields on the rule against the post.
        /// Returns the combined TriggerState to be folded into the outer result.
        ///
        /// Each field is checked independently via TriggersOrSkip:
        ///   - If the rule field is null/empty → Skip (field not configured, ignore it)
        ///   - If the rule field matches the post value → Match
        ///   - If the rule field doesn't match → NoMatch (kills the whole rule via AND)
        ///
        /// Most fields use exact string matching (case-insensitive).
        /// Title supports partial matching (substring) and regex via /slashes/.
        /// Flair uses exact matching — "Question | Help" must match exactly.
        /// </summary>
        private static TriggerState PostIsMatch(this BlockRule rule, ApiPost post)
        {
            MatchResult result = new();

            result.Apply(BlockListHelper.TriggersOrSkip(rule.Domain, post.Domain, StringMatchType.String));

            result.Apply(BlockListHelper.TriggersOrSkip(rule.IsLocked, post.IsLocked));

            result.Apply(BlockListHelper.TriggersOrSkip(rule.IsNsfw, post.IsNsfw));

            result.Apply(BlockListHelper.TriggersOrSkip(rule.IsArchived, post.IsArchived));

            result.Apply(BlockListHelper.TriggersOrSkip(rule.Flair, post.LinkFlairText, StringMatchType.String));

            result.Apply(BlockListHelper.TriggersOrSkip(rule.SubReddit, post.SubRedditName, StringMatchType.String));

            // Title uses partial matching — rule value is a substring search, not exact.
            // Also supports regex if wrapped in /slashes/.
            result.Apply(BlockListHelper.TriggersOrSkip(rule.Title, post.Title, DynamicMatchType(rule.Title), true));

            return result.State;
        }
    }
}