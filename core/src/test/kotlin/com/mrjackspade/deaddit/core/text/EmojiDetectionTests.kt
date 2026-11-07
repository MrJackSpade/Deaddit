package com.mrjackspade.deaddit.core.text

import com.mrjackspade.deaddit.core.ParityCorpus
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class EmojiDetectionTests {
    @Test
    fun testBasicEmojis() {
        val validEmojis = listOf("\uD83D\uDE00", "\uD83D\uDE42", "\uD83D\uDE0D", "\uD83E\uDD14", "\uD83D\uDC4D", "\uD83C\uDF89")
        for (emoji in validEmojis) {
            assertTrue("Emoji $emoji should be detected", EmojiDetector.isMatch(emoji))
        }
    }

    @Test
    fun testComplexEmojis() {
        val complexEmojis = listOf(
            "\uD83D\uDC68\u200D\uD83D\uDC69\u200D\uD83D\uDC67\u200D\uD83D\uDC66",
            "\uD83C\uDFF3\uFE0F\u200D\uD83C\uDF08",
            "\uD83D\uDC69\uD83C\uDFFD\u200D\uD83D\uDE80",
            "\uD83E\uDDD1\uD83C\uDFFB\u200D\uD83E\uDD1D\u200D\uD83E\uDDD1\uD83C\uDFFF",
        )
        for (emoji in complexEmojis) {
            assertTrue("Complex emoji $emoji should be detected", EmojiDetector.isMatch(emoji))
        }
    }

    @Test
    fun testEmojiSubstitution() {
        val input = "Hello \uD83D\uDC4B World! \uD83D\uDE0A How are you? \uD83C\uDF1F"
        val expected = "Hello [emoji] World! [emoji] How are you? [emoji]"
        val result = EmojiDetector.replace(input, "[emoji]")
        assertEquals("Emoji substitution should work correctly", expected, result)
    }

    @Test
    fun testEmojiVariations() {
        val emojiVariations = listOf(
            "\uD83D\uDC4D\uD83C\uDFFB", "\uD83D\uDC4D\uD83C\uDFFC", "\uD83D\uDC4D\uD83C\uDFFD", "\uD83D\uDC4D\uD83C\uDFFE", "\uD83D\uDC4D\uD83C\uDFFF",
        )
        for (emoji in emojiVariations) {
            assertTrue("Emoji variation '$emoji' should be detected", EmojiDetector.isMatch(emoji))
        }
    }

    @Test
    fun testMixedTextAndEmoji() {
        val mixedTexts = listOf(
            "Hello \uD83D\uDC4B World!",
            "\u3053\u3093\u306B\u3061\u306F\uD83D\uDE0A",
            "\u0645\u0631\u062D\u0628\u0627\uD83C\uDF1F",
            "\u041F\u0440\u0438\u0432\u0435\u0442\uD83C\uDF89\u043C\u0438\u0440",
            "\u4F60\u597D\uD83D\uDC68\u200D\uD83D\uDC69\u200D\uD83D\uDC67\u200D\uD83D\uDC66\u4E16\u754C",
        )
        for (text in mixedTexts) {
            assertTrue("Mixed text '$text' should contain detected emoji", EmojiDetector.isMatch(text))
        }
    }

    @Test
    fun testNonEmojiAscii() {
        val asciiChars = listOf(":-)", ":D", "<3", "^_^", ":P", ":|")
        for (ascii in asciiChars) {
            assertFalse("ASCII emoticon '$ascii' should not be detected as emoji", EmojiDetector.isMatch(ascii))
        }
    }

    @Test
    fun testNonEnglishText() {
        val nonEnglishTexts = listOf(
            "\u3053\u3093\u306B\u3061\u306F", // Japanese
            "\u0645\u0631\u062D\u0628\u0627", // Arabic
            "\u4F60\u597D", // Chinese
            "\u041F\u0440\u0438\u0432\u0435\u0442", // Russian (Cyrillic)
            "\u0393\u03B5\u03B9\u03AC \u03C3\u03BF\u03C5", // Greek
        )
        for (text in nonEnglishTexts) {
            assertFalse("Non-English text '$text' should not be detected as emoji", EmojiDetector.isMatch(text))
        }
    }

    /** Every row of parity/emoji-parity.tsv: IsMatch, and Replace with .NET substitution patterns, as the C# did. */
    @Test
    fun matchesTheCSharpOnRecordedInputs() {
        for (row in ParityCorpus.rows("emoji-parity.tsv")) {
            val input = ParityCorpus.decode(row[0])
            val replacement = ParityCorpus.decode(row[1])

            assertEquals("isMatch of ${row[0]}", row[2].toBooleanStrict(), EmojiDetector.isMatch(input))
            assertEquals("replace of ${row[0]} with ${row[1]}", row[3], ParityCorpus.outcome { EmojiDetector.replace(input, replacement) })
        }
    }
}
