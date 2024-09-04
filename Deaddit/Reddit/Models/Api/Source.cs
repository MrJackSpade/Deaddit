using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    public class Source
    {
        [JsonPropertyName("height")]
        public int Height { get; init; }

        [JsonPropertyName("url")]
        public string? Url { get; init; }

        [JsonPropertyName("width")]
        public int Width { get; init; }
    }
}