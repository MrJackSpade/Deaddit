using System.Text.Json.Serialization;

namespace Deaddit.Services.Models
{
    public class ContentBlock
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }
}