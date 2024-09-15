using System.Text.Json.Serialization;

namespace Deaddit.Core.Reddit.Models.Api
{
    public class Variants
    {
        [JsonPropertyName("gif")]
        public string? Gif { get; init; }

        [JsonPropertyName("mp4")]
        public string? MP4 { get; init; }

        [JsonPropertyName("obfuscated")]
        public bool? IsObfuscated { get; init; }

        [JsonPropertyName("nsfw")]
        public bool? IsNsfw { get; init; }
    }
}