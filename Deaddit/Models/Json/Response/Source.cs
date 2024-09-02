using System.Text.Json.Serialization;

namespace Deaddit.Models.Json.Response
{
    public class Source
    {
        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }
    }
}