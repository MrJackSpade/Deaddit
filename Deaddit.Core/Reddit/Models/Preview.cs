using System.Text.Json.Serialization;

namespace Deaddit.Core.Reddit.Models
{
    public class Preview
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; init; }

        [JsonPropertyName("images")]
        public List<RemoteImage> Images { get; init; } = [];

        [JsonPropertyName("reddit_video_preview")]
        public string? RedditVideoPreview { get; init; }
    }
}
