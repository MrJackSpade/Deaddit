using Deaddit.Reddit.Models.Api;

namespace Deaddit.MAUI.EventArguments
{
    public class ReplySubmittedEventArgs(ApiThing replyTo, ApiCommentMeta newComment) : EventArgs
    {
        public ApiCommentMeta NewComment { get; private set; } = newComment;

        public ApiThing ReplyTo { get; private set; } = replyTo;
    }
}