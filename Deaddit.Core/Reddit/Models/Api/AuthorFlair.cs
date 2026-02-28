using System.Text.Json.Serialization;

namespace Deaddit.Core.Reddit.Models.Api
{
    public class AuthorFlair
    {
        [JsonPropertyName("a")]
        public string? Author { get; init; }

        [JsonPropertyName("e")]
        public string? Emoji { get; init; }

        [JsonPropertyName("t")]
        public string? Text { get; init; }

        [JsonPropertyName("u")]
        public string? UserLink { get; init; }
    }
}