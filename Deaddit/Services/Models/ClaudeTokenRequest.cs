using Deaddit.Configurations.Ai;
using Deaddit.Extensions;
using System.Text.Json.Serialization;

namespace Deaddit.Services.Models
{
    public class ClaudeTokenRequest
    {
        [JsonPropertyName("messages")]
        public List<Message> Messages { get; set; } = [];

        [JsonPropertyName("model")]
        public string Model { get; set; } = ClaudeModel.Claude_Sonnet_4_5.ToModelId();

        [JsonPropertyName("system")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? System { get; set; }
    }
}