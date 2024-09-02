using System.Text.Json.Serialization;

namespace Deaddit.Models.Json.Response
{
    public class SubRedditReadResponse
    {
        [JsonPropertyName("kind")]
        public string? Kind { get; set; }

        [JsonPropertyName("data")]
        public ReadMetaData<RedditPostMeta>? Meta { get; set; }
    }
}