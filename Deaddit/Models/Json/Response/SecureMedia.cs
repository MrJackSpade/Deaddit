using System.Text.Json.Serialization;

namespace Deaddit.Models.Json.Response
{
    public class SecureMedia
    {
        [JsonPropertyName("reddit_video")]
        public RedditVideo? RedditVideo { get; set; }
    }
}