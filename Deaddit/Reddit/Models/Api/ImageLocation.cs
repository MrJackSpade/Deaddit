using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    public class ImageLocation
    {
        [JsonPropertyName("u")]
        public string? Url { get; set; }

        [JsonPropertyName("gif")]
        public string? Gif { get; set; }

        [JsonPropertyName("mp4")]
        public string? Mp4 { get; set; }

        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }
    }
}