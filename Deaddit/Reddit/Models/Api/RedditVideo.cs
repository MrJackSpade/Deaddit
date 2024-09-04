using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    public class RedditVideo
    {
        [JsonPropertyName("bitrate_kbps")]
        public int BitrateKbps { get; init; }

        [JsonPropertyName("dash_url")]
        public string? DashUrl { get; init; }

        [JsonPropertyName("duration")]
        public int Duration { get; init; }

        [JsonPropertyName("fallback_url")]
        public string? FallbackUrl { get; init; }

        [JsonPropertyName("has_audio")]
        public bool HasAudio { get; init; }

        [JsonPropertyName("height")]
        public int Height { get; init; }

        [JsonPropertyName("hls_url")]
        public string? HlsUrl { get; init; }

        [JsonPropertyName("is_gif")]
        public bool IsGif { get; init; }

        [JsonPropertyName("scrubber_media_url")]
        public string? ScrubberMediaUrl { get; init; }

        [JsonPropertyName("transcoding_status")]
        public string? TranscodingStatus { get; init; }

        [JsonPropertyName("width")]
        public int Width { get; init; }
    }
}