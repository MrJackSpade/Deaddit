using System.Text.Json.Serialization;

namespace Deaddit.Core.Reddit.Models.Api
{
    public class Media
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("height")]
        public int? Height { get; set; }

        [JsonPropertyName("media_domain_url")]
        public string? MediaDomainUrl { get; set; }

        [JsonPropertyName("oembed")]
        public OEmbed? OEmbed { get; set; }

        [JsonPropertyName("reddit_video")]
        public RedditVideo? RedditVideo { get; init; }

        [JsonPropertyName("scrolling")]
        public bool? Scrolling { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("width")]
        public int? Width { get; set; }
    }
}