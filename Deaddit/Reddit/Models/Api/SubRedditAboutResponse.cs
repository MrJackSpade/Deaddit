using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    public class SubRedditAboutResponse
    {
        [JsonPropertyName("data")]
        public ApiSubReddit Data { get; init; }

        [JsonPropertyName("kind")]
        public ThingKind? Kind { get; init; }
    }
}