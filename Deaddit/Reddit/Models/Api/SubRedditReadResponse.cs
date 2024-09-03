using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    public class SubRedditReadResponse
    {
        [JsonPropertyName("kind")]
        public string? Kind { get; set; }

        [JsonPropertyName("data")]
        public ReadMetaData<RedditPostMeta>? Meta { get; set; }
    }
}