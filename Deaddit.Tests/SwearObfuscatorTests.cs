using Deaddit.Core.Utils;

namespace Deaddit.Tests
{
    [TestClass]
    public class SwearObfuscatorTests
    {
        [TestMethod]
        public void SwapsHomoglyphsAndInsertsZeroWidthSpace()
        {
            Assert.AreEqual("fu\u200B\u0441k", SwearObfuscator.Obfuscate("fuck"));
            Assert.AreEqual("\u0405\u041D\u200B\u0406\u0422", SwearObfuscator.Obfuscate("SHIT"));
        }

        [TestMethod]
        public void IsDeterministic()
        {
            const string input = "well shit, that's a fucking mess";

            Assert.AreEqual(SwearObfuscator.Obfuscate(input), SwearObfuscator.Obfuscate(input));
        }

        [TestMethod]
        public void IsIdempotent()
        {
            string once = SwearObfuscator.Obfuscate("bullshit motherfucker ass");

            Assert.AreEqual(once, SwearObfuscator.Obfuscate(once));
        }

        [TestMethod]
        public void LeavesCleanTextAlone()
        {
            const string input = "The class passed the scrap assessment, swank.";

            Assert.AreEqual(input, SwearObfuscator.Obfuscate(input));
        }

        [TestMethod]
        public void LooksIdenticalOnceInvisibleCharactersAreRemovedAndScriptsFolded()
        {
            string obfuscated = SwearObfuscator.Obfuscate("What the Fuck, you dumbass bitch.");

            Assert.AreNotEqual("What the Fuck, you dumbass bitch.", obfuscated);
            Assert.AreEqual(
                "What the Fuck, you dumbass bitch.",
                Fold(obfuscated));
        }

        [TestMethod]
        public void LeavesUrlsCodeAndReferencesIntact()
        {
            const string input = "r/fuckcars /u/shitposter [link](https://example.com/shit) https://fuck.com `shit` ```\nfuck\n```";

            Assert.AreEqual(input, SwearObfuscator.Obfuscate(input));
        }

        [TestMethod]
        public void ObfuscatesLinkTextButNotDestination()
        {
            Assert.AreEqual("[\u0455\u04BB\u200B\u0456t](https://example.com/shit)", SwearObfuscator.Obfuscate("[shit](https://example.com/shit)"));
        }

        private static string Fold(string text)
        {
            Dictionary<char, char> reverse = new()
            {
                ['\u0430'] = 'a', ['\u0441'] = 'c', ['\u0501'] = 'd', ['\u0435'] = 'e', ['\u04BB'] = 'h',
                ['\u0456'] = 'i', ['\u0458'] = 'j', ['\u043E'] = 'o', ['\u0440'] = 'p', ['\u051B'] = 'q',
                ['\u0455'] = 's', ['\u051D'] = 'w', ['\u0445'] = 'x', ['\u0443'] = 'y', ['\u0410'] = 'A',
                ['\u0412'] = 'B', ['\u0421'] = 'C', ['\u0415'] = 'E', ['\u041D'] = 'H', ['\u0406'] = 'I',
                ['\u0408'] = 'J', ['\u041A'] = 'K', ['\u041C'] = 'M', ['\u039D'] = 'N', ['\u041E'] = 'O',
                ['\u0420'] = 'P', ['\u0405'] = 'S', ['\u0422'] = 'T', ['\u0425'] = 'X', ['\u04AE'] = 'Y',
                ['\u0396'] = 'Z',
            };

            return new string([.. text.Where(c => c != '\u200B').Select(c => reverse.TryGetValue(c, out char latin) ? latin : c)]);
        }
    }
}
