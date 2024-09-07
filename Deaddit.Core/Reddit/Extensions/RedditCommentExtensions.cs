using Deaddit.Core.Reddit.Models.Api;

namespace Deaddit.Core.Reddit.Extensions
{
    public static class RedditCommentExtensions
    {
        public static void AddReply(this ApiComment redditCommentMeta, ApiThing child)
        {
            redditCommentMeta.Replies ??= new ApiThingCollection();

            redditCommentMeta.Replies.Children.Add(child);
        }

        public static IEnumerable<ApiThing> GetReplies(this ApiComment redditComment)
        {
            if (redditComment?.Replies?.Children is null)
            {
                yield break;
            }

            foreach (ApiThing comment in redditComment.Replies.Children)
            {
                yield return comment;
            }
        }

        public static bool HasChildren(this ApiComment redditCommentMeta)
        {
            return redditCommentMeta.Replies?.Children?.Count > 0;
        }

        public static bool IsDeleted(this ApiThing redditCommentMeta)
        {
            return redditCommentMeta.Author == "[deleted]";
        }

        public static bool IsRemoved(this ApiThing redditCommentMeta)
        {
            return redditCommentMeta.Author == "[removed]";
        }
    }
}