using Deaddit.Models.Json.Response;

namespace Deaddit.EventArguments
{
    public class ReplySubmittedEventArgs : EventArgs
    {
        public ReplySubmittedEventArgs(RedditThing replyTo, RedditCommentMeta newComment)
        {
            ReplyTo = replyTo;
            NewComment = newComment;
        }

        public RedditCommentMeta NewComment { get; private set; }

        public RedditThing ReplyTo { get; private set; }
    }
}