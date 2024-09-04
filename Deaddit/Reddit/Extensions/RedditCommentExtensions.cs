using Deaddit.Reddit.Models;
using Deaddit.Reddit.Models.Api;

namespace Deaddit.Reddit.Extensions
{
    internal static class RedditCommentExtensions
    {
        public static void AddReply(this RedditCommentMeta redditCommentMeta, RedditCommentMeta child)
        {
            if (redditCommentMeta.Data.Replies is null)
            {
                redditCommentMeta.Data.Replies = new CommentReadResponse() { Data = new ReadMetaData<RedditCommentMeta>() };
            }

            if(redditCommentMeta.Data.Replies.Data is null)
            {
                redditCommentMeta.Data.Replies.Data = new();
            }

            redditCommentMeta.Data.Replies.Data.Children.Add(child);
        }

        public static IEnumerable<RedditCommentMeta> GetReplies(this RedditCommentMeta redditComment, ThingKind thingKind = ThingKind.Comment)
        {
            if (redditComment?.Data?.Replies?.Data?.Children is null)
            {
                yield break;
            }

            foreach (RedditCommentMeta comment in redditComment.Data.Replies.Data.Children)
            {
                if ((comment.Kind == thingKind || comment.Kind == ThingKind.More) && comment.Data is not null)
                {
                    yield return comment;
                }
            }
        }
    }
}