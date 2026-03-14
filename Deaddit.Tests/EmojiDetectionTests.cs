using Deaddit.Core.Utils;

namespace Deaddit.Tests
{
    [TestClass]
    public class EmojiDetectionTests
    {
        [TestMethod]
        public void TestBasicEmojis()
        {
            string[] validEmojis = ["😀", "🙂", "😍", "🤔", "👍", "🎉"];
            foreach (string emoji in validEmojis)
            {
                Assert.IsTrue(EmojiDetector.IsMatch(emoji), $"Emoji {emoji} should be detected");
            }
        }

        [TestMethod]
        public void TestComplexEmojis()
        {
            string[] complexEmojis = ["👨‍👩‍👧‍👦", "🏳️‍🌈", "👩🏽‍🚀", "🧑🏻‍🤝‍🧑🏿"];
            foreach (string emoji in complexEmojis)
            {
                Assert.IsTrue(EmojiDetector.IsMatch(emoji), $"Complex emoji {emoji} should be detected");
            }
        }

        [TestMethod]
        public void TestEmojiSubstitution()
        {
            string input = "Hello 👋 World! 😊 How are you? 🌟";
            string expected = "Hello [emoji] World! [emoji] How are you? [emoji]";
            string result = EmojiDetector.Replace(input, "[emoji]");
            Assert.AreEqual(expected, result, "Emoji substitution should work correctly");
        }

        [TestMethod]
        public void TestEmojiVariations()
        {
            string[] emojiVariations = ["👍🏻", "👍🏼", "👍🏽", "👍🏾", "👍🏿"];
            foreach (string emoji in emojiVariations)
            {
                Assert.IsTrue(EmojiDetector.IsMatch(emoji), $"Emoji variation '{emoji}' should be detected");
            }
        }

        [TestMethod]
        public void TestMixedTextAndEmoji()
        {
            string[] mixedTexts = [
                "Hello 👋 World!",
                "こんにちは😊",
                "مرحبا🌟",
                "Привет🎉мир",
                "你好👨‍👩‍👧‍👦世界"
            ];
            foreach (string text in mixedTexts)
            {
                Assert.IsTrue(EmojiDetector.IsMatch(text), $"Mixed text '{text}' should contain detected emoji");
            }
        }

        [TestMethod]
        public void TestNonEmojiAscii()
        {
            string[] asciiChars = [":-)", ":D", "<3", "^_^", ":P", ":|"];
            foreach (string ascii in asciiChars)
            {
                Assert.IsFalse(EmojiDetector.IsMatch(ascii), $"ASCII emoticon '{ascii}' should not be detected as emoji");
            }
        }

        [TestMethod]
        public void TestNonEnglishText()
        {
            string[] nonEnglishTexts = [
                "こんにちは", // Japanese
                "مرحبا", // Arabic
                "你好", // Chinese
                "Привет", // Russian (Cyrillic)
                "Γειά σου" // Greek
            ];
            foreach (string text in nonEnglishTexts)
            {
                Assert.IsFalse(EmojiDetector.IsMatch(text), $"Non-English text '{text}' should not be detected as emoji");
            }
        }
    }
}