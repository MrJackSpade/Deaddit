package com.mrjackspade.deaddit.core.util

/**
 * The typed response headers the C# app read, as .NET 10's System.Net.Http parses them from the raw values its
 * Android handler stored (AndroidMessageHandler adds every header with TryAddWithoutValidation, so a value .NET can't
 * parse reads as null rather than failing the request).
 */
object DotNetHttpHeaders {
    /**
     * `response.Headers.ETag?.Tag`: the quoted entity tag, without any `W/` weakness prefix; null when the header is
     * missing or its value is not a single valid entity tag ("*" included). ETag holds one value: when a response
     * sends several, .NET parses the first and treats the others as invalid.
     */
    fun entityTag(values: List<String>): String? {
        val value = values.firstOrNull() ?: return null
        return parseSingleEntityTag(value)
    }

    /**
     * `response.Headers.Location?.ToString()`: the redirect target as .NET's Uri displays it, unescaped but for
     * '#', '?' and '%'. Null when .NET can't parse it (UriHeaderParser: RelativeOrAbsolute, then again after
     * reading Latin-1 chars as UTF-8 bytes).
     */
    fun location(value: String): String? {
        if (value.isEmpty()) {
            return null
        }
        val uri = DotNetUri.tryCreateRelativeOrAbsolute(value)
            ?: DotNetUri.tryCreateRelativeOrAbsolute(decodeUtf8FromString(value))
            ?: return null
        return uri.toString()
    }

    /** UriHeaderParser.DecodeUtf8FromString: Latin-1 chars taken as UTF-8 bytes, when that is valid UTF-8. */
    private fun decodeUtf8FromString(input: String): String {
        if (input.isBlank() || input.all { it.code <= 0x7F } || input.any { it.code > 0xFF }) {
            return input
        }
        val bytes = ByteArray(input.length) { input[it].code.toByte() }
        val decoder = Charsets.UTF_8.newDecoder()
            .onMalformedInput(java.nio.charset.CodingErrorAction.REPORT)
            .onUnmappableCharacter(java.nio.charset.CodingErrorAction.REPORT)
        return try {
            decoder.decode(java.nio.ByteBuffer.wrap(bytes)).toString()
        } catch (_: java.nio.charset.CharacterCodingException) {
            // .NET's exception-fallback decoder throws ArgumentException here, which DecodeUtf8FromString answers with the input.
            input
        }
    }

    /** GenericHeaderParser.SingleValueEntityTagParser.TryParseValue (BaseHeaderParser) over the whole value. */
    private fun parseSingleEntityTag(value: String): String? {
        if (value.isEmpty()) {
            return null
        }
        val start = nextNonEmptyOrWhitespaceIndex(value, 0) ?: return null
        if (start == value.length) {
            return null
        }
        val (length, tag) = entityTagLength(value, start)
        // ParseSingleEntityTag refuses "*" (EntityTagHeaderValue.Any).
        if (length == 0 || tag == null) {
            return null
        }
        val end = nextNonEmptyOrWhitespaceIndex(value, start + length) ?: return null
        return if (end < value.length) null else tag
    }

    /** HeaderUtilities.GetNextNonEmptyOrWhitespaceIndex for a single-value header: null where it finds a ',' separator. */
    private fun nextNonEmptyOrWhitespaceIndex(input: String, startIndex: Int): Int? {
        val num = startIndex + whitespaceLength(input, startIndex)
        if (num == input.length || input[num] != ',') {
            return num
        }
        return null
    }

    /** EntityTagHeaderValue.GetEntityTagLength: (length, tag), tag null for "*". */
    private fun entityTagLength(input: String, startIndex: Int): Pair<Int, String?> {
        if (input.isEmpty() || startIndex >= input.length) {
            return 0 to null
        }
        var num = startIndex
        val c = input[startIndex]
        val tag: String?
        if (c == '*') {
            tag = null
            num++
        } else {
            if (c == 'W' || c == 'w') {
                num++
                if (num + 2 >= input.length || input[num] != '/') {
                    return 0 to null
                }
                num++
                num += whitespaceLength(input, num)
            }
            if (num == input.length) {
                return 0 to null
            }
            val length = quotedStringLength(input, num) ?: return 0 to null
            tag = input.substring(num, num + length)
            num += length
        }
        num += whitespaceLength(input, num)
        return (num - startIndex) to tag
    }

    private fun whitespaceLength(input: String, startIndex: Int): Int {
        if (startIndex >= input.length) {
            return 0
        }
        for (i in startIndex until input.length) {
            val c = input[i]
            if (c != ' ' && c != '\t') {
                return i - startIndex
            }
        }
        return input.length - startIndex
    }

    /** HttpRuleParser.GetQuotedStringLength (GetExpressionLength without nesting): the length, or null unless Parsed. */
    private fun quotedStringLength(input: String, startIndex: Int): Int? {
        if (input[startIndex] != '"') {
            return null
        }
        var num = startIndex + 1
        while (num < input.length) {
            if (num + 2 < input.length && isQuotedPair(input, num)) {
                num += 2
                continue
            }
            val c = input[num]
            if (c == '\r' || c == '\n' || c == '\u0000') {
                return null
            }
            if (c == '"') {
                return num - startIndex + 1
            }
            num++
        }
        return null
    }

    /** HttpRuleParser.GetQuotedPairLength(...) == Parsed. */
    private fun isQuotedPair(input: String, startIndex: Int): Boolean {
        if (input[startIndex] != '\\') {
            return false
        }
        if (startIndex + 2 > input.length) {
            return false
        }
        val c = input[startIndex + 1]
        return !(c > '\u007f' || c == '\u0000' || c == '\n' || c == '\r')
    }
}
