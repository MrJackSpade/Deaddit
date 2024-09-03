using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    public class Preview
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("images")]
        public List<RemoteImage> Images { get; set; } = [];

        [JsonPropertyName("reddit_video_preview")]
        public string? RedditVideoPreview { get; set; }
    }
}