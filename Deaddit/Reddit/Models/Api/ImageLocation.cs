using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    public class ImageLocation
    {
        [JsonPropertyName("u")]
        public string? Url { get; set; }

        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }
    }
}