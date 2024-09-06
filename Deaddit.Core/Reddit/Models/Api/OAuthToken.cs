using System.Text.Json.Serialization;

namespace Deaddit.Core.Reddit.Models.Api
{
    public class OAuthToken
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }

        [JsonPropertyName("scope")]
        public string? Scope { get; init; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; init; }
    }
}