using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    public class Variants
    {
        [JsonPropertyName("gif")]
        public string? Gif { get; init; }

        [JsonPropertyName("mp4")]
        public string? MP4 { get; init; }
    }
}