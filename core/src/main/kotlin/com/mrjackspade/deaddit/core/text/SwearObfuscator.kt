package com.mrjackspade.deaddit.core.text

import java.util.regex.Pattern

/**
 * Deterministically rewrites swear words in outgoing markdown so they render identically but no longer match
 * plain-text filters: every Latin letter that has a visually identical counterpart in another script
 * (Cyrillic/Greek) is swapped for it, and a zero width space is placed mid-word. What counts as a swear is decided
 * by [ProfanityMatcher]. Code, URLs, link destinations and r/ u/ references are left untouched so they keep working.
 */
object SwearObfuscator {
    private const val ZERO_WIDTH_SPACE = '\u200B'

    private val homoglyphs: Map<Char, Char> = mapOf(
        'a' to '\u0430', // а
        'c' to '\u0441', // с
        'd' to '\u0501', // ԁ
        'e' to '\u0435', // е
        'h' to '\u04BB', // һ
        'i' to '\u0456', // і
        'j' to '\u0458', // ј
        'o' to '\u043E', // о
        'p' to '\u0440', // р
        'q' to '\u051B', // ԛ
        's' to '\u0455', // ѕ
        'w' to '\u051D', // ԝ
        'x' to '\u0445', // х
        'y' to '\u0443', // у
        'A' to '\u0410', // А
        'B' to '\u0412', // В
        'C' to '\u0421', // С
        'E' to '\u0415', // Е
        'H' to '\u041D', // Н
        'I' to '\u0406', // І
        'J' to '\u0408', // Ј
        'K' to '\u041A', // К
        'M' to '\u041C', // М
        'N' to '\u039D', // Ν (Greek)
        'O' to '\u041E', // О
        'P' to '\u0420', // Р
        'S' to '\u0405', // Ѕ
        'T' to '\u0422', // Т
        'X' to '\u0425', // Х
        'Y' to '\u04AE', // Ү
        'Z' to '\u0396', // Ζ (Greek)
    )

    // Fenced code, inline code, markdown link destinations, bare URLs, r/ and u/ references.
    // C#: ```[\s\S]*?```|`[^`\n]*`|\]\([^)]*\)|(?:https?://|www\.)\S+|(?<![\w/])/?[ru]/\w+ with IgnoreCase,
    // with .NET's Unicode \s and \w spelled out. .NET's IgnoreCase folds none of these letters beyond ASCII, which is
    // what Java's CASE_INSENSITIVE does.
    private val protectedSpanRegex: Pattern = Pattern.compile(
        "```[\\s\\S]*?```" +
            "|`[^`\\n]*`" +
            "|\\]\\([^)]*\\)" +
            "|(?:https?://|www\\.)${DotNetText.NON_WHITESPACE_CLASS}+" +
            "|(?<!${DotNetText.wordClass("/")})/?[ru]/${DotNetText.wordClass()}+",
        Pattern.CASE_INSENSITIVE,
    )

    fun obfuscate(text: String): String {
        val result = StringBuilder(text.length)
        var position = 0

        val protectedSpan = protectedSpanRegex.matcher(text)

        while (protectedSpan.find()) {
            result.append(obfuscateSwears(text.substring(position, protectedSpan.start())))
            result.append(protectedSpan.group())
            position = protectedSpan.end()
        }

        result.append(obfuscateSwears(text.substring(position)))

        return result.toString()
    }

    private fun obfuscateSpan(span: String): String {
        val result = StringBuilder(span.length + 1)

        for (i in span.indices) {
            if (i == span.length / 2) {
                result.append(ZERO_WIDTH_SPACE)
            }

            result.append(homoglyphs[span[i]] ?: span[i])
        }

        return result.toString()
    }

    private fun obfuscateSwears(text: String): String {
        val result = StringBuilder(text.length)
        var position = 0

        for ((start, end) in ProfanityMatcher.Default.findMatches(text)) {
            result.append(text, position, start)
            result.append(obfuscateSpan(text.substring(start, end)))
            position = end
        }

        result.append(text, position, text.length)

        return result.toString()
    }
}
