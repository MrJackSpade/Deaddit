using Deaddit.Services.Constants;
using System.Text.Json.Serialization;

namespace Deaddit.Services.Models
{
    /// <summary>
    /// Request/Response types
    /// </summary>
    public class ClaudeMessageRequest
    {
        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; } = 1024;

        [JsonPropertyName("messages")]
        public List<Message> Messages { get; set; } = [];

        [JsonPropertyName("model")]
        public string Model { get; set; } = ModelNames.Claude_Sonnet_4_5_20250929;

        [JsonPropertyName("system")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? System { get; set; }

        [JsonPropertyName("temperature")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Temperature { get; set; }
    }
}