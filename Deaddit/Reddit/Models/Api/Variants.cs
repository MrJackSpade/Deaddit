using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    public class Variants
    {
        [JsonPropertyName("gif")]
        public string? Gif { get; set; }

        [JsonPropertyName("mp4")]
        public string? MP4 { get; set; }
    }
}