using System.Text.Json.Serialization;

namespace Deaddit.Core.Reddit.Models
{
    // Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
    public class PostSubmitData
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("drafts_count")]
        public int DraftsCount { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class Json
    {
        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; }

        [JsonPropertyName("data")]
        public PostSubmitData Data { get; set; }
    }

    public class PostSubmitResponse
    {
        [JsonPropertyName("json")]
        public Json Json { get; set; }
    }
}
