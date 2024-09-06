using System.Text.Json.Serialization;

namespace Deaddit.Core.Reddit.Models.Api
{
    public class SecureMedia
    {
        [JsonPropertyName("reddit_video")]
        public RedditVideo? RedditVideo { get; init; }
    }
}