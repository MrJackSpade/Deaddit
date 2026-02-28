using System.Text.Json.Serialization;

namespace Deaddit.Core.Reddit.Models
{
    // Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
    public class CommentContributionSettings
    {
        [JsonPropertyName("allowed_media_types")]
        public List<string> AllowedMediaTypes { get; init; } = [];
    }
}
