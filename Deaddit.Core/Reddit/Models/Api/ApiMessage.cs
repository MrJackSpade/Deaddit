using System.Text.Json.Serialization;

namespace Deaddit.Core.Reddit.Models.Api
{
    public class ApiMessage : ApiThing
    {

        [JsonPropertyName("first_message")]
        public long? FirstMessage { get; set; }

        [JsonPropertyName("first_message_name")]
        public string? FirstMessageName { get; set; }

        [JsonPropertyName("subreddit")]
        public string? Subreddit { get; set; }

        [JsonPropertyName("replies")]
        public string? Replies { get; set; }

        [JsonPropertyName("author_fullname")]
        public string? AuthorFullname { get; set; }

        [JsonPropertyName("subject")]
        public string? Subject { get; set; }

        [JsonPropertyName("associated_awarding_id")]
        public object AssociatedAwardingId { get; set; }

        [JsonPropertyName("new")]
        public bool? New { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("dest")]
        public string? Dest { get; set; }

        [JsonPropertyName("was_comment")]
        public bool WasComment { get; set; }

        [JsonPropertyName("context")]
        public string? Context { get; set; }
    }
}

