using System.Text.Json.Serialization;

namespace Deaddit.Models.Json.Response
{
    public class Media
    {
        [JsonPropertyName("reddit_video")]
        public RedditVideo? RedditVideo { get; set; }
    }
}