using Deaddit.Core.Reddit;
using System.Text;

namespace Deaddit.Tests
{
    [TestClass]
    public class TokenV2Tests
    {
        private static string Jwt(string payloadJson)
        {
            static string Segment(string json) => Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');

            return $"{Segment("{\"alg\":\"RS256\",\"typ\":\"JWT\"}")}.{Segment(payloadJson)}.signature";
        }

        [TestMethod]
        public void Parse_UserToken_IsLoggedInWithExpiration()
        {
            long exp = DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeSeconds();

            TokenV2 token = TokenV2.Parse(Jwt($"{{\"sub\":\"t2_abc123\",\"exp\":{exp},\"scp\":\"eJx\"}}"));

            Assert.IsTrue(token.IsLoggedIn);
            Assert.AreEqual(DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime, token.Expiration);
            Assert.IsFalse(token.IsExpired);
        }

        [TestMethod]
        public void Parse_AnonymousToken_IsNotLoggedIn()
        {
            long exp = DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeSeconds();

            TokenV2 token = TokenV2.Parse(Jwt($"{{\"sub\":\"loid\",\"exp\":{exp}}}"));

            Assert.IsFalse(token.IsLoggedIn);
        }

        [TestMethod]
        public void Parse_PastExpiration_IsExpired()
        {
            long exp = DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds();

            TokenV2 token = TokenV2.Parse(Jwt($"{{\"sub\":\"t2_abc123\",\"exp\":{exp}}}"));

            Assert.IsTrue(token.IsExpired);
        }

        [TestMethod]
        public void Parse_PayloadNeedingPadding_Decodes()
        {
            // Payload lengths that are not a multiple of 4 must be padded before base64 decoding.
            for (int i = 0; i < 4; i++)
            {
                string filler = new('x', i);
                TokenV2 token = TokenV2.Parse(Jwt($"{{\"sub\":\"t2_{filler}\",\"exp\":2000000000}}"));
                Assert.IsTrue(token.IsLoggedIn);
            }
        }

        [TestMethod]
        public void Parse_NotAJwt_Throws()
        {
            Assert.ThrowsException<FormatException>(() => TokenV2.Parse("not-a-jwt"));
        }

        [TestMethod]
        public void Parse_MissingExp_Throws()
        {
            Assert.ThrowsException<FormatException>(() => TokenV2.Parse(Jwt("{\"sub\":\"t2_abc123\"}")));
        }
    }
}
