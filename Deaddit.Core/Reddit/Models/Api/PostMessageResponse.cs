using System.Text.Json.Serialization;

namespace Deaddit.Core.Reddit.Models.Api
{
    // Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
    public class PostMessageResponse
    {
        [JsonPropertyName("json")]
        public PostMessageResponseMeta Json { get; init; }
    }

    public class PostMessageResponseMeta
    {

        [JsonPropertyName("errors")]
        public List<string> Errors { get; init; } = [];
    }
}