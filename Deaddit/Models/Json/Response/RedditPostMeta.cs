using System.Text.Json.Serialization;

namespace Deaddit.Models.Json.Response
{
    public class RedditPostMeta
    {
        [JsonPropertyName("kind")]
        public ThingKind Kind { get; set; }

        [JsonPropertyName("data")]
        public RedditPost? RedditPost { get; set; }
    }
}