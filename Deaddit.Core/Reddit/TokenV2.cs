using System.Text;
using System.Text.Json;

namespace Deaddit.Core.Reddit
{
    /// <summary>
    /// The claims Deaddit reads from Reddit's token_v2 browser cookie, a JWT.
    /// </summary>
    /// <param name="IsLoggedIn">False for the anonymous token Reddit issues before login (subject "loid").</param>
    /// <param name="Expiration">When Reddit stops accepting the token, from the JWT's exp claim.</param>
    public record TokenV2(bool IsLoggedIn, DateTime Expiration)
    {
        /// <summary>
        /// True once the token is past its expiration.
        /// </summary>
        public bool IsExpired => DateTime.UtcNow >= Expiration;

        /// <summary>
        /// Reads the subject and expiration claims of a token_v2 JWT.
        /// Throws <see cref="FormatException"/> if the value is not a JWT carrying both claims.
        /// </summary>
        public static TokenV2 Parse(string jwt)
        {
            string[] parts = jwt.Split('.');

            if (parts.Length != 3)
            {
                throw new FormatException($"token_v2 is not a JWT: expected 3 segments, found {parts.Length}.");
            }

            string payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');

            using JsonDocument claims = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));

            if (!claims.RootElement.TryGetProperty("sub", out JsonElement sub) || sub.ValueKind != JsonValueKind.String)
            {
                throw new FormatException("token_v2 has no string 'sub' claim.");
            }

            if (!claims.RootElement.TryGetProperty("exp", out JsonElement exp) || !exp.TryGetInt64(out long expSeconds))
            {
                throw new FormatException("token_v2 has no numeric 'exp' claim.");
            }

            return new TokenV2(
                sub.GetString() != "loid",
                DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime);
        }
    }
}
