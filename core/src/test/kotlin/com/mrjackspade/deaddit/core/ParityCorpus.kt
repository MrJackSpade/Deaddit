package com.mrjackspade.deaddit.core

/**
 * Reads the parity corpora in test resources `parity/`: tab-separated rows recorded by running the C# Deaddit.Core
 * code (.NET 10) on each input, before the C# was removed. Fields escape every char outside printable ASCII, and
 * the backslash, as a six-char `\uXXXX`. `<null>` is a null result, `<throw:Type>` an exception of that .NET type,
 * `<skip>` a value that was not recorded because it depends on the OS the C# ran on.
 */
object ParityCorpus {
    fun rows(name: String): List<List<String>> {
        val path = "/com/mrjackspade/deaddit/core/parity/$name"
        val text = (ParityCorpus::class.java.getResourceAsStream(path) ?: throw IllegalStateException("Missing resource $path"))
            .use { it.readBytes().toString(Charsets.UTF_8) }

        return text.lineSequence().filter { it.isNotEmpty() }.map { it.split('\t') }.toList()
    }

    fun decode(field: String): String {
        val result = StringBuilder(field.length)
        var i = 0

        while (i < field.length) {
            if (field[i] == '\\') {
                check(field[i + 1] == 'u') { "Bad escape in '$field'" }
                result.append(field.substring(i + 2, i + 6).toInt(16).toChar())
                i += 6
            } else {
                result.append(field[i])
                i++
            }
        }

        return result.toString()
    }

    /** Runs [block] and renders its result the way the corpus does, mapping exception types to the .NET names. */
    fun outcome(block: () -> String?): String = try {
        block()?.let(::escape) ?: "<null>"
    } catch (e: Exception) {
        "<throw:${dotNetName(e)}>"
    }

    fun escape(s: String): String {
        val result = StringBuilder(s.length)

        for (c in s) {
            if (c.code < 0x20 || c.code > 0x7E || c == '\\') {
                result.append("\\u").append("%04X".format(c.code))
            } else {
                result.append(c)
            }
        }

        return result.toString()
    }

    private fun dotNetName(e: Exception): String = when (e) {
        is com.mrjackspade.deaddit.core.util.UriFormatException -> "UriFormatException"
        is IllegalArgumentException -> if (e.message?.contains("replacement pattern") == true) "RegexParseException" else "ArgumentException"
        is IndexOutOfBoundsException -> "IndexOutOfRangeException"
        else -> e.javaClass.simpleName
    }
}
