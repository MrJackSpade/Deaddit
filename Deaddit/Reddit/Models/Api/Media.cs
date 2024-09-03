using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    public class Media
    {
        [JsonPropertyName("reddit_video")]
        public RedditVideo? RedditVideo { get; set; }
    }
}