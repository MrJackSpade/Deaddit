using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    public class RedditPostMeta
    {
        [JsonPropertyName("kind")]
        public ThingKind Kind { get; init; }

        [NotNull]
        [JsonPropertyName("data")]
        public ApiPost RedditPost { get; init; }
    }
}