package com.mrjackspade.deaddit.core.markdown

/**
 * .NET string semantics the ported converters depend on, where Kotlin's built-ins differ:
 * `char.IsWhiteSpace` (and so `Trim`/`TrimStart`/`IsNullOrWhiteSpace`), and
 * `HttpUtility.HtmlDecode` (= `WebUtility.HtmlDecode`: numeric references and the HTML 4.01 entity set plus
 * `&apos;`, semicolon required, unknown entities left as-is).
 */
internal object DotNetText {
    /** .NET `char.IsWhiteSpace`. Kotlin's `isWhitespace` also accepts U+001C..U+001F and rejects U+0085. */
    fun isWhiteSpace(c: Char): Boolean {
        if (c.code <= 0xFF) {
            return c == ' ' || c.code in 0x09..0x0D || c.code == 0xA0 || c.code == 0x85
        }

        return when (Character.getType(c).toByte()) {
            Character.SPACE_SEPARATOR, Character.LINE_SEPARATOR, Character.PARAGRAPH_SEPARATOR -> true
            else -> false
        }
    }

    fun trimStart(s: String): String = s.trimStart(::isWhiteSpace)

    fun trim(s: String): String = s.trim(::isWhiteSpace)

    fun isNullOrWhiteSpace(s: String?): Boolean = s == null || s.all(::isWhiteSpace)

    /** .NET `WebUtility.HtmlDecode`. */
    fun htmlDecode(value: String?): String? {
        if (value.isNullOrEmpty() || value.indexOf('&') < 0) {
            return value
        }

        val output = StringBuilder(value.length)
        var i = 0

        while (i < value.length) {
            var ch = value[i]

            if (ch == '&') {
                // The entity runs to the next ';' — unless another '&' comes first, which may start a real one.
                val sliceStart = i + 1
                val semicolon = value.indexOfAny(charArrayOf(';', '&'), sliceStart)

                if (semicolon >= 0 && value[semicolon] == ';') {
                    val entityLength = semicolon - sliceStart
                    val entityEndPosition = semicolon

                    if (entityLength > 1 && value[sliceStart] == '#') {
                        val hex = value[sliceStart + 1] == 'x' || value[sliceStart + 1] == 'X'
                        val parsed: Long? = if (hex) {
                            parseUIntHex(value.substring(sliceStart + 2, semicolon))
                        } else {
                            parseUIntInteger(value.substring(sliceStart + 1, semicolon))
                        }

                        if (parsed != null && (parsed < 0xD800L || (0xDFFFL < parsed && parsed <= 0x10FFFFL))) {
                            output.appendCodePoint(parsed.toInt())
                            i = entityEndPosition + 1
                            continue
                        }
                    } else {
                        val entity = value.substring(sliceStart, semicolon)
                        i = entityEndPosition
                        val entityChar = HTML_ENTITIES[entity]

                        if (entityChar != null) {
                            ch = entityChar
                        } else {
                            output.append('&')
                            output.append(entity)
                            output.append(';')
                            i++
                            continue
                        }
                    }
                }
            }

            output.append(ch)
            i++
        }

        return output.toString()
    }

    /** `uint.TryParse(s, NumberStyles.AllowHexSpecifier, InvariantCulture)`: hex digits only, no sign or whitespace. */
    private fun parseUIntHex(s: String): Long? {
        if (s.isEmpty()) {
            return null
        }

        var result = 0L

        for (c in s) {
            val digit = Character.digit(c, 16)

            if (digit < 0 || c.code > 0x7F) {
                return null
            }

            result = result * 16 + digit

            if (result > 0xFFFFFFFFL) {
                return null
            }
        }

        return result
    }

    /**
     * `uint.TryParse(s, NumberStyles.Integer, InvariantCulture)`: optional leading/trailing whitespace
     * (U+0009..U+000D and space), an optional sign ("-" only for zero), then ASCII digits.
     */
    private fun parseUIntInteger(s: String): Long? {
        fun isNumberWhite(c: Char) = c == ' ' || c in '\t'..'\r'

        var start = 0
        var end = s.length

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

            if (result > 0xFFFFFFFFL) {
                return null
            }
        }

        if (negative && result != 0L) {
            return null
        }

