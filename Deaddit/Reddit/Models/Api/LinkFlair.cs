using System.Text.Json.Serialization;

namespace Deaddit.Reddit.Models.Api
{
    public class LinkFlair
    {
        [JsonPropertyName("a")]
        public string? A { get; init; }

        [JsonPropertyName("t")]
        public string? Text { get; init; }

        [JsonPropertyName("e")]
        public string? Type { get; init; }

        [JsonPropertyName("u")]
        public string? U { get; init; }
    }
}