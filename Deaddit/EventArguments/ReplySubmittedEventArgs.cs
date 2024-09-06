using Deaddit.Core.Reddit.Models.Api;

namespace Deaddit.EventArguments
{
    public class ReplySubmittedEventArgs(ApiThing replyTo, ApiCommentMeta newComment) : EventArgs
    {
        public ApiCommentMeta NewComment { get; private set; } = newComment;

        public ApiThing ReplyTo { get; private set; } = replyTo;
    }
}