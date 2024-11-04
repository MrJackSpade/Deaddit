using Deaddit.Core.Configurations.Models;
using Reddit.Api.Models.Api;

namespace Deaddit.Core.Utils.Blocking
{
    public class BlockRuleHelper
    {
        public static BlockRule FromAuthor(ApiThing post)
        {
            return new BlockRule()
            {
                Author = post.Author,
                BlockType = BlockType.Post,
                RuleName = $"/u/{post.Author}"
            };
        }

        public static BlockRule FromDomain(ApiPost post)
        {
            return new BlockRule()
            {
                Domain = post.Domain,
                BlockType = BlockType.Post,
                RuleName = $"({post.Domain})"
            };
        }

        public static BlockRule FromFlair(ApiComment comment)
        {
            return new BlockRule()
            {
                Flair = comment.AuthorFlairText,
                SubReddit = comment.SubRedditName,
                BlockType = BlockType.Post,
                RuleName = $"{comment.SubRedditName} [{comment.AuthorFlairText}]"
            };
        }

        public static BlockRule FromFlair(ApiPost post)
        {
            return new BlockRule()
            {
                Flair = post.LinkFlairText,
                SubReddit = post.SubRedditName,
                BlockType = BlockType.Post,
                RuleName = $"{post.SubRedditName} [{post.LinkFlairText}]"
            };
        }

        public static BlockRule FromSubReddit(ApiThing post)
        {
            return new BlockRule()
            {
                SubReddit = post.SubRedditName,
                BlockType = BlockType.Post,
                RuleName = $"/r/{post.SubRedditName}"
            };
        }
    }
}