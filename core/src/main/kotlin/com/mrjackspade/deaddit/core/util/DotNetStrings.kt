package com.mrjackspade.deaddit.core.util

/** .NET's string checks, where Kotlin's differ from them. */
object DotNetStrings {
    /**
     * `char.IsWhiteSpace`: the Unicode space, line and paragraph separators, U+0009..U+000D and U+0085. Kotlin's
     * `isWhitespace` also counts U+001C..U+001F and not U+0085.
     */
    fun isWhiteSpace(c: Char): Boolean = when (c) {
        in '\t'..'\r', ' ', '\u0085', ' ' -> true
        else -> when (Character.getType(c).toByte()) {
            Character.SPACE_SEPARATOR, Character.LINE_SEPARATOR, Character.PARAGRAPH_SEPARATOR -> true
            else -> false
        }
    }

    /** `string.IsNullOrWhiteSpace`. */
    fun isNullOrWhiteSpace(s: String?): Boolean = s == null || s.all(::isWhiteSpace)

    /**
     * The char StringComparison.OrdinalIgnoreCase compares [c] as: its simple uppercase, except that no non-ASCII
     * char folds onto an ASCII one ('ı', 'ſ' and the Kelvin sign stay themselves, as .NET's ordinal casing keeps them).
     */
    fun ordinalIgnoreCaseUpper(c: Char): Char {
        if (c.code < 0x80) {
            return if (c in 'a'..'z') c - ('a' - 'A') else c
        }
        val upper = Character.toUpperCase(c)
        return if (upper.code < 0x80) c else upper
    }

    /** `s.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)`. */
    fun endsWithOrdinalIgnoreCase(s: String, suffix: String): Boolean {
        if (suffix.length > s.length) return false
        val offset = s.length - suffix.length
        return suffix.indices.all { ordinalIgnoreCaseUpper(s[offset + it]) == ordinalIgnoreCaseUpper(suffix[it]) }
    }
}
