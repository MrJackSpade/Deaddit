using Reddit.Api.Models.Api;

namespace Deaddit.EventArguments
{
    public class ReplySubmittedEventArgs(ApiThing replyTo, ApiComment newComment) : EventArgs
    {
        public ApiComment NewComment { get; private set; } = newComment;

        public ApiThing ReplyTo { get; private set; } = replyTo;
    }
}