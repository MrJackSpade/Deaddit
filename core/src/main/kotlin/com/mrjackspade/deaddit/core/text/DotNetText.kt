package com.mrjackspade.deaddit.core.text

/**
 * Pieces of .NET's text semantics that the JVM does differently, so code ported from C# behaves the same.
 *
 * .NET regexes run over UTF-16 chars, so a surrogate pair is two chars of category Cs and never a letter or
 * word char; java.util.regex runs over code points. The classes here are limited to the BMP to keep the
 * .NET behaviour. They also spell out .NET's Unicode-aware \s and \w, where Java's are ASCII-only.
 */
internal object DotNetText {
    /** .NET's `\s`: `[\f\n\r\t\v\x85\p{Z}]`. */
    const val WHITESPACE = "\\t\\n\\u000B\\f\\r\\u0085\\p{Z}"

    /** .NET's `\s` as a Java character class. */
    const val WHITESPACE_CLASS = "[$WHITESPACE]"

    /** .NET's `\S` as a Java character class. */
    const val NON_WHITESPACE_CLASS = "[^$WHITESPACE]"

    /** .NET's `\p{L}` as a Java character class: a single UTF-16 char that is a letter. */
    const val LETTER_CLASS = "[\\p{L}&&[\\u0000-\\uFFFF]]"

    /** .NET's `\w` (`[\p{L}\p{Mn}\p{Nd}\p{Pc}]`, one UTF-16 char) plus [extra], as a Java character class. */
    fun wordClass(extra: String = ""): String = "[[\\p{L}\\p{Mn}\\p{Nd}\\p{Pc}$extra]&&[\\u0000-\\uFFFF]]"

    /**
     * .NET's `string.ToLowerInvariant()`: simple case mapping that never changes the length, so indexes line up
     * with the original. Unlike [String.lowercase], it never expands a char ('İ' stays one char), and .NET's
     * invariant casing leaves U+0130 'İ' and U+0131 'ı' unmapped.
     */
    fun toLowerInvariant(text: String): String {
        val result = StringBuilder(text.length)
        var i = 0

        while (i < text.length) {
            val c = text[i]

            if (Character.isHighSurrogate(c) && i + 1 < text.length && Character.isLowSurrogate(text[i + 1])) {
                result.appendCodePoint(Character.toLowerCase(Character.toCodePoint(c, text[i + 1])))
                i += 2
                continue
            }

            result.append(if (c == 'İ') c else Character.toLowerCase(c))
            i++
        }

        return result.toString()
    }

    /**
     * .NET's `StringComparer.OrdinalIgnoreCase` against keys that are pure ASCII: only ASCII letters fold.
     * Kotlin's `equals(ignoreCase = true)` would also fold 'ſ' to 's' and 'K' (Kelvin) to 'k', which .NET does not.
     */
    fun asciiFold(text: String): String {
        val chars = CharArray(text.length)

        for (i in text.indices) {
            val c = text[i]
            chars[i] = if (c in 'A'..'Z') c + ('a' - 'A') else c
        }

        return String(chars)
    }
}
