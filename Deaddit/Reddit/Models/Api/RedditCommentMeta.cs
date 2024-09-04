using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    // Root myDeserializedClass = JsonSerializer.Deserialize<List<Root>>(myJsonResponse);

    public class RedditCommentMeta
    {
        [NotNull]
        [JsonPropertyName("data")]
        public ApiComment Data { get; init; }

        [JsonPropertyName("kind")]
        public ThingKind Kind { get; init; }
    }
}