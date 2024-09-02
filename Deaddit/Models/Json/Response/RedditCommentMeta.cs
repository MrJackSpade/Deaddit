using System.Text.Json.Serialization;

namespace Deaddit.Models.Json.Response
{
    // Root myDeserializedClass = JsonSerializer.Deserialize<List<Root>>(myJsonResponse);

    public class RedditCommentMeta
    {
        [JsonPropertyName("data")]
        public RedditComment? Data { get; set; }

        [JsonPropertyName("kind")]
        public ThingKind Kind { get; set; }
    }
}