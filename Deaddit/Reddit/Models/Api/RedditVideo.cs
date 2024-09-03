using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    public class RedditVideo
    {
        [JsonPropertyName("bitrate_kbps")]
        public int BitrateKbps { get; set; }

        [JsonPropertyName("dash_url")]
        public string? DashUrl { get; set; }

        [JsonPropertyName("duration")]
        public int Duration { get; set; }

        [JsonPropertyName("fallback_url")]
        public string? FallbackUrl { get; set; }

        [JsonPropertyName("has_audio")]
        public bool HasAudio { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("hls_url")]
        public string? HlsUrl { get; set; }

        [JsonPropertyName("is_gif")]
        public bool IsGif { get; set; }

        [JsonPropertyName("scrubber_media_url")]
        public string? ScrubberMediaUrl { get; set; }

        [JsonPropertyName("transcoding_status")]
        public string? TranscodingStatus { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }
    }
}