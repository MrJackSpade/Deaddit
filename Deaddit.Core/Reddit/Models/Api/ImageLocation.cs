using System.Text.Json.Serialization;

namespace Deaddit.Core.Reddit.Models.Api
{
    public class ImageLocation
    {
        [JsonPropertyName("gif")]
        public string? Gif { get; init; }

        [JsonPropertyName("mp4")]
        public string? Mp4 { get; init; }

        [JsonPropertyName("u")]
        public string? Url { get; init; }

        [JsonPropertyName("x")]
        public int X { get; init; }

        [JsonPropertyName("y")]
        public int Y { get; init; }
    }
}