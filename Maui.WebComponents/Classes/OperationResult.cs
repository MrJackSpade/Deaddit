using System.Text.Json.Serialization;

namespace Maui.WebComponents.Classes
{
    internal class OperationResult
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; } = true;
    }
}