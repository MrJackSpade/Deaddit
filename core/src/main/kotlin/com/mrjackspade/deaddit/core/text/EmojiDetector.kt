package com.mrjackspade.deaddit.core.text

/**
 * Finds emoji with the C# pattern `\p{So}|\p{Cs}\p{Cs}(\p{Cf}\p{Cs}\p{Cs})*`.
 *
 * That pattern only means what it says because .NET regexes run over UTF-16 chars: an emoji outside the BMP is
 * two chars of category Cs (surrogate), and a ZWJ sequence is surrogate pairs joined by a Cf char. java.util.regex
 * runs over code points, where `\p{Cs}` never matches a paired surrogate, so the pattern is matched here by hand,
 * one UTF-16 char at a time, exactly as .NET's engine walks it: leftmost match, alternatives in order, the group
 * greedy, scanning resumes after each match.
 */
object EmojiDetector {
    fun isMatch(str: String): Boolean {
        for (i in str.indices) {
            if (matchAt(str, i) != null) {
                return true
            }
        }

        return false
    }

    /**
     * Replaces every match with [replacement], which is a .NET substitution pattern as in `Regex.Replace`:
     * `$0`/`$&` the match, `$1`/`$+` the last `(\p{Cf}\p{Cs}\p{Cs})` repetition (empty when there is none),
     * `${0}`/`${1}`, `` $` `` the text before the match, `$'` the text after, `$_` the whole input, `$$` a literal "$".
     * Any other "$" is literal.
     */
    fun replace(inStr: String, replacement: String): String {
        val substitution = parseSubstitution(replacement)
        val result = StringBuilder(inStr.length)
        var position = 0
        var i = 0

        while (i < inStr.length) {
            val match = matchAt(inStr, i)

            if (match == null) {
                i++
                continue
            }

            result.append(inStr, position, match.start)

            for (part in substitution) {
                when (part) {
                    is Part.Literal -> result.append(part.text)
                    Part.Match -> result.append(inStr, match.start, match.end)
                    Part.Group1 -> if (match.groupStart >= 0) result.append(inStr, match.groupStart, match.groupEnd)
                    Part.Before -> result.append(inStr, 0, match.start)
                    Part.After -> result.append(inStr, match.end, inStr.length)
                    Part.Input -> result.append(inStr)
                }
            }

            position = match.end
            i = match.end
        }

        result.append(inStr, position, inStr.length)

        return result.toString()
    }

    private class Match(val start: Int, val end: Int, val groupStart: Int, val groupEnd: Int)

    private fun matchAt(s: String, i: Int): Match? {
        if (Character.getType(s[i]) == Character.OTHER_SYMBOL.toInt()) {
            return Match(i, i + 1, -1, -1)
        }

        if (!isSurrogate(s, i) || !isSurrogate(s, i + 1)) {
            return null
        }

        var end = i + 2
        var groupStart = -1

        while (isFormat(s, end) && isSurrogate(s, end + 1) && isSurrogate(s, end + 2)) {
            groupStart = end
            end += 3
        }

        return Match(i, end, groupStart, if (groupStart >= 0) end else -1)
    }

    private fun isSurrogate(s: String, i: Int): Boolean = i < s.length && Character.isSurrogate(s[i])

    private fun isFormat(s: String, i: Int): Boolean = i < s.length && Character.getType(s[i]) == Character.FORMAT.toInt()

    private sealed interface Part {
        class Literal(val text: String) : Part
        data object Match : Part
        data object Group1 : Part
        data object Before : Part
        data object After : Part
        data object Input : Part
    }

    // .NET's RegexParser.ParseReplacement for a pattern whose only groups are 0 and 1, without ECMAScript.
    private fun parseSubstitution(replacement: String): List<Part> {
        val parts = ArrayList<Part>()
        val literal = StringBuilder()

        fun add(part: Part) {
            if (literal.isNotEmpty()) {
                parts.add(Part.Literal(literal.toString()))
                literal.setLength(0)
            }

            parts.add(part)
        }

        fun group(number: Int): Part? = when (number) {
            0 -> Part.Match
            1 -> Part.Group1
            else -> null
        }

        var i = 0

        while (i < replacement.length) {
            val c = replacement[i++]

            if (c != '$' || i >= replacement.length) {
                literal.append(c)
                continue
            }

            val next = replacement[i]

            if (next == '$') {
                literal.append('$')
                i++
                continue
            }

            val angled = next == '{' && i + 1 < replacement.length
            var j = if (angled) i + 1 else i

            if (j < replacement.length && replacement[j] in '0'..'9') {
                val (number, afterDigits) = scanDecimal(replacement, j)
                j = afterDigits
                val part = group(number)

                if (part != null && (!angled || (j < replacement.length && replacement[j] == '}'))) {
                    add(part)
                    i = if (angled) j + 1 else j
                    continue
                }
            } else if (angled) {
                // ${name}: no named groups exist, so it is always literal.
            } else {
                val part = when (next) {
                    '&' -> Part.Match
                    '`' -> Part.Before
                    '\'' -> Part.After
                    '+' -> Part.Group1
                    '_' -> Part.Input
                    else -> null
                }

                if (part != null) {
                    add(part)
                    i++
                    continue
                }
            }

            literal.append('$')
        }

        if (literal.isNotEmpty()) {
            parts.add(Part.Literal(literal.toString()))
        }

        return parts
    }

    // RegexParser.ScanDecimal: every following digit, throwing on overflow past int.MaxValue.
    private fun scanDecimal(s: String, start: Int): Pair<Int, Int> {
        var value = 0
        var i = start

        while (i < s.length && s[i] in '0'..'9') {
            val digit = s[i] - '0'

            if (value > Int.MAX_VALUE / 10 || (value == Int.MAX_VALUE / 10 && digit > Int.MAX_VALUE % 10)) {
                throw IllegalArgumentException("Capture group number out of range in replacement pattern '$s'")
            }

            value = value * 10 + digit
            i++
        }

        return value to i
    }
}
