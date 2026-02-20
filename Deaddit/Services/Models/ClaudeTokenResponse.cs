using System.Text.Json.Serialization;

namespace Deaddit.Services.Models
{
    public class ClaudeTokenResponse
    {
        [JsonPropertyName("input_tokens")]
        public int InputTokens { get; set; }
    }
}