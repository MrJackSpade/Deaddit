using Deaddit.Core.Reddit.Models;

namespace Deaddit.Core.Reddit.Extensions
{
    public static class RedditCommentExtensions
    {
        public static void AddReply(this ApiComment comment, ApiThing child)
        {
            comment.Replies ??= new ApiThingCollection();
            comment.Replies.Children.Add(child);
        }
    }
}
