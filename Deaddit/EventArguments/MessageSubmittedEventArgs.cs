using Reddit.Api.Models.Api;

namespace Deaddit.EventArguments
{
    public class MessageSubmittedEventArgs(ApiMessage replyTo, ApiMessage newMessage) : EventArgs
    {
        public ApiMessage NewMessage { get; private set; } = newMessage;

        public ApiThing ReplyTo { get; private set; } = replyTo;
    }
}