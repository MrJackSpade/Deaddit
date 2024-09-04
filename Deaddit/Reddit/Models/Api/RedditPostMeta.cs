using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    public class RedditPostMeta
    {
        [JsonPropertyName("kind")]
        public ThingKind Kind { get; set; }

        [JsonPropertyName("data")]
        public ApiPost? RedditPost { get; set; }
    }
}