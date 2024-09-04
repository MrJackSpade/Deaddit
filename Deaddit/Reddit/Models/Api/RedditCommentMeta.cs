using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    // Root myDeserializedClass = JsonSerializer.Deserialize<List<Root>>(myJsonResponse);

    public class RedditCommentMeta
    {
        [JsonPropertyName("data")]
        public ApiComment? Data { get; set; }

        [JsonPropertyName("kind")]
        public ThingKind Kind { get; set; }
    }
}