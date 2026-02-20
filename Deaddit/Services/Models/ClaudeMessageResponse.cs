using ClaudeApi;
using System.Text.Json.Serialization;

namespace Deaddit.Services.Models
{
    public class ClaudeMessageResponse
    {
        [JsonPropertyName("content")]
        public List<ContentBlock> Content { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("stop_reason")]
        public string StopReason { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("usage")]
        public Usage Usage { get; set; }
    }
}