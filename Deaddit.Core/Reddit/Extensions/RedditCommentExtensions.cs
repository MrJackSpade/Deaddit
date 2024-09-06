using Deaddit.Core.Reddit.Models;
using Deaddit.Core.Reddit.Models.Api;

namespace Deaddit.Core.Reddit.Extensions
{
    public static class RedditCommentExtensions
    {
        public static void AddReply(this ApiCommentMeta redditCommentMeta, ApiCommentMeta child)
        {
            if (redditCommentMeta.Data.Replies is null)
            {
                redditCommentMeta.Data.Replies = new CommentReadResponse() { Data = new ReadMetaData<ApiCommentMeta>() };
            }

            if (redditCommentMeta.Data.Replies.Data is null)
            {
                redditCommentMeta.Data.Replies.Data = new();
            }

            redditCommentMeta.Data.Replies.Data.Children.Add(child);
        }

        public static IEnumerable<ApiCommentMeta> GetReplies(this ApiCommentMeta redditComment, ThingKind thingKind = ThingKind.Comment)
        {
            if (redditComment?.Data?.Replies?.Data?.Children is null)
            {
                yield break;
            }

            foreach (ApiCommentMeta comment in redditComment.Data.Replies.Data.Children)
            {
                if ((comment.Kind == thingKind || comment.Kind == ThingKind.More) && comment.Data is not null)
                {
                    yield return comment;
                }
            }
        }

        public static bool HasChildren(this ApiCommentMeta redditCommentMeta)
        {
            return redditCommentMeta.Data.Replies?.Data?.Children?.Count > 0 || redditCommentMeta.Kind == ThingKind.More;
        }

        public static bool IsDeleted(this ApiCommentMeta redditCommentMeta)
        {
            return redditCommentMeta.Data.Author == "[deleted]";
        }

        public static bool IsRemoved(this ApiCommentMeta redditCommentMeta)
        {
            return redditCommentMeta.Data.Author == "[removed]";
        }
    }
}