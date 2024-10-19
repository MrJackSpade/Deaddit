using Deaddit.Core.Configurations.Models;
using Deaddit.Core.Reddit.Models.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public static BlockRule FromSubReddit(ApiThing post)
        {
            return new BlockRule()
            {
                SubReddit = post.SubReddit,
                BlockType = BlockType.Post,
                RuleName = $"/r/{post.SubReddit}"
            };
        }

        public static BlockRule FromDomain(ApiPost post) {
            return new BlockRule()
            {
                Domain = post.Domain,
                BlockType = BlockType.Post,
                RuleName = $"({post.Domain})"
            };
        }

        public static BlockRule FromFlair(ApiPost post)
        {
            return new BlockRule()
            {
                Flair = post.LinkFlairText,
                BlockType = BlockType.Post,
                RuleName = $"{post.SubReddit} [{post.Domain}]"
            };
        }
    }
}
