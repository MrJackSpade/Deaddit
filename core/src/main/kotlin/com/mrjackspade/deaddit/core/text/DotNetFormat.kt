package com.mrjackspade.deaddit.core.text

/** What .NET's string.Format throws for a malformed format or an index past the arguments. */
class FormatException(message: String) : IllegalArgumentException(message)

/**
 * .NET's string.Format, for the C# app's prompt templates: "{0}" items (with optional alignment
 * and format, which a string argument ignores), "{{" and "}}" escapes, a null argument as empty.
 */
object DotNetFormat {
    private const val BAD_FORMAT = "Input string was not in a correct format."
    private const val BAD_INDEX = "Index (zero based) must be greater than or equal to zero and less than the size of the argument list."

    fun format(format: String, vararg args: String?): String {
        val out = StringBuilder()
        var i = 0

        while (i < format.length) {
            val c = format[i]

            if (c == '}') {
                if (i + 1 < format.length && format[i + 1] == '}') {
                    out.append('}')
                    i += 2
                    continue
                }
                throw FormatException(BAD_FORMAT)
            }

            if (c != '{') {
                out.append(c)
                i++
                continue
            }

            if (i + 1 < format.length && format[i + 1] == '{') {
                out.append('{')
                i += 2
                continue
            }

            // An item: {index[,alignment][:format]}
            i++
            if (i >= format.length || !format[i].isAsciiDigit()) throw FormatException(BAD_FORMAT)
            var index = 0
            while (i < format.length && format[i].isAsciiDigit()) {
                index = index * 10 + (format[i] - '0')
                if (index >= 1_000_000) throw FormatException(BAD_FORMAT)
                i++
            }
            while (i < format.length && format[i] == ' ') i++

            var alignment = 0
            if (i < format.length && format[i] == ',') {
                i++
                while (i < format.length && format[i] == ' ') i++
                val negative = i < format.length && format[i] == '-'
                if (negative) i++
                if (i >= format.length || !format[i].isAsciiDigit()) throw FormatException(BAD_FORMAT)
                while (i < format.length && format[i].isAsciiDigit()) {
                    alignment = alignment * 10 + (format[i] - '0')
                    if (alignment >= 1_000_000) throw FormatException(BAD_FORMAT)
                    i++
                }
                if (negative) alignment = -alignment
                while (i < format.length && format[i] == ' ') i++
            }

            if (i < format.length && format[i] == ':') {
                // A string ignores its format; it runs to the closing brace.
                i++
                while (i < format.length && format[i] != '}') {
                    if (format[i] == '{') throw FormatException(BAD_FORMAT)
                    i++
                }
            }

            if (i >= format.length || format[i] != '}') throw FormatException(BAD_FORMAT)
            i++

            if (index >= args.size) throw FormatException(BAD_INDEX)

            val value = args[index] ?: ""
            when {
                alignment > value.length -> out.append(" ".repeat(alignment - value.length)).append(value)
                -alignment > value.length -> out.append(value).append(" ".repeat(-alignment - value.length))
                else -> out.append(value)
            }
        }

        return out.toString()
    }

    private fun Char.isAsciiDigit() = this in '0'..'9'
}
