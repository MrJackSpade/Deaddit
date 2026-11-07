package com.mrjackspade.deaddit.core.util

/** .NET's number parsing, where the C# app's results depended on it. */
object DotNetNumbers {
    /**
     * `int.TryParse(s, out int value)` (NumberStyles.Integer): optional leading/trailing whitespace (U+0009..U+000D
     * and space), an optional '+' or '-', then ASCII digits, within Int32's range; trailing NULs are ignored, as
     * .NET ignores them. Null where .NET answers false. The sign is the invariant culture's, which the culture of a
     * device the app runs on shares.
     */
    fun tryParseInt(s: String?): Int? {
        if (s == null) {
            return null
        }

        fun isNumberWhite(c: Char) = c == ' ' || c in '\t'..'\r'

        var start = 0
        var end = s.length

        while (end > start && s[end - 1] == '\u0000') {
            end--
        }

        while (start < end && isNumberWhite(s[start])) {
            start++
        }

        while (end > start && isNumberWhite(s[end - 1])) {
            end--
        }

        var negative = false

        if (start < end && (s[start] == '+' || s[start] == '-')) {
            negative = s[start] == '-'
            start++
        }

        if (start >= end) {
            return null
        }

        var result = 0L

        for (k in start until end) {
            val c = s[k]

            if (c !in '0'..'9') {
                return null
            }

            result = result * 10 + (c - '0')

            if (result > Int.MAX_VALUE.toLong() + 1) {
                return null
            }
        }

        val signed = if (negative) -result else result

        return if (signed in Int.MIN_VALUE..Int.MAX_VALUE) signed.toInt() else null
    }
}
