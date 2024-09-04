using Deaddit.Reddit.Models.Api;

namespace Deaddit.MAUI.EventArguments
{
    public class ReplySubmittedEventArgs(ApiThing replyTo, RedditCommentMeta newComment) : EventArgs
    {
        public RedditCommentMeta NewComment { get; private set; } = newComment;

        public ApiThing ReplyTo { get; private set; } = replyTo;
    }
}