        return result
    }

    /** .NET `WebUtility.HtmlEntities`: the HTML 4.01 named entities plus `apos`. */
    private val HTML_ENTITIES: Map<String, Char> = buildMap {
        put("quot", '"'); put("amp", '&'); put("apos", '\''); put("lt", '<'); put("gt", '>')

        val latin1 = listOf(
            "nbsp", "iexcl", "cent", "pound", "curren", "yen", "brvbar", "sect", "uml", "copy", "ordf", "laquo",
            "not", "shy", "reg", "macr", "deg", "plusmn", "sup2", "sup3", "acute", "micro", "para", "middot",
            "cedil", "sup1", "ordm", "raquo", "frac14", "frac12", "frac34", "iquest", "Agrave", "Aacute", "Acirc",
            "Atilde", "Auml", "Aring", "AElig", "Ccedil", "Egrave", "Eacute", "Ecirc", "Euml", "Igrave", "Iacute",
            "Icirc", "Iuml", "ETH", "Ntilde", "Ograve", "Oacute", "Ocirc", "Otilde", "Ouml", "times", "Oslash",
            "Ugrave", "Uacute", "Ucirc", "Uuml", "Yacute", "THORN", "szlig", "agrave", "aacute", "acirc", "atilde",
            "auml", "aring", "aelig", "ccedil", "egrave", "eacute", "ecirc", "euml", "igrave", "iacute", "icirc",
            "iuml", "eth", "ntilde", "ograve", "oacute", "ocirc", "otilde", "ouml", "divide", "oslash", "ugrave",
            "uacute", "ucirc", "uuml", "yacute", "thorn", "yuml",
        )
        latin1.forEachIndexed { index, name -> put(name, Char(0xA0 + index)) }

        val greekUpper = listOf(
            "Alpha", "Beta", "Gamma", "Delta", "Epsilon", "Zeta", "Eta", "Theta", "Iota", "Kappa", "Lambda", "Mu",
            "Nu", "Xi", "Omicron", "Pi", "Rho", null, "Sigma", "Tau", "Upsilon", "Phi", "Chi", "Psi", "Omega",
        )
        greekUpper.forEachIndexed { index, name -> if (name != null) put(name, Char(0x391 + index)) }

        val greekLower = listOf(
            "alpha", "beta", "gamma", "delta", "epsilon", "zeta", "eta", "theta", "iota", "kappa", "lambda", "mu",
            "nu", "xi", "omicron", "pi", "rho", "sigmaf", "sigma", "tau", "upsilon", "phi", "chi", "psi", "omega",
        )
        greekLower.forEachIndexed { index, name -> put(name, Char(0x3B1 + index)) }

        val other = listOf(
            "OElig" to 338, "oelig" to 339, "Scaron" to 352, "scaron" to 353, "Yuml" to 376, "fnof" to 402,
            "circ" to 710, "tilde" to 732, "thetasym" to 977, "upsih" to 978, "piv" to 982,
            "ensp" to 8194, "emsp" to 8195, "thinsp" to 8201, "zwnj" to 8204, "zwj" to 8205, "lrm" to 8206,
            "rlm" to 8207, "ndash" to 8211, "mdash" to 8212, "lsquo" to 8216, "rsquo" to 8217, "sbquo" to 8218,
            "ldquo" to 8220, "rdquo" to 8221, "bdquo" to 8222, "dagger" to 8224, "Dagger" to 8225, "bull" to 8226,
            "hellip" to 8230, "permil" to 8240, "prime" to 8242, "Prime" to 8243, "lsaquo" to 8249,
            "rsaquo" to 8250, "oline" to 8254, "frasl" to 8260, "euro" to 8364, "image" to 8465, "weierp" to 8472,
            "real" to 8476, "trade" to 8482, "alefsym" to 8501, "larr" to 8592, "uarr" to 8593, "rarr" to 8594,
            "darr" to 8595, "harr" to 8596, "crarr" to 8629, "lArr" to 8656, "uArr" to 8657, "rArr" to 8658,
            "dArr" to 8659, "hArr" to 8660, "forall" to 8704, "part" to 8706, "exist" to 8707, "empty" to 8709,
            "nabla" to 8711, "isin" to 8712, "notin" to 8713, "ni" to 8715, "prod" to 8719, "sum" to 8721,
            "minus" to 8722, "lowast" to 8727, "radic" to 8730, "prop" to 8733, "infin" to 8734, "ang" to 8736,
            "and" to 8743, "or" to 8744, "cap" to 8745, "cup" to 8746, "int" to 8747, "there4" to 8756,
            "sim" to 8764, "cong" to 8773, "asymp" to 8776, "ne" to 8800, "equiv" to 8801, "le" to 8804,
            "ge" to 8805, "sub" to 8834, "sup" to 8835, "nsub" to 8836, "sube" to 8838, "supe" to 8839,
            "oplus" to 8853, "otimes" to 8855, "perp" to 8869, "sdot" to 8901, "lceil" to 8968, "rceil" to 8969,
            "lfloor" to 8970, "rfloor" to 8971, "lang" to 9001, "rang" to 9002, "loz" to 9674, "spades" to 9824,
            "clubs" to 9827, "hearts" to 9829, "diams" to 9830,
        )
        other.forEach { (name, code) -> put(name, Char(code)) }
    }
}
