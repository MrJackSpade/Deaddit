package com.mrjackspade.deaddit.core.text

import java.util.regex.Pattern

/**
 * Runs a pattern written for .NET's `Regex` (default options) on java.util.regex, translating what
 * the two read differently, so user-written rules (blocklist /regex/ values) match as they did in
 * the C# app:
 *
 * - `\d`, `\w`, `\s`, `\b` and their negations are Unicode-aware in .NET; Java's are ASCII-only
 *   unless asked, so they're spelled out as .NET defines them (one UTF-16 char, see [DotNetText]).
 *   Inside a character class `\b` is a backspace in .NET.
 * - `.` and `$` treat only `\n` as a line end in .NET ([Pattern.UNIX_LINES]); `(?i)` folds
 *   Unicode case ([Pattern.UNICODE_CASE]).
 * - .NET reads as literals what Java rejects or reads as syntax: a `{` or `}` that isn't part of a
 *   quantifier, and `[` and `&` inside a character class.
 * - .NET-only syntax: class subtraction `[a-z-[aeiou]]`, comments `(?#...)`, `(?'name'...)` and
 *   `\k'name'` groups, `\0` octal escapes, and `\p{IsBlock}` Unicode block names.
 *
 * Anything else is passed through as written; a pattern Java can't compile throws, as an invalid
 * one threw in .NET.
 */
object DotNetRegex {
    fun isMatch(input: String, pattern: String): Boolean =
        Pattern.compile(toJava(pattern), Pattern.UNIX_LINES or Pattern.UNICODE_CASE).matcher(input).find()

    /** [pattern], as .NET reads it, in java.util.regex syntax. */
    fun toJava(pattern: String): String = Translator(pattern).translate()

    private const val BMP = "[\\u0000-\\uFFFF]"
    private const val WORD = "\\p{L}\\p{Mn}\\p{Nd}\\p{Pc}"
    private const val DIGIT_CLASS = "[\\p{Nd}&&$BMP]"
    private const val NON_DIGIT_CLASS = "[^\\p{Nd}]"
    private val WORD_CLASS = DotNetText.wordClass()
    private const val NON_WORD_CLASS = "[^$WORD]"
    private val BOUNDARY = "(?:(?<=$WORD_CLASS)(?!$WORD_CLASS)|(?<!$WORD_CLASS)(?=$WORD_CLASS))"
    private val NON_BOUNDARY = "(?:(?<=$WORD_CLASS)(?=$WORD_CLASS)|(?<!$WORD_CLASS)(?!$WORD_CLASS))"
    // Android's java.util.regex (ICU) rejects an unescaped '}' that desktop Java reads as a literal.
    private val QUANTIFIER = Regex("""\{\d+(,\d*)?\}""")

    private class Translator(private val pattern: String) {
        private val out = StringBuilder(pattern.length * 2)
        private var i = 0

        fun translate(): String {
            while (i < pattern.length) {
                val c = pattern[i]
                when {
                    c == '\\' -> escape(inClass = false)
                    c == '[' -> characterClass()
                    pattern.startsWith("(?#", i) -> {
                        val end = pattern.indexOf(')', i)
                        require(end >= 0) { "Unterminated (?#...) comment in '$pattern'." }
                        i = end + 1
                    }
                    pattern.startsWith("(?'", i) -> {
                        val end = pattern.indexOf('\'', i + 3)
                        require(end >= 0) { "Unterminated (?'name' group in '$pattern'." }
                        out.append("(?<").append(pattern, i + 3, end).append('>')
                        i = end + 1
                    }
                    c == '{' -> {
                        val quantifier = QUANTIFIER.matchAt(pattern, i)
                        if (quantifier != null) {
                            out.append(quantifier.value)
                            i += quantifier.value.length
                        } else {
                            out.append("\\{")
                            i++
                        }
                    }
                    c == '}' -> {
                        // A literal in .NET; ICU (Android) rejects it unescaped.
                        out.append("\\}")
                        i++
                    }
                    else -> {
                        out.append(c)
                        i++
                    }
                }
            }

            return out.toString()
        }

        /** A class at [i] (its '['), through its closing ']'. */
        private fun characterClass() {
            out.append('[')
            i++

            if (i < pattern.length && pattern[i] == '^') {
                out.append('^')
                i++
            }

            // A ']' first in the class is a literal.
            if (i < pattern.length && pattern[i] == ']') {
                out.append("\\]")
                i++
            }

            while (i < pattern.length) {
                val c = pattern[i]
                when {
                    c == ']' -> {
                        out.append(']')
                        i++
                        return
                    }
                    c == '\\' -> escape(inClass = true)
                    c == '-' && i + 1 < pattern.length && pattern[i + 1] == '[' -> {
                        // .NET subtraction: what's left of the class, less the class after '-'.
                        out.append("&&[^")
                        i++
                        characterClass()
                        out.append(']')
                    }
                    c == '[' || c == '&' -> {
                        out.append('\\').append(c)
                        i++
                    }
                    else -> {
                        out.append(c)
                        i++
                    }
                }
            }

            throw IllegalArgumentException("Unterminated [] set in '$pattern'.")
        }

        /** An escape at [i] (its backslash). */
        private fun escape(inClass: Boolean) {
            require(i + 1 < pattern.length) { "Illegal \\ at end of pattern '$pattern'." }
            val c = pattern[i + 1]
            i += 2

            when (c) {
                'd' -> out.append(DIGIT_CLASS)
                'D' -> out.append(NON_DIGIT_CLASS)
                'w' -> out.append(WORD_CLASS)
                'W' -> out.append(NON_WORD_CLASS)
                's' -> out.append(DotNetText.WHITESPACE_CLASS)
                'S' -> out.append(DotNetText.NON_WHITESPACE_CLASS)
                'b' -> out.append(if (inClass) "\\u0008" else BOUNDARY)
                'B' -> if (inClass) out.append("\\B") else out.append(NON_BOUNDARY)
                '0' -> {
                    // Up to two more octal digits.
                    var end = i
                    while (end < pattern.length && end < i + 2 && pattern[end] in '0'..'7') end++
                    val code = if (end == i) 0 else pattern.substring(i, end).toInt(8)
                    out.append(String.format("\\u%04X", code))
                    i = end
                }
                'p', 'P' -> {
                    out.append('\\').append(c)
                    // The name in braces, through its '}', as one piece (not a literal '}').
                    if (i < pattern.length && pattern[i] == '{') {
                        val end = pattern.indexOf('}', i)
                        require(end >= 0) { "Incomplete \\p{X} character escape in '$pattern'." }
                        val name = pattern.substring(i + 1, end)
                        out.append('{').append(if (name.startsWith("Is")) "In" + name.substring(2) else name).append('}')
                        i = end + 1
                    }
                }
                'k' -> {
                    if (i < pattern.length && pattern[i] == '\'') {
                        val end = pattern.indexOf('\'', i + 1)
                        require(end >= 0) { "Unterminated \\k'name' in '$pattern'." }
                        out.append("\\k<").append(pattern, i + 1, end).append('>')
                        i = end + 1
                    } else {
                        out.append("\\k")
                    }
                }
                else -> out.append('\\').append(c)
            }
        }
    }
}
