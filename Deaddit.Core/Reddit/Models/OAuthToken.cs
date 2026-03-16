
namespace Deaddit.Core.Reddit.Models
{
    public class OAuthToken
    {
        public string? AccessToken { get; init; }

        public int ExpiresIn { get; init; }

        public string? Scope { get; init; }

        public string? TokenType { get; init; }
    }
}