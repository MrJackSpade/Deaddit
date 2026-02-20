using System.Text.Json.Serialization;

namespace Deaddit.Services.Models
{
    // Message types
    public abstract class Message
    {
        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("role")]
        public abstract string Role { get; }
    }
}