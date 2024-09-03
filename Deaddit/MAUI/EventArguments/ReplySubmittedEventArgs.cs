using Deaddit.Reddit.Models.Api;

namespace Deaddit.MAUI.EventArguments
{
    public class ReplySubmittedEventArgs(RedditThing replyTo, RedditCommentMeta newComment) : EventArgs
    {
        public RedditCommentMeta NewComment { get; private set; } = newComment;

        public RedditThing ReplyTo { get; private set; } = replyTo;
    }
}