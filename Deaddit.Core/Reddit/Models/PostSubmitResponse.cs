using System.Text.Json.Serialization;

namespace Deaddit.Core.Reddit.Models
{
    public class Json
    {
        [JsonPropertyName("data")]
        public PostSubmitData Data { get; set; }

        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; }
    }

    // Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
    public class PostSubmitData
    {
        [JsonPropertyName("drafts_count")]
        public int DraftsCount { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

    public class PostSubmitResponse
    {
        [JsonPropertyName("json")]
        public Json Json { get; set; }
    }
}