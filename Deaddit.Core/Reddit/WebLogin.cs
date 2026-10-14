namespace Deaddit.Core.Reddit
{
    /// <summary>
    /// The result of logging in through the reddit.com login page.
    /// </summary>
    /// <param name="AccessToken">The logged-in token_v2 value, used as the bearer token.</param>
    /// <param name="Expiration">When the token expires.</param>
    /// <param name="Cookies">
    /// The browser's complete reddit.com cookie header. Reddit only serves comment threads to
    /// requests carrying the full browser session, not to the token alone.
    /// </param>
    public record WebLogin(string AccessToken, DateTime Expiration, string Cookies);
}
