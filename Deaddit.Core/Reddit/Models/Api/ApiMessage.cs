
namespace Deaddit.Core.Reddit.Models.Api
{
    public class ApiMessage : ApiThing
    {
        public object AssociatedAwardingId { get; set; }

        public string? AuthorFullname { get; set; }

        public string? Context { get; set; }

        public string? Dest { get; set; }

        public long? FirstMessage { get; set; }

        public string? FirstMessageName { get; set; }

        public bool? New { get; set; }

        public string? Replies { get; set; }

        public string? Subject { get; set; }

        public string? Subreddit { get; set; }

        public string? Type { get; set; }

        public bool WasComment { get; set; }
    }
}