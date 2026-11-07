package com.mrjackspade.deaddit.core.util

import com.mrjackspade.deaddit.core.text.DotNetText
import java.text.Normalizer

/** .NET's System.UriFormatException: a string that System.Uri refuses as an absolute URI. */
class UriFormatException(message: String) : IllegalArgumentException(message)

/**
 * A port of the parts of .NET's System.Uri (as shipped in .NET 10, System.Private.Uri) the app relies on: parsing
 * (`new Uri(s)`, `Uri.TryCreate(s, UriKind.Absolute, out uri)`, `new Uri(s, UriKind.RelativeOrAbsolute)`),
 * resolving a reference against a base (`new Uri(baseUri, relative)`), [scheme], [host], [absolutePath],
 * [absoluteUri], [getLeftPartPath] and [toString].
 *
 * The C# app's results hinge on System.Uri's exact acceptance rules and canonicalization (IPv4 forms like
 * "0x7f.1", IDN hosts, lowercased hosts, underscores in hosts, dot-segment removal, percent-decoding of unreserved
 * chars, backslashes, spaces), which java.net.URI does not share, so the .NET code is ported here method by method,
 * keeping its names and flow so it can be checked against the decompiled source. Scope: built-in schemes and
 * unregistered ("V1 unknown") schemes, never user-escaped, path/query canonicalization on, running on a non-Windows
 * OS (Android), so Windows-only branches are left out. UriComponents.NormalizedHost is not ported.
 *
 * Where .NET reads through a char pointer it can read the string's terminating NUL; [at] reproduces that.
 */
class DotNetUri private constructor(private var string: String) {
    private var flags = 0L
    private var syntax: Syntax? = null
    private var originalUnicodeString: String? = null
    private var info: UriInfo? = null

    /** Uri.IsAbsoluteUri: false for a relative reference made by [createRelativeOrAbsolute]. */
    val isAbsoluteUri: Boolean get() = syntax != null

    /** Uri.Scheme. */
    val scheme: String get() = absoluteSyntax.schemeName

    /** Uri.Host: lowercased for DNS hosts, as .NET canonicalizes them. */
    val host: String get() = getParts(C_HOST, FMT_URI_ESCAPED)

    /** Uri.AbsolutePath. */
    val absolutePath: String
        get() {
            val text = getParts(C_PATH or C_KEEP_DELIMITER, FMT_URI_ESCAPED)
            return if (isDosPath && text[0] == '/') text.substring(1) else text
        }

    /** Uri.AbsoluteUri: the escaped, canonical form, which is what .NET's HTTP handlers put on the wire. */
    val absoluteUri: String get() = getParts(C_ABSOLUTE_URI, FMT_URI_ESCAPED)

    /** Uri.OriginalString. */
    val originalString: String get() = originalUnicodeString ?: string

    /** Uri.GetLeftPart(UriPartial.Path). */
    fun getLeftPartPath(): String {
        ensureUriInfo()
        return getParts(C_SCHEME_AND_SERVER or C_USER_INFO or C_PATH, FMT_URI_ESCAPED)
    }

    /** Uri.ToString(): the canonical form with everything unescaped but '#', '?' and '%'. A relative reference is returned as given. */
    override fun toString(): String {
        if (syntax == null) {
            return string
        }
        ensureUriInfo()
        return getComponentsHelper(C_ABSOLUTE_URI, FMT_V1_TO_STRING)
    }

    private val absoluteSyntax: Syntax
        get() = syntax ?: throw IllegalStateException("This operation is not supported for a relative URI.")

    /** Uri.GetParts / InternalGetComponents for the simple (built-in) parsers. */
    private fun getParts(uriParts: Int, formatAs: Int): String {
        absoluteSyntax
        return getComponentsHelper(uriParts, formatAs)
    }

    // region Syntax

    internal class Syntax(val schemeName: String, val defaultPort: Int, flags: Int) {
        val flags = flags or S_SIMPLE_USER_SYNTAX

        fun inFact(f: Int) = (flags and f) != 0
        fun notAny(f: Int) = (flags and f) == 0
        fun isAllSet(f: Int) = (flags and f) == f
    }

    private class UriInfo {
        var scheme = 0
        var user = 0
        var host = 0
        var portValue = 0
        var path = 0
        var query = 0
        var fragment = 0
        var end = 0
        var hostString: String? = null
        var scopeId: String? = null
    }

    private class IntRef(var value: Int)

    // endregion

    // region Flags helpers

    private fun notAny(f: Long) = (flags and f) == 0L
    private fun inFact(f: Long) = (flags and f) != 0L
    private val hostType: Long get() = flags and F_HOST_TYPE_MASK
    private val isImplicitFile: Boolean get() = inFact(F_IMPLICIT_FILE)
    private val isDosPath: Boolean get() = inFact(F_DOS_PATH)
    private val isUncPath: Boolean get() = inFact(F_UNC_PATH)
    private val isUnixPath: Boolean get() = inFact(F_UNIX_PATH)

    /** Uri.DnsSafeHost. */
    private val dnsSafeHost: String
        get() {
            ensureHostString()
            val info = info!!
            val text = info.hostString!!
            return when {
                hostType == F_IPV6_HOST_TYPE -> text.substring(1, text.length - 1) + (info.scopeId ?: "")
                hostType == F_BASIC_HOST_TYPE && inFact(F_HOST_NOT_CANONICAL or F_E_HOST_NOT_CANONICAL) -> StringBuilder().also {
                    unescapeString(text, 0, text.length, it, '￿', '￿', '￿', U_UNESCAPE or U_UNESCAPE_ALL, syntax, isQuery = false)
                }.toString()
                else -> text
            }
        }

    private val securedPathIndex: Int
        get() {
            if (isDosPath) {
                val c = string[info!!.path]
                return if (c != '/' && c != '\\') 2 else 3
            }
            return 0
        }

    // endregion

    // region Creation

    /**
     * InitializeUri for UriKind.Absolute ([allowRelative] false) or UriKind.RelativeOrAbsolute. [err] is
     * ParseScheme's result. A relative reference leaves [syntax] null.
     */
    private fun initializeUri(err: Int, allowRelative: Boolean): UriFormatException? {
        if (err == E_NONE) {
            if (isImplicitFile && allowRelative && notAny(F_DOS_PATH) &&
                ((string.length >= 2 && (string[0] != '\\' || string[1] != '\\')) || isUnixPath)
            ) {
                syntax = null
                flags = 0L
                return null
            }
        } else if (err > E_LAST_ERROR_OKAY_FOR_RELATIVE_URIS) {
            return exception(err)
        }

        var flag = false

        if (checkForUnicodeOrEscapedUnreserved(string)) {
            flags = flags or F_HAS_UNICODE
            flag = true
            originalUnicodeString = string
        }

        if (syntax != null) {
            var e: UriFormatException? = null
            val err2 = privateParseMinimal()

            if (err2 != E_NONE) {
                if (allowRelative && err2 <= E_LAST_ERROR_OKAY_FOR_RELATIVE_URIS) {
                    syntax = null
                    flags = 0L
                    return null
                }
                e = exception(err2)
            }

            if (flag) {
                try {
                    ensureParseRemaining()
                } catch (ex: UriFormatException) {
                    e = ex
                }
            }

            return e
        }

        if (allowRelative && err != E_NONE && err <= E_LAST_ERROR_OKAY_FOR_RELATIVE_URIS) {
            flags = flags and F_HAS_UNICODE
            if (flag) {
                string = escapeUnescapeIri(originalUnicodeString!!, 0, originalUnicodeString!!.length, 0)
            }
            return null
        }

        return exception(err)
    }

    private fun parseScheme(): Int {
        val uriString = string

        if (uriString.isEmpty()) {
            return E_EMPTY_URI_STRING
        }

        if (startsWithOrdinalIgnoreCase(uriString, "https:")) {
            syntax = HTTPS
            flags = flags or F_USER_NOT_CANONICAL or F_HOST_NOT_CANONICAL
        } else if (startsWithOrdinalIgnoreCase(uriString, "http:")) {
            syntax = HTTP
            flags = flags or F_SCHEME_NOT_CANONICAL or F_HOST_NOT_CANONICAL
        } else {
            val err = IntRef(E_NONE)
            val num = parseSchemeCheckImplicitFile(uriString, err)

            if (err.value != E_NONE) {
                return err.value
            }

            flags = flags or num.toLong()
        }

        return E_NONE
    }

    private fun parseSchemeCheckImplicitFile(uriString: String, err: IntRef): Int {
        var i = 0
        while (i < uriString.length && isLWS(uriString[i])) {
            i++
        }

        // Not Windows: a path starting with one '/' is a Unix file path.
        var flag = i < uriString.length && uriString[i] == '/'
        if (flag) {
            flag = i + 1 >= uriString.length || (uriString[i + 1] != '/' && uriString[i + 1] != '\\')
        }

        if (flag) {
            flags = flags or F_AUTHORITY_FOUND or F_IMPLICIT_FILE or F_UNIX_PATH
            syntax = UNIX_FILE
            return i
        }

        val num = uriString.indexOf(':', i).let { if (it < 0) -1 else it - i }

        if (i + 2 >= uriString.length || num == 0 || i >= uriString.length || i + 1 >= uriString.length) {
            err.value = E_BAD_FORMAT
            return 0
        }

        var c = uriString[i + 1]
        if (c == ':' || c == '|') {
            if (isAsciiLetter(uriString[i])) {
                c = uriString[i + 2]
                if (c == '/' || c == '\\') {
                    flags = flags or F_AUTHORITY_FOUND or F_DOS_PATH or F_IMPLICIT_FILE
                    syntax = FILE
                    return i
                }
                err.value = E_MUST_ROOTED_PATH
                return 0
            }
            err.value = if (uriString[i + 1] != ':') E_BAD_FORMAT else E_BAD_SCHEME
            return 0
        }

        c = uriString[i]
        if (c == '/' || c == '\\') {
            c = uriString[i + 1]
            if (c == '/' || c == '\\') {
                flags = flags or F_AUTHORITY_FOUND or F_UNC_PATH or F_IMPLICIT_FILE
                syntax = FILE
                i += 2
                while (i < uriString.length && (uriString[i] == '/' || uriString[i] == '\\')) {
                    i++
                }
                return i
            }
            err.value = E_BAD_FORMAT
            return 0
        }

        if (num < 0) {
            err.value = E_BAD_FORMAT
            return 0
        }

        val found = checkSchemeSyntax(uriString.substring(i, i + num), err) ?: return 0
        syntax = found
        return i + num + 1
    }

    private fun privateParseMinimal(): Int {
        val syntax = syntax!!
        var num = (flags and F_INDEX_MASK).toInt()
        var num2 = string.length
        flags = flags and (F_INDEX_MASK or F_USER_DRIVEN_PARSING).inv()

        if (num2 > num && isLWS(string[num2 - 1])) {
            num2--
            while (num2 != num) {
                num2--
                if (!isLWS(string[num2])) {
                    break
                }
            }
            num2++
        }

        if (inFact(F_UNIX_PATH)) {
            flags = flags or F_BASIC_HOST_TYPE
            flags = flags or num.toLong()
            return E_NONE
        }

        if (syntax.isAllSet(S_ALLOW_EMPTY_HOST or S_ALLOW_DOS_PATH) && notAny(F_IMPLICIT_FILE) && num + 1 < num2) {
            var i = num
            while (i < num2) {
                val c = string[i]
                if (c != '\\' && c != '/') {
                    break
                }
                i++
            }

            if (syntax.inFact(S_FILE_LIKE_URI) || i - num <= 3) {
                if (i - num >= 2) {
                    flags = flags or F_AUTHORITY_FOUND
                }

                if (i + 1 < num2 && (string[i + 1] == ':' || string[i + 1] == '|') && isAsciiLetter(string[i])) {
                    if (i + 2 >= num2 || (string[i + 2] != '\\' && string[i + 2] != '/')) {
                        if (syntax.inFact(S_FILE_LIKE_URI)) {
                            return E_MUST_ROOTED_PATH
                        }
                    } else {
                        flags = flags or F_DOS_PATH
                        if (syntax.inFact(S_MUST_HAVE_AUTHORITY)) {
                            flags = flags or F_AUTHORITY_FOUND
                        }
                        num = if (i == num || i - num == 2) i else i - 1
                    }
                } else if (syntax.inFact(S_FILE_LIKE_URI) && i - num >= 2 && i - num != 3 && i < num2 && string[i] != '?' && string[i] != '#') {
                    flags = flags or F_UNC_PATH
                    num = i
                } else if (syntax.inFact(S_FILE_LIKE_URI) && string[i - 1] == '/' && i - num == 3) {
                    this.syntax = UNIX_FILE
                    flags = flags or F_AUTHORITY_FOUND or F_UNIX_PATH
                    num += 2
                }
            }
        }

        val syntaxNow = this.syntax!!

        if ((flags and (F_DOS_PATH or F_UNC_PATH or F_UNIX_PATH)) == 0L) {
            if (num + 2 <= num2) {
                val c2 = string[num]
                val c3 = string[num + 1]

                if (syntaxNow.inFact(S_MUST_HAVE_AUTHORITY)) {
                    if ((c2 != '/' && c2 != '\\') || (c3 != '/' && c3 != '\\')) {
                        return E_BAD_AUTHORITY
                    }
                    flags = flags or F_AUTHORITY_FOUND
                    num += 2
                } else if (syntaxNow.inFact(S_OPTIONAL_AUTHORITY) && (inFact(F_AUTHORITY_FOUND) || (c2 == '/' && c3 == '/'))) {
                    flags = flags or F_AUTHORITY_FOUND
                    num += 2
                } else if (syntaxNow.notAny(S_MAIL_TO_LIKE_URI)) {
                    if (inFact(F_HAS_UNICODE)) {
                        string = string.substring(0, num)
                    }
                    flags = flags or (num.toLong() or F_HOST_TYPE_MASK)
                    return E_NONE
                }
            } else {
                if (syntaxNow.inFact(S_MUST_HAVE_AUTHORITY)) {
                    return E_BAD_AUTHORITY
                }
                if (syntaxNow.notAny(S_MAIL_TO_LIKE_URI)) {
                    if (inFact(F_HAS_UNICODE)) {
                        string = string.substring(0, num)
                    }
                    flags = flags or (num.toLong() or F_HOST_TYPE_MASK)
                    return E_NONE
                }
            }
        }

        if (inFact(F_DOS_PATH)) {
            flags = flags or (if (inFact(F_AUTHORITY_FOUND)) F_BASIC_HOST_TYPE else F_HOST_TYPE_MASK)
            flags = flags or num.toLong()
            return E_NONE
        }

        val err = IntRef(E_NONE)
        val newHost = arrayOfNulls<String>(1)
        num = checkAuthorityHelper(num, num2, err, newHost)

        if (err.value != E_NONE) {
            return err.value
        }

        if (num < num2) {
            val c4 = string[num]
            if (c4 == '\\' && notAny(F_IMPLICIT_FILE) && this.syntax!!.notAny(S_ALLOW_DOS_PATH)) {
                return E_BAD_AUTHORITY_TERMINATOR
            }
            if (c4 == '/' && notAny(F_IMPLICIT_FILE) && inFact(F_UNC_PATH) && this.syntax === FILE) {
                this.syntax = UNIX_FILE
            }
        }

        if (newHost[0] != null) {
            string = newHost[0]!!
        }

        flags = flags or num.toLong()
        return E_NONE
    }

    private fun checkAuthorityHelper(idxIn: Int, length: Int, err: IntRef, newHostOut: Array<String?>): Int {
        val pString = string
        val syntax = syntax!!
        var idx = idxIn
        val end = IntRef(length)
        val num = idx
        var j = idx
        var newHost: String? = null
        val flag = inFact(F_HAS_UNICODE)
        val flags2 = syntax.flags

        if (flag) {
            newHost = originalUnicodeString!!.substring(0, num)
        }

        var c = '\u0000'
        if (idx == length || run { c = pString[idx]; c == '/' } || (c == '\\' && isFile(syntax)) || c == '#' || c == '?') {
            if (syntax.inFact(S_ALLOW_EMPTY_HOST)) {
                flags = flags and F_UNC_PATH.inv()
                if (inFact(F_IMPLICIT_FILE)) {
                    err.value = E_BAD_HOST_NAME
                } else {
                    flags = flags or F_BASIC_HOST_TYPE
                }
            } else {
                err.value = E_BAD_HOST_NAME
            }
            newHostOut[0] = newHost
            return idx
        }

        if ((flags2 and S_MAY_HAVE_USER_INFO) != 0) {
            while (j < end.value) {
                if (j == end.value - 1 || pString[j] == '?' || pString[j] == '#' || pString[j] == '\\' || pString[j] == '/') {
                    j = idx
                    break
                }
                if (pString[j] == '@') {
                    flags = flags or F_HAS_USER_INFO
                    if (flag) {
                        newHost += escapeUnescapeIri(pString, num, j + 1, C_USER_INFO)
                    }
                    j++
                    c = pString[j]
                    break
                }
                j++
            }
        }

        val notImplicitFile = notAny(F_IMPLICIT_FILE)

        if (c == '[' && syntax.inFact(S_ALLOW_IPV6_HOST) && ipv6IsValid(pString, j + 1, end)) {
            flags = flags or F_IPV6_HOST_TYPE
            if (flag) {
                newHost += pString.substring(j, end.value)
            }
        } else if (c in '0'..'9' && syntax.inFact(S_ALLOW_IPV4_HOST) &&
            ipv4IsValid(pString, j, end, allowIPv6 = false, notImplicitFile = notImplicitFile, unknownScheme = syntax.inFact(S_V1_UNKNOWN_URI))
        ) {
            flags = flags or F_IPV4_HOST_TYPE
            if (flag) {
                newHost += pString.substring(j, end.value)
            }
        } else if ((flags2 and S_ALLOW_DNS_HOST) != 0 && (flag || syntax.inFact(S_ALLOW_IDN)) &&
            run {
                val length2 = domainNameIsValid(pString, j, end.value, iri = true, notImplicitFile = notImplicitFile)
                if (length2 >= 0) end.value = j + length2
                length2 >= 0
            }
        ) {
            // (.NET first tries a non-IRI DNS branch, which only applies to schemes without AllowIriParsing:
            // none of the schemes handled here.)
            flags = flags or F_DNS_HOST_TYPE
            if (flag) {
                var span = pString.substring(j, end.value)
                stripBidiControlCharacters(span)?.let { span = it }
                try {
                    newHost = normalizeAndConcat(newHost, span)
                } catch (_: IllegalArgumentException) {
                    err.value = E_BAD_HOST_NAME
                }
            }
        } else if ((flags2 and S_ALLOW_UNC_HOST) != 0 && uncIsValid(pString, j, end, notImplicitFile) && end.value - j <= 256) {
            flags = flags or F_UNC_HOST_TYPE
            if (flag) {
                newHost += pString.substring(j, end.value)
            }
        }

        var i = end.value

        if (i < length && pString[i] == '\\' && (flags and F_HOST_TYPE_MASK) != 0L && !isFile(syntax)) {
            if (syntax.inFact(S_V1_UNKNOWN_URI)) {
                err.value = E_BAD_HOST_NAME
                flags = flags or F_HOST_TYPE_MASK
                newHostOut[0] = newHost
                return i
            }
            flags = flags and F_HOST_TYPE_MASK.inv()
        } else if (i < length && pString[i] == ':') {
            if (syntax.inFact(S_MAY_HAVE_PORT)) {
                var num2 = 0
                val num3 = i
                idx = i + 1
                while (idx < length) {
                    val num4 = pString[idx] - '0'
                    if (num4 in 0..9) {
                        num2 = num2 * 10 + num4
                        if (num2 <= 65535) {
                            idx++
                            continue
                        }
                        break
                    }
                    if (num4 == -13 || num4 == -1 || num4 == 15) {
                        break
                    }
                    if (syntax.inFact(S_ALLOW_ANY_OTHER_HOST) && syntax.notAny(S_V1_UNKNOWN_URI)) {
                        flags = flags and F_HOST_TYPE_MASK.inv()
                        break
                    }
                    err.value = E_BAD_PORT
                    newHostOut[0] = newHost
                    return idx
                }

                if (num2 > 65535) {
                    if (!syntax.inFact(S_ALLOW_ANY_OTHER_HOST)) {
                        err.value = E_BAD_PORT
                        newHostOut[0] = newHost
                        return idx
                    }
                    flags = flags and F_HOST_TYPE_MASK.inv()
                }

                if (flag) {
                    newHost += pString.substring(num3, idx)
                }
            } else {
                flags = flags and F_HOST_TYPE_MASK.inv()
            }
        }

        if ((flags and F_HOST_TYPE_MASK) == 0L) {
            flags = flags and F_HAS_USER_INFO.inv()

            if (syntax.inFact(S_ALLOW_ANY_OTHER_HOST)) {
                flags = flags or F_BASIC_HOST_TYPE
                i = idx
                while (i < length && pString[i] != '/' && pString[i] != '?' && pString[i] != '#') {
                    i++
                }
                if (flag) {
                    try {
                        newHost = normalizeAndConcat(newHost, pString.substring(num, i))
                    } catch (_: IllegalArgumentException) {
                        err.value = E_BAD_HOST_NAME
                    }
                }
            } else if (syntax.inFact(S_V1_UNKNOWN_URI)) {
                var flag2 = false
                val num5 = idx
                i = idx
                while (i < length && (!flag2 || (pString[i] != '/' && pString[i] != '?' && pString[i] != '#'))) {
                    if (i < idx + 2 && pString[i] == '.') {
                        flag2 = true
                        i++
                        continue
                    }
                    err.value = E_BAD_HOST_NAME
                    flags = flags or F_HOST_TYPE_MASK
                    newHostOut[0] = newHost
                    return idx
                }
                flags = flags or F_BASIC_HOST_TYPE
                if (flag) {
                    try {
                        newHost = normalizeAndConcat(newHost, pString.substring(num5, i))
                    } catch (_: IllegalArgumentException) {
                        err.value = E_BAD_FORMAT
                        newHostOut[0] = newHost
                        return idx
                    }
                }
            } else if (syntax.inFact(S_MUST_HAVE_AUTHORITY) || syntax.inFact(S_MAIL_TO_LIKE_URI)) {
                err.value = E_BAD_HOST_NAME
                flags = flags or F_HOST_TYPE_MASK
                newHostOut[0] = newHost
                return idx
            }
        }

        newHostOut[0] = newHost
        return i
    }

    // endregion

    // region UriInfo

    private fun ensureUriInfo(): UriInfo {
        if ((flags and F_MINIMAL_URI_INFO_SET) == 0L) {
            createUriInfo(flags)
        }
        return info!!
    }

    private fun ensureParseRemaining() {
        if ((flags and F_ALL_URI_INFO_SET) == 0L) {
            parseRemaining()
        }
    }

    private fun ensureHostString() {
        if (ensureUriInfo().hostString == null) {
            createHostString()
        }
    }

    private fun createUriInfo(cFIn: Long) {
        var cF = cFIn
        val syntax = syntax!!
        val uriInfo = UriInfo()
        uriInfo.end = string.length
        var flag = false
        var i: Int

        if ((cF and F_IMPLICIT_FILE) != 0L) {
            i = 0
            while (isLWS(string[i])) {
                i++
                uriInfo.scheme++
            }
            if ((cF and F_UNC_PATH) != 0L) {
                i += 2
                val num = (cF and F_INDEX_MASK).toInt()
                while (i < num && (string[i] == '/' || string[i] == '\\')) {
                    i++
                }
            }
        } else {
            i = syntax.schemeName.length
            while (true) {
                val ch = string[i++]
                if (ch == ':') {
                    break
                }
                uriInfo.scheme++
            }
            if ((cF and F_AUTHORITY_FOUND) != 0L) {
                if (string[i] == '\\' || string[i + 1] == '\\') {
                    flag = true
                }
                i += 2
                if ((cF and (F_DOS_PATH or F_UNC_PATH)) != 0L) {
                    val num2 = (cF and F_INDEX_MASK).toInt()
                    while (i < num2 && (string[i] == '/' || string[i] == '\\')) {
                        flag = true
                        i++
                    }
                }
            }
        }

        if (syntax.defaultPort != -1) {
            uriInfo.portValue = syntax.defaultPort
        }

        if ((cF and F_HOST_TYPE_MASK) == F_HOST_TYPE_MASK || (cF and F_DOS_PATH) != 0L) {
            uriInfo.user = (cF and F_INDEX_MASK).toInt()
            uriInfo.host = uriInfo.user
            uriInfo.path = uriInfo.user
            cF = cF and F_INDEX_MASK.inv()
            if (flag) {
                cF = cF or F_SCHEME_NOT_CANONICAL
            }
        } else {
            uriInfo.user = i
            if (hostType == F_BASIC_HOST_TYPE) {
                uriInfo.host = i
                uriInfo.path = (cF and F_INDEX_MASK).toInt()
                cF = cF and F_INDEX_MASK.inv()
            } else {
                if ((cF and F_HAS_USER_INFO) != 0L) {
                    while (string[i] != '@') {
                        i++
                    }
                    i++
                    uriInfo.host = i
                } else {
                    uriInfo.host = i
                }
                i = (cF and F_INDEX_MASK).toInt()
                cF = cF and F_INDEX_MASK.inv()
                if (flag) {
                    cF = cF or F_SCHEME_NOT_CANONICAL
                }
                uriInfo.path = i
                var flag2 = false
                if ((cF and F_HAS_UNICODE) != 0L) {
                    uriInfo.end = originalUnicodeString!!.length
                }
                if (i < uriInfo.end) {
                    val original = originalString
                    if (at(original, i) == ':') {
                        var num3 = 0
                        if (++i < uriInfo.end) {
                            num3 = at(original, i) - '0'
                            if (num3 in 0..9) {
                                flag2 = true
                                if (num3 == 0) {
                                    cF = cF or F_PORT_NOT_CANONICAL or F_E_PORT_NOT_CANONICAL
                                }
                                i++
                                while (i < uriInfo.end) {
                                    val num4 = at(original, i) - '0'
                                    if (num4 !in 0..9) {
                                        break
                                    }
                                    num3 = num3 * 10 + num4
                                    i++
                                }
                            }
                        }
                        if (flag2 && syntax.defaultPort != num3) {
                            uriInfo.portValue = num3 and 0xFFFF
                            cF = cF or F_NOT_DEFAULT_PORT
                        } else {
                            cF = cF or F_PORT_NOT_CANONICAL or F_E_PORT_NOT_CANONICAL
                        }
                        uriInfo.path = i
                    }
                }
            }
        }

        cF = cF or F_MINIMAL_URI_INFO_SET
        info = uriInfo
        flags = (flags and F_INDEX_MASK.inv()) or cF
    }

    private fun createHostString() {
        val info = info!!
        var newFlags = flags
        var text = createHostStringHelper(string, info.host, info.path, info)

        if (text.isNotEmpty()) {
            if (hostType == F_BASIC_HOST_TYPE) {
                val idx = IntRef(0)
                var check = checkCanonical(text, idx, text.length, '\uffff')
                if ((check and K_DISPLAY_CANONICAL) == 0 && (notAny(F_IMPLICIT_FILE) || (check and K_RESERVED_FOUND) != 0)) {
                    newFlags = newFlags or F_HOST_NOT_CANONICAL
                }
                if (inFact(F_IMPLICIT_FILE) && (check and (K_ESCAPED_CANONICAL or K_RESERVED_FOUND)) != 0) {
                    check = check and K_ESCAPED_CANONICAL.inv()
                }
                if ((check and (K_ESCAPED_CANONICAL or K_BACKSLASH_IN_PATH)) != K_ESCAPED_CANONICAL) {
                    newFlags = newFlags or F_E_HOST_NOT_CANONICAL
                    text = escapeString(text, !isImplicitFile, UNRESERVED_RESERVED_EXCEPT_QUESTION_MARK_HASH)
                }
            } else if (notAny(F_CANONICAL_DNS_HOST)) {
                if (info.scopeId != null) {
                    newFlags = newFlags or F_HOST_NOT_CANONICAL or F_E_HOST_NOT_CANONICAL
                } else {
                    for (i in text.indices) {
                        if (info.host + i >= info.end || text[i] != string[info.host + i]) {
                            newFlags = newFlags or F_HOST_NOT_CANONICAL or F_E_HOST_NOT_CANONICAL
                            break
                        }
                    }
                }
            }
        }

        info.hostString = text
        flags = flags or newFlags
    }

    private fun createHostStringHelper(str: String, idx: Int, end: Int, info: UriInfo): String {
        return when (flags and F_HOST_TYPE_MASK) {
            F_DNS_HOST_TYPE -> domainParseCanonicalName(str, idx, end)
            F_IPV6_HOST_TYPE -> ipv6ParseCanonicalName(str, idx, info)
            F_IPV4_HOST_TYPE -> ipv4ParseCanonicalName(str, idx, end)
            F_UNC_HOST_TYPE -> domainParseCanonicalName(str, idx, end)
            F_BASIC_HOST_TYPE -> if (!inFact(F_DOS_PATH)) str.substring(idx, end) else ""
            F_HOST_TYPE_MASK -> ""
            else -> throw exception(E_BAD_HOST_NAME)
        }
    }

    private fun parseRemaining() {
        val info = ensureUriInfo()
        val syntax = syntax!!
        var fl = 0L
        val flag = inFact(F_HAS_UNICODE)
        val scheme = IntRef(info.scheme)
        var length = string.length
        var check: Int
        val flags2 = syntax.flags

        length = lengthWithoutTrailingSpaces(string, length, scheme.value)

        if (isImplicitFile) {
            fl = fl or F_SCHEME_NOT_CANONICAL
        } else {
            val schemeName = syntax.schemeName
            var i = 0
            while (i < schemeName.length) {
                if (schemeName[i] != at(string, scheme.value + i)) {
                    fl = fl or F_SCHEME_NOT_CANONICAL
                }
                i++
            }
            if ((flags and F_AUTHORITY_FOUND) != 0L &&
                (scheme.value + i + 3 >= length || at(string, scheme.value + i + 1) != '/' || at(string, scheme.value + i + 2) != '/')
            ) {
                fl = fl or F_SCHEME_NOT_CANONICAL
            }
        }

        if ((flags and F_HAS_USER_INFO) != 0L) {
            scheme.value = info.user
            check = checkCanonical(string, scheme, info.host, '@')
            if ((check and K_DISPLAY_CANONICAL) == 0) {
                fl = fl or F_USER_NOT_CANONICAL
            }
            if ((check and (K_ESCAPED_CANONICAL or K_BACKSLASH_IN_PATH)) != K_ESCAPED_CANONICAL) {
                fl = fl or F_E_USER_NOT_CANONICAL
            }
            if ((check and (K_ESCAPED_CANONICAL or K_DISPLAY_CANONICAL or K_BACKSLASH_IN_PATH or K_NOT_IRI_CANONICAL or K_FOUND_NON_ASCII)) ==
                (K_DISPLAY_CANONICAL or K_FOUND_NON_ASCII)
            ) {
                fl = fl or F_USER_IRI_CANONICAL
            }
        }

        scheme.value = info.path
        var num = info.path

        if (flag) {
            if (isFile(syntax) && !isUncPath) {
                string = if (isImplicitFile) "" else syntax.schemeName + "://"
                info.scheme = 0
                info.user = string.length
                info.host = string.length
            }
            info.path = string.length
            scheme.value = info.path
        }

        val original = originalUnicodeString

        if (flag) {
            val start = num
            if (isImplicitFile || (flags2 and (S_MAY_HAVE_QUERY or S_MAY_HAVE_FRAGMENT)) == 0) {
                num = original!!.length
            } else {
                val span = original!!.substring(num)
                val num2 = if (syntax.inFact(S_MAY_HAVE_QUERY)) {
                    if (!syntax.inFact(S_MAY_HAVE_FRAGMENT)) span.indexOf('?') else span.indexOfAny(charArrayOf('?', '#'))
                } else {
                    span.indexOf('#')
                }
                num = if (num2 == -1) original.length else num2 + num
            }
            string += escapeUnescapeIri(original, start, num, C_PATH)
            length = string.length
            if (string == original) {
                length = lengthWithoutTrailingSpaces(string, length, scheme.value)
            }
        }

        check = if (!isImplicitFile && (flags2 and (S_MAY_HAVE_QUERY or S_MAY_HAVE_FRAGMENT)) != 0) {
            val delim = if ((flags2 and S_MAY_HAVE_QUERY) != 0) '?' else if (syntax.inFact(S_MAY_HAVE_FRAGMENT)) '#' else '\ufffe'
            checkCanonical(string, scheme, length, delim)
        } else {
            checkCanonical(string, scheme, length, '\uffff')
        }

        if ((flags and F_AUTHORITY_FOUND) != 0L && (flags2 and S_PATH_IS_ROOTED) != 0 &&
            (info.path == length || (at(string, info.path) != '/' && at(string, info.path) != '\\'))
        ) {
            fl = fl or F_FIRST_SLASH_ABSENT
        }

        var flag2 = false
        if (isDosPath || ((flags and F_AUTHORITY_FOUND) != 0L && ((flags2 and (S_CONVERT_PATH_SLASHES or S_COMPRESS_PATH)) != 0 || syntax.inFact(S_UNESCAPE_DOTS_AND_SLASHES)))) {
            if ((check and K_DOT_SLASH_ESCAPED) != 0 && syntax.inFact(S_UNESCAPE_DOTS_AND_SLASHES)) {
                fl = fl or F_PATH_NOT_CANONICAL or F_E_PATH_NOT_CANONICAL
                flag2 = true
            }
            if ((flags2 and S_CONVERT_PATH_SLASHES) != 0 && (check and K_BACKSLASH_IN_PATH) != 0) {
                fl = fl or F_PATH_NOT_CANONICAL or F_E_PATH_NOT_CANONICAL
                flag2 = true
            }
            if ((flags2 and S_COMPRESS_PATH) != 0 && ((fl and F_E_PATH_NOT_CANONICAL) != 0L || (check and K_DOT_SLASH_ATTN) != 0)) {
                fl = fl or F_SHOULD_BE_COMPRESSED
            }
            if ((check and K_BACKSLASH_IN_PATH) != 0) {
                fl = fl or F_BACKSLASH_IN_PATH
            }
        } else if ((check and K_BACKSLASH_IN_PATH) != 0) {
            fl = fl or F_E_PATH_NOT_CANONICAL
            flag2 = true
        }

        if ((check and K_DISPLAY_CANONICAL) == 0 &&
            ((flags and F_IMPLICIT_FILE) == 0L || (flags and F_USER_ESCAPED) != 0L || (check and K_RESERVED_FOUND) != 0)
        ) {
            fl = fl or F_PATH_NOT_CANONICAL
            flag2 = true
        }

        if ((flags and F_IMPLICIT_FILE) != 0L && (check and (K_ESCAPED_CANONICAL or K_RESERVED_FOUND)) != 0) {
            check = check and K_ESCAPED_CANONICAL.inv()
        }

        if ((check and K_ESCAPED_CANONICAL) == 0) {
            fl = fl or F_E_PATH_NOT_CANONICAL
        }

        if (!flag2 && (check and (K_ESCAPED_CANONICAL or K_DISPLAY_CANONICAL or K_NOT_IRI_CANONICAL or K_FOUND_NON_ASCII)) == (K_DISPLAY_CANONICAL or K_FOUND_NON_ASCII)) {
            fl = fl or F_PATH_IRI_CANONICAL
        }

        if (flag) {
            val start2 = num
            if (num < original!!.length && original[num] == '?') {
                if ((flags2 and S_MAY_HAVE_FRAGMENT) != 0) {
                    num++
                    val num3 = original.indexOf('#', num).let { if (it < 0) -1 else it - num }
                    num = if (num3 == -1) original.length else num3 + num
                } else {
                    num = original.length
                }
                string += escapeUnescapeIri(original, start2, num, C_QUERY)
                length = string.length
                if (string == original) {
                    length = lengthWithoutTrailingSpaces(string, length, scheme.value)
                }
            }
        }

        info.query = scheme.value

        if (scheme.value < length && string[scheme.value] == '?') {
            scheme.value++
            check = checkCanonical(string, scheme, length, if ((flags2 and S_MAY_HAVE_FRAGMENT) != 0) '#' else '\ufffe')
            if ((check and K_DISPLAY_CANONICAL) == 0) {
                fl = fl or F_QUERY_NOT_CANONICAL
            }
            if ((check and (K_ESCAPED_CANONICAL or K_BACKSLASH_IN_PATH)) != K_ESCAPED_CANONICAL) {
                fl = fl or F_E_QUERY_NOT_CANONICAL
            }
            if ((check and (K_ESCAPED_CANONICAL or K_DISPLAY_CANONICAL or K_BACKSLASH_IN_PATH or K_NOT_IRI_CANONICAL or K_FOUND_NON_ASCII)) ==
                (K_DISPLAY_CANONICAL or K_FOUND_NON_ASCII)
            ) {
                fl = fl or F_QUERY_IRI_CANONICAL
            }
        }

        if (flag) {
            val start3 = num
            if (num < original!!.length && original[num] == '#') {
                num = original.length
                string += escapeUnescapeIri(original, start3, num, C_FRAGMENT)
                length = string.length
                length = lengthWithoutTrailingSpaces(string, length, scheme.value)
            }
        }

        info.fragment = scheme.value

        if (scheme.value < length && string[scheme.value] == '#') {
            scheme.value++
            check = checkCanonical(string, scheme, length, '\ufffe')
            if ((check and K_DISPLAY_CANONICAL) == 0) {
                fl = fl or F_FRAGMENT_NOT_CANONICAL
            }
            if ((check and (K_ESCAPED_CANONICAL or K_BACKSLASH_IN_PATH)) != K_ESCAPED_CANONICAL) {
                fl = fl or F_E_FRAGMENT_NOT_CANONICAL
            }
            if ((check and (K_ESCAPED_CANONICAL or K_DISPLAY_CANONICAL or K_BACKSLASH_IN_PATH or K_NOT_IRI_CANONICAL or K_FOUND_NON_ASCII)) ==
                (K_DISPLAY_CANONICAL or K_FOUND_NON_ASCII)
            ) {
                fl = fl or F_FRAGMENT_IRI_CANONICAL
            }
        }

        info.end = scheme.value

        fl = fl or F_ALL_URI_INFO_SET
        flags = flags or fl
    }

    private fun checkCanonical(str: String, idx: IntRef, end: Int, delim: Char): Int {
        var check = 0
        var flag = false
        var flag2 = false
        var i = idx.value

        while (i < end) {
            var c = str[i]

            if (c <= '\u001f' || (c >= '\u007f' && c <= '\u009f')) {
                flag = true
                flag2 = true
                check = check or K_RESERVED_FOUND
                i++
                continue
            }

            if (c > '~') {
                var flag3 = false
                check = check or K_FOUND_NON_ASCII
                if (Character.isHighSurrogate(c)) {
                    if (i + 1 < end) {
                        flag3 = checkIriUnicodeRange(c, str[i + 1], isQuery = true)
                    }
                } else {
                    flag3 = checkIriUnicodeRange(c, isQuery = true)
                }
                if (!flag3) {
                    check = check or K_NOT_IRI_CANONICAL
                }
                if (!flag) {
                    flag = true
                }
                i++
                continue
            }

            if (c == delim || (delim == '?' && c == '#' && syntax != null && syntax!!.inFact(S_MAY_HAVE_FRAGMENT))) {
                break
            }

            if (c == '?') {
                if (isImplicitFile || (syntax != null && !syntax!!.inFact(S_MAY_HAVE_QUERY) && delim != '\ufffe')) {
                    check = check or K_RESERVED_FOUND
                    flag2 = true
                    flag = true
                }
                i++
                continue
            }

            if (c == '#') {
                flag = true
                if (isImplicitFile || (syntax != null && !syntax!!.inFact(S_MAY_HAVE_FRAGMENT))) {
                    check = check or K_RESERVED_FOUND
                    flag2 = true
                }
                i++
                continue
            }

            if (c == '/' || c == '\\') {
                if ((check and K_BACKSLASH_IN_PATH) == 0 && c == '\\') {
                    check = check or K_BACKSLASH_IN_PATH
                }
                if ((check and K_DOT_SLASH_ATTN) == 0 && i + 1 != end && (at(str, i + 1) == '/' || at(str, i + 1) == '\\')) {
                    check = check or K_DOT_SLASH_ATTN
                }
                i++
                continue
            }

            if (c == '.') {
                if (((check and K_DOT_SLASH_ATTN) == 0 && i + 1 == end) || at(str, i + 1) == '.' || at(str, i + 1) == '/' ||
                    at(str, i + 1) == '\\' || at(str, i + 1) == '?' || at(str, i + 1) == '#'
                ) {
                    check = check or K_DOT_SLASH_ATTN
                }
                i++
                continue
            }

            if ((c > '"' || c == '!') && (c < '[' || c > '^')) {
                when (c) {
                    '<', '>', '`' -> {}
                    '{', '|', '}' -> {
                        flag = true
                        i++
                        continue
                    }
                    else -> {
                        if (c != '%') {
                            i++
                            continue
                        }
                        if (!flag2) {
                            flag2 = true
                        }
                        if (i + 2 < end && run { c = decodeHexChars(str[i + 1], str[i + 2]); c != '\uffff' }) {
                            if (c == '.' || c == '/' || c == '\\') {
                                check = check or K_DOT_SLASH_ESCAPED
                            }
                            i += 2
                        } else if (!flag) {
                            flag = true
                        }
                        i++
                        continue
                    }
                }
            }

            if (!flag) {
                flag = true
            }
            if ((flags and F_HAS_UNICODE) != 0L) {
                check = check or K_NOT_IRI_CANONICAL
            }
            i++
        }

        if (flag2) {
            if (!flag) {
                check = check or K_ESCAPED_CANONICAL
            }
        } else {
            check = check or K_DISPLAY_CANONICAL
            if (!flag) {
                check = check or K_ESCAPED_CANONICAL
            }
        }

        idx.value = i
        return check
    }

    // endregion

    // region Components

    private fun getComponentsHelper(uriComponentsIn: Int, uriFormat: Int): String {
        var uriComponents = uriComponentsIn
        val syntax = syntax!!

        if (uriComponents == C_SCHEME) {
            return syntax.schemeName
        }

        ensureParseRemaining()

        if ((uriComponents and C_HOST) != 0) {
            // EnsureHostString(allowDnsOptimization: true); CanonicalDnsHost is never set for IRI-parsed schemes.
            ensureHostString()
        }

        if (uriComponents == C_PORT || uriComponents == C_STRONG_PORT) {
            if (inFact(F_NOT_DEFAULT_PORT) || (uriComponents == C_STRONG_PORT && syntax.defaultPort != -1)) {
                return info!!.portValue.toString()
            }
            return ""
        }

        if ((uriComponents and C_STRONG_PORT) != 0) {
            uriComponents = uriComponents or C_PORT
        }

        if (uriComponents == C_HOST && (uriFormat == FMT_URI_ESCAPED || (flags and (F_HOST_NOT_CANONICAL or F_E_HOST_NOT_CANONICAL)) == 0L)) {
            ensureHostString()
            return info!!.hostString!!
        }

        return when (uriFormat) {
            FMT_URI_ESCAPED -> getEscapedParts(uriComponents)
            FMT_UNESCAPED, FMT_SAFE_UNESCAPED, FMT_V1_TO_STRING -> getUnescapedParts(uriComponents, uriFormat)
            else -> throw IllegalArgumentException("uriFormat")
        }
    }

    private fun getEscapedParts(uriParts: Int): String {
        var num = ((flags.toInt() and 0xFFFF and 0x3F80) shr 6)
        if (inFact(F_SCHEME_NOT_CANONICAL)) {
            num = num or 1
        }
        if ((uriParts and C_PATH) != 0) {
            if (inFact(F_SHOULD_BE_COMPRESSED or F_FIRST_SLASH_ABSENT or F_BACKSLASH_IN_PATH)) {
                num = num or 0x10
            } else if (isDosPath && string[info!!.path + securedPathIndex - 1] == '|') {
                num = num or 0x10
            }
        }
        if (((uriParts and 0xFFFF) and num) == 0) {
            val fromUser = getUriPartsFromUserString(uriParts)
            if (fromUser != null) {
                return fromUser
            }
        }
        return recreateParts(uriParts, num, FMT_URI_ESCAPED)
    }

    private fun getUnescapedParts(uriParts: Int, formatAs: Int): String {
        var num = flags.toInt() and 0x7F
        if ((uriParts and C_PATH) != 0) {
            if (inFact(F_SHOULD_BE_COMPRESSED or F_FIRST_SLASH_ABSENT or F_BACKSLASH_IN_PATH)) {
                num = num or 0x10
            } else if (isDosPath && string[info!!.path + securedPathIndex - 1] == '|') {
                num = num or 0x10
            }
        }
        if (((uriParts and 0xFFFF) and num) == 0) {
            val fromUser = getUriPartsFromUserString(uriParts)
            if (fromUser != null) {
                return fromUser
            }
        }
        return recreateParts(uriParts, num, formatAs)
    }

    private fun getUriPartsFromUserString(uriParts: Int): String? {
        val info = info!!
        val s = string
        return when (uriParts and C_KEEP_DELIMITER.inv()) {
            C_SCHEME_AND_SERVER ->
                if (!inFact(F_HAS_USER_INFO)) s.substring(info.scheme, info.path)
                else s.substring(info.scheme, info.user) + s.substring(info.host, info.path)

            C_HOST_AND_PORT ->
                if (inFact(F_HAS_USER_INFO)) {
                    if (inFact(F_NOT_DEFAULT_PORT) || syntax!!.defaultPort == -1) s.substring(info.host, info.path)
                    else s.substring(info.host, info.path) + ":" + info.portValue
                } else {
                    strongAuthorityFromUserString(info, s)
                }

            C_ABSOLUTE_URI ->
                if (info.scheme == 0 && info.end == s.length) s else s.substring(info.scheme, info.end)

            C_HTTP_REQUEST_URL ->
                if (inFact(F_HAS_USER_INFO)) s.substring(info.scheme, info.user) + s.substring(info.host, info.fragment)
                else if (info.scheme == 0 && info.fragment == s.length) s
                else s.substring(info.scheme, info.fragment)

            C_SCHEME_AND_SERVER or C_USER_INFO -> s.substring(info.scheme, info.path)

            C_HTTP_REQUEST_URL or C_USER_INFO ->
                if (info.scheme == 0 && info.fragment == s.length) s else s.substring(info.scheme, info.fragment)

            C_SCHEME -> if (uriParts != C_SCHEME) s.substring(info.scheme, info.user) else syntax!!.schemeName

            C_HOST -> {
                var num2 = info.path
                if (inFact(F_PORT_NOT_CANONICAL or F_NOT_DEFAULT_PORT)) {
                    while (s[--num2] != ':') {
                        // Back to the port's ':'.
                    }
                }
                if (num2 - info.host != 0) s.substring(info.host, num2) else ""
            }

            C_PATH -> {
                val num = if (uriParts != C_PATH || !inFact(F_AUTHORITY_FOUND) || info.end <= info.path || s[info.path] != '/') info.path else info.path + 1
                if (num >= info.query) "" else s.substring(num, info.query)
            }

            C_QUERY -> {
                val num = if (uriParts != C_QUERY) info.query else info.query + 1
                if (num >= info.fragment) "" else s.substring(num, info.fragment)
            }

            C_FRAGMENT -> {
                val num = if (uriParts != C_FRAGMENT) info.fragment else info.fragment + 1
                if (num >= info.end) "" else s.substring(num, info.end)
            }

            C_USER_INFO or C_HOST or C_PORT -> userHostPortFromUserString(info, s)

            C_STRONG_AUTHORITY -> strongAuthorityFromUserString(info, s)

            C_PATH_AND_QUERY -> s.substring(info.path, info.fragment)

            C_HTTP_REQUEST_URL or C_FRAGMENT ->
                if (inFact(F_HAS_USER_INFO)) s.substring(info.scheme, info.user) + s.substring(info.host, info.end)
                else if (info.scheme == 0 && info.end == s.length) s
                else s.substring(info.scheme, info.end)

            C_PATH_AND_QUERY or C_FRAGMENT -> s.substring(info.path, info.end)

            C_USER_INFO -> {
                if (notAny(F_HAS_USER_INFO)) {
                    ""
                } else {
                    val num = if (uriParts != C_USER_INFO) info.host else info.host - 1
                    if (info.user >= num) "" else s.substring(info.user, num)
                }
            }

            else -> null
        }
    }

    private fun strongAuthorityFromUserString(info: UriInfo, s: String): String {
        if (!inFact(F_NOT_DEFAULT_PORT) && syntax!!.defaultPort != -1) {
            return s.substring(info.user, info.path) + ":" + info.portValue
        }
        return userHostPortFromUserString(info, s)
    }

    private fun userHostPortFromUserString(info: UriInfo, s: String): String =
        if (info.path - info.user != 0) s.substring(info.user, info.path) else ""

    private fun recreateParts(parts: Int, nonCanonical: Int, formatAs: Int): String {
        ensureHostString()
        val info = info!!
        val str = string
        val syntax = syntax!!
        val dest = StringBuilder()

        if ((parts and C_SCHEME) != 0) {
            dest.append(syntax.schemeName)
            if (parts != C_SCHEME) {
                dest.append(':')
                if (inFact(F_AUTHORITY_FOUND)) {
                    dest.append('/')
                    dest.append('/')
                }
            }
        }

        if ((parts and C_USER_INFO) != 0 && inFact(F_HAS_USER_INFO)) {
            val span = str.substring(info.user, info.host)
            if ((nonCanonical and 2) != 0) {
                when (formatAs) {
                    FMT_URI_ESCAPED ->
                        if (notAny(F_USER_ESCAPED)) {
                            escapeString(span, dest, checkExistingEscaped = true, noEscape = UNRESERVED_RESERVED_EXCEPT_QUESTION_MARK_HASH)
                        } else {
                            dest.append(span)
                        }

                    FMT_SAFE_UNESCAPED -> {
                        unescapeString(
                            span, 0, span.length - 1, dest, '@', '/', '\\',
                            if (inFact(F_USER_ESCAPED)) U_UNESCAPE else U_ESCAPE_UNESCAPE, syntax, isQuery = false,
                        )
                        dest.append('@')
                    }

                    FMT_UNESCAPED ->
                        unescapeString(span, 0, span.length, dest, '￿', '￿', '￿', U_UNESCAPE or U_UNESCAPE_ALL, syntax, isQuery = false)

                    else -> dest.append(span)
                }
            } else {
                dest.append(span)
            }
            if (parts == C_USER_INFO) {
                dest.setLength(dest.length - 1)
            }
        }

        if ((parts and C_HOST) != 0) {
            val text = info.hostString!!
            if (text.isNotEmpty()) {
                val unescapeMode = if (formatAs != FMT_URI_ESCAPED && hostType == F_BASIC_HOST_TYPE && (nonCanonical and 4) != 0) {
                    if (formatAs == FMT_UNESCAPED) U_UNESCAPE or U_UNESCAPE_ALL else if (inFact(F_USER_ESCAPED)) U_UNESCAPE else U_ESCAPE_UNESCAPE
                } else {
                    U_COPY_ONLY
                }
                unescapeString(text, 0, text.length, dest, '/', '?', '#', unescapeMode, syntax, isQuery = false)
            }
        }

        if ((parts and C_PORT) != 0 && (inFact(F_NOT_DEFAULT_PORT) || ((parts and C_STRONG_PORT) != 0 && syntax.defaultPort != -1))) {
            dest.append(':')
            dest.append(info.portValue)
        }

        if ((parts and C_PATH) != 0) {
            getCanonicalPath(dest, formatAs)
            if (parts == C_PATH) {
                val start = if (inFact(F_AUTHORITY_FOUND) && dest.isNotEmpty() && dest[0] == '/') 1 else 0
                return dest.substring(start)
            }
        }

        if ((parts and C_QUERY) != 0 && info.query < info.fragment) {
            val num = info.query + 1
            if (parts != C_QUERY) {
                dest.append('?')
            }
            var unescapeMode = U_COPY_ONLY
            var escaped = false
            if ((nonCanonical and 0x20) != 0) {
                if (formatAs == FMT_URI_ESCAPED) {
                    if (notAny(F_USER_ESCAPED)) {
                        escapeString(str.substring(num, info.fragment), dest, checkExistingEscaped = true, noEscape = UNRESERVED_RESERVED_EXCEPT_HASH)
                        escaped = true
                    }
                } else {
                    unescapeMode = unescapeModeFor(formatAs)
                }
            }
            if (!escaped) {
                unescapeString(str, num, info.fragment, dest, '#', '￿', '￿', unescapeMode, syntax, isQuery = true)
            }
        }

        if ((parts and C_FRAGMENT) != 0 && info.fragment < info.end) {
            val num2 = info.fragment + 1
            if (parts != C_FRAGMENT) {
                dest.append('#')
            }
            var unescapeMode = U_COPY_ONLY
            var escaped = false
            if ((nonCanonical and 0x40) != 0) {
                if (formatAs == FMT_URI_ESCAPED) {
                    if (notAny(F_USER_ESCAPED)) {
                        escapeString(str.substring(num2, info.end), dest, checkExistingEscaped = true, noEscape = UNRESERVED_RESERVED)
                        escaped = true
                    }
                } else {
                    unescapeMode = unescapeModeFor(formatAs)
                }
            }
            if (!escaped) {
                unescapeString(str, num2, info.end, dest, '#', '￿', '￿', unescapeMode, syntax, isQuery = false)
            }
        }

        return dest.toString()
    }

    /** The UnescapeMode RecreateParts uses for a non-canonical query or fragment in a non-escaped [formatAs]. */
    private fun unescapeModeFor(formatAs: Int): Int = when (formatAs) {
        FMT_V1_TO_STRING -> (if (inFact(F_USER_ESCAPED)) U_UNESCAPE else U_ESCAPE_UNESCAPE) or U_V1_TO_STRING
        FMT_UNESCAPED -> U_UNESCAPE or U_UNESCAPE_ALL
        else -> if (inFact(F_USER_ESCAPED)) U_UNESCAPE else U_ESCAPE_UNESCAPE
    }

    private fun getCanonicalPath(dest: StringBuilder, formatAs: Int) {
        val info = info!!
        val syntax = syntax!!

        if (inFact(F_FIRST_SLASH_ABSENT)) {
            dest.append('/')
        }

        if (info.path == info.query) {
            return
        }

        var length = dest.length
        val securedPathIndex = securedPathIndex

        if (formatAs == FMT_URI_ESCAPED) {
            if (inFact(F_SHOULD_BE_COMPRESSED)) {
                dest.append(string, info.path, info.query)
                if (syntax.inFact(S_UNESCAPE_DOTS_AND_SLASHES) && inFact(F_PATH_NOT_CANONICAL) && !isImplicitFile) {
                    unescapeOnly(dest, length, '.', '/', if (syntax.inFact(S_CONVERT_PATH_SLASHES)) '\\' else '￿')
                }
            } else if (inFact(F_E_PATH_NOT_CANONICAL) && notAny(F_USER_ESCAPED)) {
                var span = string
                if (securedPathIndex != 0 && span[securedPathIndex + info.path - 1] == '|') {
                    val array = span.toCharArray()
                    array[securedPathIndex + info.path - 1] = ':'
                    span = String(array)
                }
                escapeString(span.substring(info.path, info.query), dest, !isImplicitFile, UNRESERVED_RESERVED_EXCEPT_QUESTION_MARK_HASH)
            } else {
                dest.append(string, info.path, info.query)
            }

            if (inFact(F_BACKSLASH_IN_PATH) && syntax.notAny(S_CONVERT_PATH_SLASHES) && syntax.inFact(S_FILE_LIKE_URI) && !isImplicitFile) {
                val region = dest.substring(length)
                dest.setLength(length)
                escapeString(region, dest, checkExistingEscaped = true, noEscape = UNRESERVED_RESERVED)
                length = dest.length
            }
        } else {
            dest.append(string, info.path, info.query)
            if (inFact(F_SHOULD_BE_COMPRESSED) && syntax.inFact(S_UNESCAPE_DOTS_AND_SLASHES) && inFact(F_PATH_NOT_CANONICAL) && !isImplicitFile) {
                unescapeOnly(dest, length, '.', '/', if (syntax.inFact(S_CONVERT_PATH_SLASHES)) '\\' else '￿')
            }
        }

        val num = length + securedPathIndex
        if (securedPathIndex != 0 && dest[num - 1] == '|') {
            dest.setCharAt(num - 1, ':')
        }

        if (inFact(F_SHOULD_BE_COMPRESSED) && dest.length - num > 0) {
            val region = CharArray(dest.length - num)
            dest.getChars(num, dest.length, region, 0)
            val compressed = compress(region, syntax.inFact(S_CONVERT_PATH_SLASHES), syntax.inFact(S_CANONICALIZE_AS_FILE_PATH))
            dest.setLength(num)
            dest.appendRange(region, 0, compressed)
            // .NET reads its buffer past the logical end here when the path compressed away; that is invisible.
            if (length < dest.length && dest[length] == '\\') {
                dest.setCharAt(length, '/')
            }
            if (formatAs == FMT_URI_ESCAPED && notAny(F_USER_ESCAPED) && inFact(F_E_PATH_NOT_CANONICAL)) {
                val tail = dest.substring(length)
                dest.setLength(length)
                escapeString(tail, dest, !isImplicitFile, UNRESERVED_RESERVED_EXCEPT_QUESTION_MARK_HASH)
                length = dest.length
            }
        }

        if (formatAs == FMT_URI_ESCAPED || !inFact(F_PATH_NOT_CANONICAL)) {
            return
        }

        var unescapeMode = when (formatAs) {
            FMT_V1_TO_STRING -> (if (inFact(F_USER_ESCAPED)) U_UNESCAPE else U_ESCAPE_UNESCAPE) or U_V1_TO_STRING
            FMT_UNESCAPED -> if (!isImplicitFile) U_UNESCAPE or U_UNESCAPE_ALL else U_COPY_ONLY
            else -> if (inFact(F_USER_ESCAPED)) U_UNESCAPE else U_ESCAPE_UNESCAPE
        }
        if (formatAs != FMT_UNESCAPED && isImplicitFile) {
            unescapeMode = unescapeMode and U_UNESCAPE.inv()
        }

        if (unescapeMode != U_COPY_ONLY) {
            val tail = dest.substring(length)
            dest.setLength(length)
            unescapeString(tail, 0, tail.length, dest, '?', '#', '￿', unescapeMode, syntax, isQuery = false)
        }
    }

    // endregion

    // region Static helpers ported from System.Uri

    companion object {
        /** `Uri.TryCreate(uriString, UriKind.Absolute, out uri)`. */
        fun tryCreate(uriString: String?): DotNetUri? {
            if (uriString == null) {
                return null
            }
            val uri = DotNetUri(uriString)
            if (uri.parseScheme() != E_NONE) {
                return null
            }
            // Uri.CreateHelper: TryCreate answers false for a string .NET refuses (UriFormatException), as .NET does.
            return try {
                if (uri.initializeUri(E_NONE, allowRelative = false) == null) uri else null
            } catch (_: UriFormatException) {
                null
            }
        }

        /** `new Uri(uriString)`. Throws [UriFormatException] where .NET did. */
        fun create(uriString: String): DotNetUri {
            val uri = DotNetUri(uriString)
            uri.initializeUri(uri.parseScheme(), allowRelative = false)?.let { throw it }
            return uri
        }

        /**
         * `Uri.TryCreate(uriString, UriKind.RelativeOrAbsolute, out uri)`: null where .NET answers false. Unlike
         * [createRelativeOrAbsolute], a string without a valid scheme is kept exactly as given (Uri.CreateHelper makes
         * that relative Uri without initializing it).
         */
        fun tryCreateRelativeOrAbsolute(uriString: String?): DotNetUri? {
            if (uriString == null) {
                return null
            }
            val uri = DotNetUri(uriString)
            val err = uri.parseScheme()
            if (err != E_NONE) {
                if (err > E_LAST_ERROR_OKAY_FOR_RELATIVE_URIS) {
                    return null
                }
                return DotNetUri(uriString)
            }
            // Uri.CreateHelper: TryCreate answers false for a string .NET refuses (UriFormatException), as .NET does.
            return try {
                if (uri.initializeUri(E_NONE, allowRelative = true) == null) uri else null
            } catch (_: UriFormatException) {
                null
            }
        }

        /** `new Uri(uriString, UriKind.RelativeOrAbsolute)`: see [isAbsoluteUri]. Throws [UriFormatException] where .NET did. */
        fun createRelativeOrAbsolute(uriString: String): DotNetUri {
            val uri = DotNetUri(uriString)
            uri.initializeUri(uri.parseScheme(), allowRelative = true)?.let { throw it }
            return uri
        }

        /**
         * `new Uri(baseUri, relativeUri)`: [relativeUri] resolved against [baseUri] (an absolute URI) the way .NET
         * does it (Uri.CreateUri, ResolveHelper, GetCombinedString, CombineUri), which is not RFC 3986's algorithm.
         * Throws [UriFormatException] where .NET did.
         */
        fun create(baseUri: DotNetUri, relativeUri: String): DotNetUri {
            require(baseUri.isAbsoluteUri) { "baseUri must be absolute" }

            val relative = createRelativeOrAbsolute(relativeUri)
            if (relative.isAbsoluteUri) {
                return relative
            }

            return create(resolveHelper(baseUri, relative))
        }

        /** Uri.ResolveHelper for a relative [relativeUri]: the string to parse as the combined absolute URI. */
        private fun resolveHelper(baseUri: DotNetUri, relativeUri: DotNetUri): String {
            var text = relativeUri.originalString
            val baseSyntax = baseUri.syntax!!

            if (text.isNotEmpty() && (isLWS(text[0]) || isLWS(text[text.length - 1]))) {
                text = text.trim(' ', '\n', '\r', '\t')
            }

            if (text.isEmpty()) {
                return baseUri.getParts(C_ABSOLUTE_URI, FMT_SAFE_UNESCAPED)
            }

            if (text[0] == '#' && !baseUri.isImplicitFile && baseSyntax.inFact(S_MAY_HAVE_FRAGMENT)) {
                return baseUri.getParts(C_HTTP_REQUEST_URL or C_USER_INFO, FMT_URI_ESCAPED) + text
            }

            if (text[0] == '?' && !baseUri.isImplicitFile && baseSyntax.inFact(S_MAY_HAVE_QUERY)) {
                return baseUri.getParts(C_SCHEME_AND_SERVER or C_USER_INFO or C_PATH, FMT_URI_ESCAPED) + text
            }

            if (text.length >= 3 && (text[1] == ':' || text[1] == '|') && isAsciiLetter(text[0]) && (text[2] == '\\' || text[2] == '/')) {
                if (baseUri.isImplicitFile) {
                    return text
                }
                if (baseSyntax.inFact(S_ALLOW_DOS_PATH)) {
                    val delimiter = if (!baseUri.inFact(F_AUTHORITY_FOUND)) {
                        if (baseSyntax.inFact(S_PATH_IS_ROOTED)) ":/" else ":"
                    } else {
                        if (baseSyntax.inFact(S_PATH_IS_ROOTED)) ":///" else "://"
                    }
                    return baseUri.scheme + delimiter + text
                }
            }

            // ResolveHelper hands back baseUri itself when the combined string is its own; parsing that string again is the same URI.
            return getCombinedString(baseUri, text)
        }

        private fun getCombinedString(baseUri: DotNetUri, relativeStrIn: String): String {
            var relativeStr = relativeStrIn
            var i = 0

            while (i < relativeStr.length && relativeStr[i] != '/' && relativeStr[i] != '\\' && relativeStr[i] != '?' && relativeStr[i] != '#') {
                if (relativeStr[i] == ':') {
                    if (i < 2) {
                        break
                    }
                    val error = IntRef(E_NONE)
                    val uriParser = checkSchemeSyntax(relativeStr.substring(0, i), error)
                    if (error.value != E_NONE) {
                        break
                    }
                    if (sameParser(baseUri.syntax!!, uriParser!!)) {
                        relativeStr = if (i + 1 >= relativeStr.length) "" else relativeStr.substring(i + 1)
                        break
                    }
                    return relativeStr
                }
                i++
            }

            if (relativeStr.isEmpty()) {
                return baseUri.originalString
            }

            return combineUri(baseUri, relativeStr, FMT_SAFE_UNESCAPED)
        }

        /** Reference equality of .NET's UriParser objects: one per built-in syntax, and one per unknown scheme name. */
        private fun sameParser(a: Syntax, b: Syntax): Boolean =
            a === b || (a.inFact(S_V1_UNKNOWN_URI) && b.inFact(S_V1_UNKNOWN_URI) && a.schemeName == b.schemeName)

        private fun combineUri(basePart: DotNetUri, relativePartIn: String, uriFormat: Int): String {
            var relativePart = relativePartIn
            val baseSyntax = basePart.syntax!!
            var c = relativePart[0]

            if (basePart.isDosPath && (c == '/' || c == '\\') && (relativePart.length == 1 || (relativePart[1] != '/' && relativePart[1] != '\\'))) {
                var num = basePart.originalString.indexOf(':')
                if (basePart.isImplicitFile) {
                    return basePart.originalString.substring(0, num + 1) + relativePart
                }
                num = basePart.originalString.indexOf(':', num + 1)
                return basePart.originalString.substring(0, num + 1) + relativePart
            }

            if (staticIsFile(baseSyntax) && (c == '\\' || c == '/')) {
                if (relativePart.length >= 2 && (relativePart[1] == '\\' || relativePart[1] == '/')) {
                    return if (!basePart.isImplicitFile) "file:$relativePart" else relativePart
                }
                if (basePart.isUncPath) {
                    var span = basePart.getParts(C_PATH or C_KEEP_DELIMITER, FMT_UNESCAPED)
                    val num2 = span.substring(1).indexOf('/')
                    if (num2 >= 0) {
                        span = span.substring(0, num2 + 1)
                    }
                    if (basePart.isImplicitFile) {
                        return "\\\\" + basePart.getParts(C_HOST, FMT_UNESCAPED) + span + relativePart
                    }
                    return "file://" + basePart.getParts(C_HOST, uriFormat) + span + relativePart
                }
                return "file://$relativePart"
            }

            val flag = baseSyntax.inFact(S_CONVERT_PATH_SLASHES)
            var text: String

            if (c == '/' || (c == '\\' && flag)) {
                if (relativePart.length >= 2 && relativePart[1] == '/') {
                    return basePart.scheme + ":" + relativePart
                }
                text = if (basePart.hostType != F_IPV6_HOST_TYPE) {
                    basePart.getParts(C_SCHEME_AND_SERVER or C_USER_INFO, uriFormat)
                } else {
                    basePart.getParts(C_SCHEME or C_USER_INFO, uriFormat) + "[" + basePart.dnsSafeHost + "]" +
                        basePart.getParts(C_PORT or C_KEEP_DELIMITER, uriFormat)
                }
                if (!flag || c != '\\') {
                    return text + relativePart
                }
                return text + "/" + relativePart.substring(1)
            }

            text = basePart.getParts(C_PATH or C_KEEP_DELIMITER, if (basePart.isImplicitFile) FMT_UNESCAPED else uriFormat)
            var num3 = text.length
            val array = CharArray(num3 + relativePart.length)

            if (num3 > 0) {
                text.toCharArray(array, 0, 0, num3)
                while (num3 > 0) {
                    if (array[--num3] == '/') {
                        num3++
                        break
                    }
                }
            }

            relativePart.toCharArray(array, num3, 0, relativePart.length)

            c = if (baseSyntax.inFact(S_MAY_HAVE_QUERY)) '?' else '￿'
            val c2 = if (!basePart.isImplicitFile && baseSyntax.inFact(S_MAY_HAVE_FRAGMENT)) '#' else '￿'
            var extra = ""

            if (c != '￿' || c2 != '￿') {
                var i = 0
                while (i < relativePart.length && array[num3 + i] != c && array[num3 + i] != c2) {
                    i++
                }
                if (i == 0) {
                    extra = relativePart
                } else if (i < relativePart.length) {
                    extra = relativePart.substring(i)
                }
                num3 += i
            } else {
                num3 += relativePart.length
            }

            if (basePart.hostType == F_IPV6_HOST_TYPE) {
                text = if (!basePart.isImplicitFile) {
                    basePart.getParts(C_SCHEME or C_USER_INFO, uriFormat) + "[" + basePart.dnsSafeHost + "]" +
                        basePart.getParts(C_PORT or C_KEEP_DELIMITER, uriFormat)
                } else {
                    "\\\\[" + basePart.dnsSafeHost + "]"
                }
            } else if (basePart.isImplicitFile) {
                if (basePart.isDosPath) {
                    num3 = compressRange(array, 3, num3, baseSyntax)
                    return String(array, 1, num3 - 1) + extra
                }
                // Not Windows: a Unix path's host part is used as is.
                text = if (!basePart.isUnixPath) "\\\\" + basePart.getParts(C_HOST, FMT_UNESCAPED) else basePart.getParts(C_HOST, FMT_UNESCAPED)
            } else {
                text = basePart.getParts(C_SCHEME_AND_SERVER or C_USER_INFO, uriFormat)
            }

            num3 = compressRange(array, basePart.securedPathIndex, num3, baseSyntax)
            return text + String(array, 0, num3) + extra
        }

        /** Uri.Compress(char[], start, ref destLength, syntax): the new length. */
        private fun compressRange(dest: CharArray, start: Int, destLength: Int, syntax: Syntax): Int {
            val region = dest.copyOfRange(start, destLength)
            val length = compress(region, syntax.inFact(S_CONVERT_PATH_SLASHES), syntax.inFact(S_CANONICALIZE_AS_FILE_PATH))
            System.arraycopy(region, 0, dest, start, length)
            return start + length
        }

        private fun staticIsFile(syntax: Syntax) = syntax.inFact(S_FILE_LIKE_URI)

        // Flags
        private const val F_SCHEME_NOT_CANONICAL = 0x1L
        private const val F_USER_NOT_CANONICAL = 0x2L
        private const val F_HOST_NOT_CANONICAL = 0x4L
        private const val F_PORT_NOT_CANONICAL = 0x8L
        private const val F_PATH_NOT_CANONICAL = 0x10L
        private const val F_QUERY_NOT_CANONICAL = 0x20L
        private const val F_FRAGMENT_NOT_CANONICAL = 0x40L
        private const val F_E_USER_NOT_CANONICAL = 0x80L
        private const val F_E_HOST_NOT_CANONICAL = 0x100L
        private const val F_E_PORT_NOT_CANONICAL = 0x200L
        private const val F_E_PATH_NOT_CANONICAL = 0x400L
        private const val F_E_QUERY_NOT_CANONICAL = 0x800L
        private const val F_E_FRAGMENT_NOT_CANONICAL = 0x1000L
        private const val F_SHOULD_BE_COMPRESSED = 0x2000L
        private const val F_FIRST_SLASH_ABSENT = 0x4000L
        private const val F_BACKSLASH_IN_PATH = 0x8000L
        private const val F_INDEX_MASK = 0xFFFFFFFFL
        private const val F_HOST_TYPE_MASK = 0x700000000L
        private const val F_IPV6_HOST_TYPE = 0x100000000L
        private const val F_IPV4_HOST_TYPE = 0x200000000L
        private const val F_DNS_HOST_TYPE = 0x300000000L
        private const val F_UNC_HOST_TYPE = 0x400000000L
        private const val F_BASIC_HOST_TYPE = 0x500000000L
        private const val F_USER_ESCAPED = 0x800000000L
        private const val F_AUTHORITY_FOUND = 0x1000000000L
        private const val F_HAS_USER_INFO = 0x2000000000L
        private const val F_NOT_DEFAULT_PORT = 0x8000000000L
        private const val F_USER_DRIVEN_PARSING = 0x10000000000L
        private const val F_CANONICAL_DNS_HOST = 0x20000000000L
        private const val F_DOS_PATH = 0x80000000000L
        private const val F_UNC_PATH = 0x100000000000L
        private const val F_IMPLICIT_FILE = 0x200000000000L
        private const val F_MINIMAL_URI_INFO_SET = 0x400000000000L
        private const val F_ALL_URI_INFO_SET = 0x800000000000L
        private const val F_HAS_UNICODE = 0x2000000000000L
        private const val F_USER_IRI_CANONICAL = 0x4000000000000L
        private const val F_PATH_IRI_CANONICAL = 0x8000000000000L
        private const val F_QUERY_IRI_CANONICAL = 0x10000000000000L
        private const val F_FRAGMENT_IRI_CANONICAL = 0x20000000000000L
        private const val F_UNIX_PATH = 0x40000000000000L

        // Check
        private const val K_ESCAPED_CANONICAL = 1
        private const val K_DISPLAY_CANONICAL = 2
        private const val K_DOT_SLASH_ATTN = 4
        private const val K_FOUND_NON_ASCII = 8
        private const val K_BACKSLASH_IN_PATH = 0x10
        private const val K_RESERVED_FOUND = 0x20
        private const val K_NOT_IRI_CANONICAL = 0x40
        private const val K_DOT_SLASH_ESCAPED = 0x80

        // UriComponents
        private const val C_SCHEME = 1
        private const val C_USER_INFO = 2
        private const val C_HOST = 4
        private const val C_PORT = 8
        private const val C_PATH = 0x10
        private const val C_QUERY = 0x20
        private const val C_FRAGMENT = 0x40
        private const val C_STRONG_PORT = 0x80
        private const val C_SCHEME_AND_SERVER = 0xD
        private const val C_HOST_AND_PORT = C_HOST or C_STRONG_PORT
        private const val C_STRONG_AUTHORITY = C_HOST_AND_PORT or C_USER_INFO
        private const val C_HTTP_REQUEST_URL = 0x3D
        private const val C_PATH_AND_QUERY = 0x30
        private const val C_ABSOLUTE_URI = 0x7F
        private const val C_KEEP_DELIMITER = 0x40000000

        // UriFormat (V1ToString is the internal (UriFormat)0x7FFF Uri.ToString uses)
        private const val FMT_URI_ESCAPED = 1
        private const val FMT_UNESCAPED = 2
        private const val FMT_SAFE_UNESCAPED = 3
        private const val FMT_V1_TO_STRING = 0x7FFF

        // UnescapeMode
        private const val U_COPY_ONLY = 0
        private const val U_ESCAPE = 1
        private const val U_UNESCAPE = 2
        private const val U_ESCAPE_UNESCAPE = U_ESCAPE or U_UNESCAPE
        private const val U_V1_TO_STRING = 4
        private const val U_UNESCAPE_ALL = 8

        // UriSyntaxFlags
        private const val S_MUST_HAVE_AUTHORITY = 1
        private const val S_OPTIONAL_AUTHORITY = 2
        private const val S_MAY_HAVE_USER_INFO = 4
        private const val S_MAY_HAVE_PORT = 8
        private const val S_MAY_HAVE_PATH = 0x10
        private const val S_MAY_HAVE_QUERY = 0x20
        private const val S_MAY_HAVE_FRAGMENT = 0x40
        private const val S_ALLOW_EMPTY_HOST = 0x80
        private const val S_ALLOW_UNC_HOST = 0x100
        private const val S_ALLOW_DNS_HOST = 0x200
        private const val S_ALLOW_IPV4_HOST = 0x400
        private const val S_ALLOW_IPV6_HOST = 0x800
        private const val S_ALLOW_AN_INTERNET_HOST = S_ALLOW_DNS_HOST or S_ALLOW_IPV4_HOST or S_ALLOW_IPV6_HOST
        private const val S_ALLOW_ANY_OTHER_HOST = 0x1000
        private const val S_FILE_LIKE_URI = 0x2000
        private const val S_MAIL_TO_LIKE_URI = 0x4000
        private const val S_V1_UNKNOWN_URI = 0x10000
        private const val S_SIMPLE_USER_SYNTAX = 0x20000
        private const val S_ALLOW_DOS_PATH = 0x100000
        private const val S_PATH_IS_ROOTED = 0x200000
        private const val S_CONVERT_PATH_SLASHES = 0x400000
        private const val S_COMPRESS_PATH = 0x800000
        private const val S_CANONICALIZE_AS_FILE_PATH = 0x1000000
        private const val S_UNESCAPE_DOTS_AND_SLASHES = 0x2000000
        private const val S_ALLOW_IDN = 0x4000000
        private const val S_ALLOW_IRI_PARSING = 0x10000000

        // ParsingError
        private const val E_NONE = 0
        private const val E_BAD_FORMAT = 1
        private const val E_BAD_SCHEME = 2
        private const val E_BAD_AUTHORITY = 3
        private const val E_EMPTY_URI_STRING = 4
        private const val E_LAST_ERROR_OKAY_FOR_RELATIVE_URIS = 5
        private const val E_SCHEME_LIMIT = 6
        private const val E_MUST_ROOTED_PATH = 7
        private const val E_BAD_HOST_NAME = 8
        private const val E_BAD_PORT = 10
        private const val E_BAD_AUTHORITY_TERMINATOR = 11

        private val ERROR_MESSAGES = mapOf(
            E_BAD_FORMAT to "Invalid URI: The format of the URI could not be determined.",
            E_BAD_SCHEME to "Invalid URI: The URI scheme is not valid.",
            E_BAD_AUTHORITY to "Invalid URI: The Authority/Host could not be parsed.",
            E_EMPTY_URI_STRING to "Invalid URI: The URI is empty.",
            E_SCHEME_LIMIT to "Invalid URI: The Uri scheme is too long.",
            E_MUST_ROOTED_PATH to "Invalid URI: A Dos path must be rooted, for example, 'c:\\\\'.",
            E_BAD_HOST_NAME to "Invalid URI: The hostname could not be parsed.",
            E_BAD_PORT to "Invalid URI: Invalid port specified.",
            E_BAD_AUTHORITY_TERMINATOR to "Invalid URI: The Authority/Host cannot end with a backslash character ('\\\\').",
        )

        private fun exception(err: Int) = UriFormatException(ERROR_MESSAGES.getValue(err))

        private const val HTTP_FLAGS = S_ALLOW_AN_INTERNET_HOST or S_MUST_HAVE_AUTHORITY or S_MAY_HAVE_USER_INFO or S_MAY_HAVE_PORT or
            S_MAY_HAVE_PATH or S_MAY_HAVE_QUERY or S_MAY_HAVE_FRAGMENT or S_ALLOW_UNC_HOST or S_PATH_IS_ROOTED or
            S_CONVERT_PATH_SLASHES or S_COMPRESS_PATH or S_CANONICALIZE_AS_FILE_PATH or S_ALLOW_IDN or S_ALLOW_IRI_PARSING

        private const val NEWS_FLAGS = S_MAY_HAVE_PATH or S_MAY_HAVE_FRAGMENT or S_ALLOW_IRI_PARSING

        private val HTTP = Syntax("http", 80, HTTP_FLAGS)
        private val HTTPS = Syntax("https", 443, HTTP_FLAGS)
        private val WS = Syntax("ws", 80, HTTP_FLAGS)
        private val WSS = Syntax("wss", 443, HTTP_FLAGS)
        private val FTP = Syntax(
            "ftp", 21,
            S_ALLOW_AN_INTERNET_HOST or S_MUST_HAVE_AUTHORITY or S_MAY_HAVE_USER_INFO or S_MAY_HAVE_PORT or S_MAY_HAVE_PATH or
                S_MAY_HAVE_FRAGMENT or S_ALLOW_UNC_HOST or S_PATH_IS_ROOTED or S_CONVERT_PATH_SLASHES or S_COMPRESS_PATH or
                S_CANONICALIZE_AS_FILE_PATH or S_ALLOW_IDN or S_ALLOW_IRI_PARSING,
        )
        private val FILE = Syntax(
            "file", -1,
            S_ALLOW_AN_INTERNET_HOST or S_MUST_HAVE_AUTHORITY or S_MAY_HAVE_PATH or S_MAY_HAVE_QUERY or S_MAY_HAVE_FRAGMENT or
                S_ALLOW_EMPTY_HOST or S_ALLOW_UNC_HOST or S_FILE_LIKE_URI or S_ALLOW_DOS_PATH or S_PATH_IS_ROOTED or
                S_CONVERT_PATH_SLASHES or S_COMPRESS_PATH or S_CANONICALIZE_AS_FILE_PATH or S_UNESCAPE_DOTS_AND_SLASHES or
                S_ALLOW_IDN or S_ALLOW_IRI_PARSING,
        )
        private val UNIX_FILE = Syntax(
            "file", -1,
            S_ALLOW_AN_INTERNET_HOST or S_MUST_HAVE_AUTHORITY or S_MAY_HAVE_PATH or S_MAY_HAVE_QUERY or S_MAY_HAVE_FRAGMENT or
                S_ALLOW_EMPTY_HOST or S_ALLOW_UNC_HOST or S_FILE_LIKE_URI or S_ALLOW_DOS_PATH or S_PATH_IS_ROOTED or
                S_COMPRESS_PATH or S_CANONICALIZE_AS_FILE_PATH or S_UNESCAPE_DOTS_AND_SLASHES or S_ALLOW_IDN or S_ALLOW_IRI_PARSING,
        )
        private val GOPHER = Syntax(
            "gopher", 70,
            S_ALLOW_AN_INTERNET_HOST or S_MUST_HAVE_AUTHORITY or S_MAY_HAVE_USER_INFO or S_MAY_HAVE_PORT or S_MAY_HAVE_PATH or
                S_MAY_HAVE_FRAGMENT or S_ALLOW_UNC_HOST or S_PATH_IS_ROOTED or S_ALLOW_IDN or S_ALLOW_IRI_PARSING,
        )
        private val NNTP = Syntax(
            "nntp", 119,
            S_ALLOW_AN_INTERNET_HOST or S_MUST_HAVE_AUTHORITY or S_MAY_HAVE_USER_INFO or S_MAY_HAVE_PORT or S_MAY_HAVE_PATH or
                S_MAY_HAVE_FRAGMENT or S_ALLOW_UNC_HOST or S_PATH_IS_ROOTED or S_ALLOW_IDN or S_ALLOW_IRI_PARSING,
        )
        private val NEWS = Syntax("news", -1, NEWS_FLAGS)
        private val MAILTO = Syntax(
            "mailto", 25,
            S_ALLOW_AN_INTERNET_HOST or S_MAY_HAVE_USER_INFO or S_MAY_HAVE_PORT or S_MAY_HAVE_PATH or S_MAY_HAVE_QUERY or
                S_MAY_HAVE_FRAGMENT or S_ALLOW_EMPTY_HOST or S_ALLOW_UNC_HOST or S_MAIL_TO_LIKE_URI or S_ALLOW_IDN or S_ALLOW_IRI_PARSING,
        )
        private val UUID = Syntax("uuid", -1, NEWS_FLAGS)
        private val TELNET = Syntax(
            "telnet", 23,
            S_ALLOW_AN_INTERNET_HOST or S_MUST_HAVE_AUTHORITY or S_MAY_HAVE_USER_INFO or S_MAY_HAVE_PORT or S_MAY_HAVE_PATH or
                S_MAY_HAVE_FRAGMENT or S_ALLOW_UNC_HOST or S_PATH_IS_ROOTED or S_ALLOW_IDN or S_ALLOW_IRI_PARSING,
        )
        private val LDAP = Syntax(
            "ldap", 389,
            S_ALLOW_AN_INTERNET_HOST or S_MUST_HAVE_AUTHORITY or S_MAY_HAVE_USER_INFO or S_MAY_HAVE_PORT or S_MAY_HAVE_PATH or
                S_MAY_HAVE_QUERY or S_MAY_HAVE_FRAGMENT or S_ALLOW_EMPTY_HOST or S_ALLOW_UNC_HOST or S_PATH_IS_ROOTED or
                S_ALLOW_IDN or S_ALLOW_IRI_PARSING,
        )
        private val NET_TCP = Syntax(
            "net.tcp", 808,
            S_ALLOW_AN_INTERNET_HOST or S_MUST_HAVE_AUTHORITY or S_MAY_HAVE_PORT or S_MAY_HAVE_PATH or S_MAY_HAVE_QUERY or
                S_MAY_HAVE_FRAGMENT or S_PATH_IS_ROOTED or S_CONVERT_PATH_SLASHES or S_COMPRESS_PATH or
                S_CANONICALIZE_AS_FILE_PATH or S_UNESCAPE_DOTS_AND_SLASHES or S_ALLOW_IDN or S_ALLOW_IRI_PARSING,
        )
        private val NET_PIPE = Syntax(
            "net.pipe", -1,
            S_ALLOW_AN_INTERNET_HOST or S_MUST_HAVE_AUTHORITY or S_MAY_HAVE_PATH or S_MAY_HAVE_QUERY or S_MAY_HAVE_FRAGMENT or
                S_PATH_IS_ROOTED or S_CONVERT_PATH_SLASHES or S_COMPRESS_PATH or S_CANONICALIZE_AS_FILE_PATH or
                S_UNESCAPE_DOTS_AND_SLASHES or S_ALLOW_IDN or S_ALLOW_IRI_PARSING,
        )
        private val VS_MACROS = Syntax(
            "vsmacros", -1,
            S_ALLOW_AN_INTERNET_HOST or S_MUST_HAVE_AUTHORITY or S_MAY_HAVE_PATH or S_MAY_HAVE_FRAGMENT or S_ALLOW_EMPTY_HOST or
                S_ALLOW_UNC_HOST or S_FILE_LIKE_URI or S_ALLOW_DOS_PATH or S_CONVERT_PATH_SLASHES or S_COMPRESS_PATH or
                S_CANONICALIZE_AS_FILE_PATH or S_UNESCAPE_DOTS_AND_SLASHES or S_ALLOW_IDN or S_ALLOW_IRI_PARSING,
        )

        private val BUILT_IN = listOf(HTTP, HTTPS, WS, WSS, FTP, FILE, GOPHER, NNTP, NEWS, MAILTO, UUID, TELNET, LDAP, NET_TCP, NET_PIPE, VS_MACROS)
            .associateBy { it.schemeName }

        private const val V1_UNKNOWN_FLAGS = S_ALLOW_AN_INTERNET_HOST or S_OPTIONAL_AUTHORITY or S_MAY_HAVE_USER_INFO or S_MAY_HAVE_PORT or
            S_MAY_HAVE_PATH or S_MAY_HAVE_QUERY or S_MAY_HAVE_FRAGMENT or S_ALLOW_EMPTY_HOST or S_ALLOW_UNC_HOST or
            S_V1_UNKNOWN_URI or S_ALLOW_DOS_PATH or S_PATH_IS_ROOTED or S_CONVERT_PATH_SLASHES or S_COMPRESS_PATH or
            S_ALLOW_IDN or S_ALLOW_IRI_PARSING

        private const val SCHEME_CHARS = "+-.0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"

        private fun checkSchemeSyntax(scheme: String, error: IntRef): Syntax? {
            BUILT_IN[DotNetText.asciiFold(scheme)]?.let { return it }

            if (scheme.isEmpty() || !isAsciiLetter(scheme[0]) || scheme.any { it !in SCHEME_CHARS }) {
                error.value = E_BAD_SCHEME
                return null
            }

            if (scheme.length > 1024) {
                error.value = E_SCHEME_LIMIT
                return null
            }

            // Every char is ASCII here, so the invariant lowercase is the ASCII one.
            return Syntax(DotNetText.asciiFold(scheme), -1, V1_UNKNOWN_FLAGS)
        }

        private fun isFile(syntax: Syntax) = syntax.schemeName == "file"

        private fun startsWithOrdinalIgnoreCase(s: String, prefix: String): Boolean =
            s.length >= prefix.length && DotNetText.asciiFold(s.substring(0, prefix.length)) == prefix

        private fun isAsciiLetter(c: Char) = c in 'a'..'z' || c in 'A'..'Z'

        private fun isAsciiHexDigit(c: Char) = c in '0'..'9' || c in 'a'..'f' || c in 'A'..'F'

        /** A char pointer read: the string's terminating NUL past the end. */
        private fun at(s: String, i: Int): Char = if (i < s.length) s[i] else '\u0000'

        private fun isLWS(ch: Char) = ch <= ' ' && (ch == ' ' || ch == '\n' || ch == '\r' || ch == '\t')

        private fun lengthWithoutTrailingSpaces(str: String, length: Int, idx: Int): Int {
            var num = length
            while (num > idx && isLWS(str[num - 1])) {
                num--
            }
            return num
        }

        private fun checkForUnicodeOrEscapedUnreserved(data: String): Boolean {
            var i = 0
            while (i < data.length) {
                val c = data[i]
                if (c == '%') {
                    if (i + 2 < data.length) {
                        val c2 = decodeHexChars(data[i + 1], data[i + 2])
                        if (c2.code > 0x7F || c2 in UNRESERVED) {
                            return true
                        }
                        i += 2
                    }
                } else if (c > '\u007f') {
                    return true
                }
                i++
            }
            return false
        }

        // region UriHelper

        private const val UNRESERVED = "-.0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz~"
        private const val UNRESERVED_RESERVED = "!#$&'()*+,-./0123456789:;=?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]_abcdefghijklmnopqrstuvwxyz~"
        private const val UNRESERVED_RESERVED_EXCEPT_QUESTION_MARK_HASH = "!$&'()*+,-./0123456789:;=@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]_abcdefghijklmnopqrstuvwxyz~"
        private const val UNRESERVED_RESERVED_EXCEPT_HASH = "!$&'()*+,-./0123456789:;=?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]_abcdefghijklmnopqrstuvwxyz~"

        /** UriHelper.UnescapeString: [input] from [start] to [end] into [dest], per [unescapeMode] (UnescapeMode flags). */
        private fun unescapeString(
            input: String, startIn: Int, end: Int, dest: StringBuilder, rsvd1: Char, rsvd2: Char, rsvd3: Char,
            unescapeMode: Int, syntax: Syntax?, isQuery: Boolean,
        ) {
            if ((unescapeMode and U_ESCAPE_UNESCAPE) == 0) {
                dest.append(input, startIn, end)
                return
            }

            var start = startIn
            var flag = false
            // IriParsingStatic(syntax): every syntax ported here allows IRI parsing.
            val flag2 = (syntax == null || syntax.inFact(S_ALLOW_IRI_PARSING)) && (unescapeMode and U_ESCAPE_UNESCAPE) == U_ESCAPE_UNESCAPE
            var i = start

            while (i < end) {
                var c = '\u0000'

                scan@ while (i < end) {
                    c = input[i]
                    if (c == '%') {
                        if ((unescapeMode and U_UNESCAPE) == 0) {
                            flag = true
                            break@scan
                        }
                        if (i + 2 < end) {
                            c = decodeHexChars(input[i + 1], input[i + 2])
                            if (unescapeMode < U_UNESCAPE_ALL) {
                                when {
                                    c == '￿' -> {
                                        if ((unescapeMode and U_ESCAPE) == 0) {
                                            i++
                                            continue@scan
                                        }
                                        flag = true
                                    }

                                    c == '%' -> {
                                        i += 3
                                        continue@scan
                                    }

                                    c == rsvd1 || c == rsvd2 || c == rsvd3 -> {
                                        i += 3
                                        continue@scan
                                    }

                                    (unescapeMode and U_V1_TO_STRING) == 0 && isNotSafeForUnescape(c) -> {
                                        i += 3
                                        continue@scan
                                    }

                                    flag2 && ((c <= '\u009f' && isNotSafeForUnescape(c)) || (c > '\u009f' && !checkIriUnicodeRange(c, isQuery))) -> {
                                        i += 3
                                        continue@scan
                                    }
                                }
                                break@scan
                            }
                            if (c != '￿') {
                                break@scan
                            }
                        } else if (unescapeMode < U_UNESCAPE_ALL) {
                            flag = true
                            break@scan
                        }
                    } else if ((unescapeMode and (U_UNESCAPE or U_UNESCAPE_ALL)) != (U_UNESCAPE or U_UNESCAPE_ALL) && (unescapeMode and U_ESCAPE) != 0) {
                        if (c == rsvd1 || c == rsvd2 || c == rsvd3) {
                            flag = true
                            break@scan
                        }
                        if ((unescapeMode and U_V1_TO_STRING) == 0 && (c <= '\u001f' || (c >= '\u007f' && c <= '\u009f'))) {
                            flag = true
                            break@scan
                        }
                    }
                    i++
                }

                dest.append(input, start, i)

                if (i != end) {
                    if (flag) {
                        // (byte)pStr[i]: the char's low byte.
                        percentEncodeByte(input[i].code and 0xFF, dest)
                        flag = false
                        i++
                    } else if (c <= '\u007f') {
                        dest.append(c)
                        i += 3
                    } else {
                        i += unescapePercentEncodedUtf8Sequence(input, i, end - i, dest, isQuery, flag2)
                    }
                    start = i
                }
            }
        }

        private fun hexValue(c: Char): Int = when (c) {
            in '0'..'9' -> c - '0'
            in 'A'..'F' -> c - 'A' + 10
            in 'a'..'f' -> c - 'a' + 10
            else -> 255
        }

        private fun decodeHexChars(first: Char, second: Char): Char {
            val num = hexValue(first)
            val num2 = hexValue(second)
            if ((num or num2) == 255) {
                return '\uffff'
            }
            return ((num shl 4) or num2).toChar()
        }

        private const val HEX_UPPER = "0123456789ABCDEF"

        private fun percentEncodeByte(b: Int, to: StringBuilder) {
            to.append('%')
            to.append(HEX_UPPER[(b shr 4) and 0xF])
            to.append(HEX_UPPER[b and 0xF])
        }

        private fun escapeString(s: String, checkExistingEscaped: Boolean, noEscape: String): String {
            val dest = StringBuilder(s.length)
            escapeString(s, dest, checkExistingEscaped, noEscape)
            return dest.toString()
        }

        private fun escapeString(s: String, dest: StringBuilder, checkExistingEscaped: Boolean, noEscape: String) {
            var i = 0
            while (i < s.length) {
                val c = s[i]
                if (c.code > 0x7F) {
                    // Rune.DecodeFromUtf16: a lone surrogate becomes U+FFFD and consumes one char.
                    val codePoint: Int
                    if (Character.isHighSurrogate(c) && i + 1 < s.length && Character.isLowSurrogate(s[i + 1])) {
                        codePoint = Character.toCodePoint(c, s[i + 1])
                        i += 2
                    } else {
                        codePoint = if (Character.isSurrogate(c)) 0xFFFD else c.code
                        i++
                    }
                    for (b in String(Character.toChars(codePoint)).toByteArray(Charsets.UTF_8)) {
                        percentEncodeByte(b.toInt() and 0xFF, dest)
                    }
                } else if (c !in noEscape) {
                    if (c == '%' && checkExistingEscaped && s.length - i > 2 && isAsciiHexDigit(s[i + 1]) && isAsciiHexDigit(s[i + 2])) {
                        dest.append('%').append(s[i + 1]).append(s[i + 2])
                        i += 3
                    } else {
                        percentEncodeByte(c.code, dest)
                        i++
                    }
                } else {
                    dest.append(c)
                    i++
                }
            }
        }

        private fun isNotSafeForUnescape(ch: Char): Boolean {
            if (ch <= '\u001f' || (ch >= '\u007f' && ch <= '\u009f')) {
                return true
            }
            return ch in ";/?:@&=+$,#[]!'()*%\\#"
        }

        private fun isGenDelim(ch: Char) = ch == ':' || ch == '/' || ch == '?' || ch == '#' || ch == '[' || ch == ']' || ch == '@'

        private fun isBidiControlCharacter(ch: Char) = ch in '\u200e'..'\u202e' && ch !in '\u2010'..'\u2029'

        private fun stripBidiControlCharacters(s: String): String? {
            if (s.none(::isBidiControlCharacter)) {
                return null
            }
            return s.filterNot(::isBidiControlCharacter)
        }

        // string.Normalize(NormalizationForm.FormC), which throws ArgumentException on invalid UTF-16.
        private fun normalizeAndConcat(start: String?, toNormalize: String): String {
            var i = 0
            while (i < toNormalize.length) {
                val c = toNormalize[i]
                if (Character.isHighSurrogate(c) && i + 1 < toNormalize.length && Character.isLowSurrogate(toNormalize[i + 1])) {
                    i += 2
                    continue
                }
                if (Character.isSurrogate(c)) {
                    throw IllegalArgumentException("Invalid Unicode code point found at index $i.")
                }
                i++
            }
            return (start ?: "") + Normalizer.normalize(toNormalize, Normalizer.Form.NFC)
        }

        private fun compress(span: CharArray, convertPathSlashes: Boolean, canonicalizeAsFilePath: Boolean): Int {
            if (span.isEmpty()) {
                return 0
            }
            if (convertPathSlashes) {
                for (k in span.indices) {
                    if (span[k] == '\\') span[k] = '/'
                }
            }

            val list = ArrayList<IntArray>()
            var num = 0
            var num2 = 0
            var num3 = 0
            var num4 = 0
            var num5 = span.size - 1

            while (num5 >= 0) {
                val c = span[num5]
                if (c == '/') {
                    num++
                } else {
                    if (num > 1) {
                        num2 = num5 + 1
                    }
                    num = 0
                }

                if (c == '.') {
                    num3++
                    num5--
                    continue
                }

                if (num3 != 0) {
                    if ((!canonicalizeAsFilePath || (num3 <= 2 && c == '/')) && c == '/' &&
                        (num2 == num5 + num3 + 1 || (num2 == 0 && num5 + num3 + 1 == span.size)) && num3 <= 2
                    ) {
                        list.add(intArrayOf(num5 + 1, num3 + if (num2 != 0) 1 else 0))
                        num2 = num5
                        if (num3 == 2) {
                            num4++
                        }
                        num3 = 0
                        num5--
                        continue
                    }
                    num3 = 0
                }

                if (c == '/') {
                    if (num4 != 0) {
                        num4--
                        list.add(intArrayOf(num5 + 1, num2 - num5))
                    }
                    num2 = num5
                }
                num5--
            }

            if (canonicalizeAsFilePath && num <= 1) {
                if (num4 != 0 && span[0] != '/') {
                    list.add(intArrayOf(0, num2 + 1))
                } else if (num3 != 0 && (num2 == num3 || (num2 == 0 && num3 == span.size))) {
                    list.add(intArrayOf(0, num3 + if (num2 != 0) 1 else 0))
                }
            }

            if (list.isEmpty()) {
                return span.size
            }

            var num6 = list[list.size - 1][0]
            var num7 = num6
            for (num8 in list.indices.reversed()) {
                val num9 = list[num8][0]
                val num10 = list[num8][1]
                if (num7 != num9) {
                    val num11 = num9 - num7
                    System.arraycopy(span, num7, span, num6, num11)
                    num6 += num11
                }
                num7 = num9 + num10
            }
            if (num7 != span.size) {
                System.arraycopy(span, num7, span, num6, span.size - num7)
                num6 += span.size - num7
            }
            return num6
        }

        private fun unescapeOnly(sb: StringBuilder, start: Int, ch1: Char, ch2: Char, ch3: Char) {
            var end = sb.length
            if (end - start < 3) {
                return
            }
            val buf = CharArray(end)
            sb.getChars(0, end, buf, 0)

            var limit = end - 2
            var pch = start
            var ptr2 = -1

            while (pch < limit) {
                if (buf[pch++] != '%') {
                    continue
                }
                var c = decodeHexChars(buf[pch++], buf[pch++])
                if (c != ch1 && c != ch2 && c != ch3) {
                    continue
                }
                ptr2 = pch - 2
                buf[ptr2 - 1] = c
                while (pch < limit) {
                    val copied = buf[pch++]
                    buf[ptr2++] = copied
                    if (copied == '%') {
                        val h1 = buf[pch++]
                        buf[ptr2++] = h1
                        val h2 = buf[pch++]
                        buf[ptr2++] = h2
                        c = decodeHexChars(h1, h2)
                        if (c == ch1 || c == ch2 || c == ch3) {
                            ptr2 -= 2
                            buf[ptr2 - 1] = c
                        }
                    }
                }
                break
            }

            limit += 2
            if (ptr2 == -1) {
                return
            }
            if (pch != limit) {
                buf[ptr2++] = buf[pch++]
                if (pch != limit) {
                    buf[ptr2++] = buf[pch++]
                }
            }
            end -= pch - ptr2

            sb.setLength(0)
            sb.appendRange(buf, 0, end)
        }

        // endregion

        // region IriHelper / PercentEncodingHelper

        private fun inRange(value: Int, min: Int, max: Int) = value in min..max

        private fun checkIriUnicodeRange(unicode: Char, isQuery: Boolean): Boolean {
            val v = unicode.code
            if (!inRange(v, 160, 55295) && !inRange(v, 63744, 64975) && !inRange(v, 65008, 65519)) {
                return isQuery && inRange(v, 57344, 63743)
            }
            return true
        }

        private fun checkIriUnicodeRange(highSurr: Char, lowSurr: Char, isQuery: Boolean): Boolean {
            if (Character.isHighSurrogate(highSurr) && Character.isLowSurrogate(lowSurr)) {
                val value = Character.toCodePoint(highSurr, lowSurr)
                if ((value and 0xFFFF) < 65534 && (value - 917504) !in 0 until 4096) {
                    return isQuery || value < 983040
                }
                return false
            }
            return false
        }

        private fun checkIriUnicodeRange(value: Int, isQuery: Boolean): Boolean {
            if (value <= 65535) {
                if (!inRange(value, 160, 55295) && !inRange(value, 63744, 64975) && !inRange(value, 65008, 65519)) {
                    return isQuery && inRange(value, 57344, 63743)
                }
                return true
            }
            if ((value and 0xFFFF) < 65534 && !inRange(value, 917504, 921599)) {
                return isQuery || value < 983040
            }
            return false
        }

        private fun checkIsReserved(ch: Char, component: Int): Boolean {
            if ((C_ABSOLUTE_URI and component) == 0) {
                if (component == 0) {
                    return isGenDelim(ch)
                }
                return false
            }
            return ch in ";/?:@&=+$,#[]!'()*"
        }

        private fun escapeUnescapeIri(input: String, start: Int, end: Int, component: Int): String {
            val to = StringBuilder(end - start)
            var i = start
            while (i < end) {
                var c = input[i]
                if (c == '%') {
                    if (end - i > 2) {
                        c = decodeHexChars(input[i + 1], input[i + 2])
                        if (c == '\uffff' || c == '%' || checkIsReserved(c, component) || isNotSafeForUnescape(c)) {
                            to.append(input[i++])
                            to.append(input[i++])
                            to.append(input[i])
                        } else if (c <= '\u007f') {
                            to.append(c)
                            i += 2
                        } else {
                            val num2 = unescapePercentEncodedUtf8Sequence(input, i, end - i, to, component == C_QUERY, iriParsing = true)
                            i += num2 - 1
                        }
                    } else {
                        to.append(input[i])
                    }
                } else if (c > '\u007f') {
                    var isSurrogatePair = false
                    var c2 = '\u0000'
                    val flag: Boolean
                    if (Character.isHighSurrogate(c) && i + 1 < end) {
                        c2 = input[i + 1]
                        isSurrogatePair = Character.isLowSurrogate(c2)
                        flag = checkIriUnicodeRange(c, c2, component == C_QUERY)
                    } else {
                        flag = checkIriUnicodeRange(c, component == C_QUERY)
                    }
                    if (flag) {
                        to.append(c)
                        if (isSurrogatePair) {
                            to.append(c2)
                        }
                    } else {
                        val codePoint = when {
                            isSurrogatePair -> Character.toCodePoint(c, c2)
                            Character.isSurrogate(c) -> 0xFFFD
                            else -> c.code
                        }
                        for (b in String(Character.toChars(codePoint)).toByteArray(Charsets.UTF_8)) {
                            percentEncodeByte(b.toInt() and 0xFF, to)
                        }
                    }
                    if (isSurrogatePair) {
                        i++
                    }
                } else {
                    to.append(input[i])
                }
                i++
            }
            return to.toString()
        }

        /** Rune.DecodeFromUtf8: (codePoint or -1 when invalid/incomplete, bytesConsumed). */
        private fun decodeUtf8(bytes: IntArray, count: Int): Pair<Int, Int> {
            val b0 = bytes[0]
            if (b0 < 0x80) return b0 to 1
            val (needed, lo, hi) = when (b0) {
                in 0xC2..0xDF -> Triple(1, 0x80, 0xBF)
                0xE0 -> Triple(2, 0xA0, 0xBF)
                in 0xE1..0xEC, 0xEE, 0xEF -> Triple(2, 0x80, 0xBF)
                0xED -> Triple(2, 0x80, 0x9F)
                0xF0 -> Triple(3, 0x90, 0xBF)
                in 0xF1..0xF3 -> Triple(3, 0x80, 0xBF)
                0xF4 -> Triple(3, 0x80, 0x8F)
                else -> return -1 to 1
            }
            var codePoint = when (needed) {
                1 -> b0 and 0x1F
                2 -> b0 and 0x0F
                else -> b0 and 0x07
            }
            for (k in 1..needed) {
                if (k >= count) return -1 to k
                val b = bytes[k]
                val (min, max) = if (k == 1) lo to hi else 0x80 to 0xBF
                if (b !in min..max) return -1 to k
                codePoint = (codePoint shl 6) or (b and 0x3F)
            }
            return codePoint to needed + 1
        }

        private fun unescapePercentEncodedUtf8Sequence(input: String, offset: Int, length: Int, dest: StringBuilder, isQuery: Boolean, iriParsing: Boolean): Int {
            fun ch(k: Int) = input[offset + k]

            var num = 0
            var num2 = 0
            var num3 = 0
            var num4 = 0
            var bytesConsumed = 0

            while (true) {
                var num5 = num3 + num2 * 3
                var decode = false

                while (true) {
                    var invalid = true
                    if (length - num5 > 2 && ch(num5) == '%') {
                        var num6 = ch(num5 + 1).code
                        var ok = true
                        if (((num6 - 65) and -33) in 0..5) {
                            num6 = (num6 or 0x20) - 97 + 10
                        } else if (num6 - 56 in 0..1) {
                            num6 -= 48
                        } else {
                            ok = false
                        }
                        if (ok) {
                            var num7 = ch(num5 + 2).code - 48
                            if (num7 !in 0..9) {
                                if (((num7 - 17) and -33) !in 0..5) {
                                    ok = false
                                } else {
                                    num7 = ((num7 + 48) or 0x20) - 97 + 10
                                }
                            }
                            if (ok) {
                                invalid = false
                                num6 = (num6 shl 4) or num7
                                num = (num ushr 8) or (num6 shl 24)
                                if (++num2 == 4) {
                                    decode = true
                                    break
                                }
                                num5 += 3
                                continue
                            }
                        }
                    }
                    if (invalid) {
                        if (num2 > 1) {
                            num = if (bytesConsumed != 1) num ushr (32 - (num2 shl 3)) else num ushr 8
                            decode = true
                            break
                        }
                        if ((num2 or num4) == 0) {
                            return num3
                        }
                        num2 *= 3
                        dest.append(input, offset + num3 - num4, offset + num3 + num2)
                        return num3 + num2
                    }
                }

                check(decode)
                val bytes = IntArray(4) { (num ushr (8 * it)) and 0xFF }
                val (codePoint, consumed) = decodeUtf8(bytes, num2)
                bytesConsumed = consumed
                if (codePoint >= 0 && (!iriParsing || checkIriUnicodeRange(codePoint, isQuery))) {
                    if (num4 != 0) {
                        dest.append(input, offset + num3 - num4, offset + num3)
                        num4 = 0
                    }
                    dest.appendCodePoint(codePoint)
                } else {
                    num4 += bytesConsumed * 3
                }
                num2 -= bytesConsumed
                num3 += bytesConsumed * 3
            }
        }

        // endregion

        // region DomainNameHelper / UncNameHelper

        private const val DNS_IRI_INVALID = "\u0000\u0001\u0002\u0003\u0004\u0005\u0006\u0007\u0008\t\n\u000b\u000c\r\u000e\u000f" +
            "\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001a\u001b\u001c\u001d\u001e\u001f" +
            " !\"#$%&'()*+,/:;<=>?@[\\]^`{|}~\u007f" +
            "\u0080\u0081\u0082\u0083\u0084\u0085\u0086\u0087\u0088\u0089\u008a\u008b\u008c\u008d\u008e\u008f" +
            "\u0090\u0091\u0092\u0093\u0094\u0095\u0096\u0097\u0098\u0099\u009a\u009b\u009c\u009d\u009e\u009f"

        private const val DOTS = ".\u3002\uff0e\uff61"

        /** DomainNameHelper.IsValid(iri: true): the valid host length from [start], or -1. */
        private fun domainNameIsValid(s: String, start: Int, end: Int, iri: Boolean, notImplicitFile: Boolean): Int {
            check(iri)
            var hostEnd = end
            for (k in start until end) {
                val c = s[k]
                if (c in DNS_IRI_INVALID) {
                    val terminator = c == '/' || c == '\\' || (notImplicitFile && (c == '#' || c == ':' || c == '?'))
                    if (!terminator) {
                        return -1
                    }
                    hostEnd = k
                    break
                }
            }

            val length = hostEnd - start
            if (length == 0) {
                return -1
            }

            var p = start
            while (true) {
                val c2 = s[p]
                if (c2 < '\u00a0' && !(isAsciiLetter(c2) || c2 in '0'..'9')) {
                    return -1
                }
                var num2 = -1
                for (k in p until hostEnd) {
                    if (s[k] in DOTS) {
                        num2 = k - p
                        break
                    }
                }
                var num3 = if (num2 < 0) hostEnd - p else num2
                val label = s.substring(p, p + num3)
                if (label.any { it.code > 0x7F }) {
                    num3 += 4
                    for (ch in label) {
                        if (ch > '\u00ff') {
                            num3++
                        }
                    }
                }
                if (num3 !in 1..63) {
                    return -1
                }
                if (num2 < 0) {
                    return length
                }
                p += num2 + 1
                if (p >= hostEnd) {
                    return length
                }
            }
        }

        private fun domainParseCanonicalName(str: String, start: Int, endIn: Int): String {
            var end = endIn
            var num = -1
            for (k in end - 1 downTo start) {
                val c = str[k]
                if (c in 'A'..'Z' || c == ':') {
                    num = k - start
                    break
                }
            }
            if (num >= 0 && str[start + num] == ':') {
                end = start + num
                num = -1
                for (k in start until end) {
                    if (str[k] in 'A'..'Z') {
                        num = k - start
                        break
                    }
                }
            }
            val span = str.substring(start, end)
            if (num >= 0) {
                val folded = DotNetText.asciiFold(span)
                if (folded == "localhost" || folded == "loopback") {
                    return "localhost"
                }
                return DotNetText.toLowerInvariant(span)
            }
            if (span == "localhost" || span == "loopback") {
                return "localhost"
            }
            return span
        }

        private fun uncIsValid(name: String, start: Int, returnedEnd: IntRef, notImplicitFile: Boolean): Boolean {
            var num = returnedEnd.value
            if (start == num) {
                return false
            }
            var flag = false
            var i = start
            while (i < num) {
                val c = name[i]
                if (c == '/' || c == '\\' || (notImplicitFile && (c == ':' || c == '?' || c == '#'))) {
                    num = i
                    break
                }
                if (c == '.') {
                    i++
                    break
                }
                if (Character.isLetter(c) || c == '-' || c == '_') {
                    flag = true
                } else if (c !in '0'..'9') {
                    return false
                }
                i++
            }
            if (!flag) {
                return false
            }
            while (i < num) {
                val c = name[i]
                if (c == '/' || c == '\\' || (notImplicitFile && (c == ':' || c == '?' || c == '#'))) {
                    num = i
                    break
                }
                if (c == '.') {
                    if (!flag || (i - 1 >= start && name[i - 1] == '.')) {
                        return false
                    }
                    flag = false
                    i++
                    continue
                }
                if (c == '-' || c == '_') {
                    if (!flag) {
                        return false
                    }
                    i++
                    continue
                }
                if (Character.isLetter(c) || c in '0'..'9') {
                    if (!flag) {
                        flag = true
                    }
                    i++
                    continue
                }
                return false
            }
            if (i - 1 >= start && name[i - 1] == '.') {
                flag = true
            }
            if (!flag) {
                return false
            }
            returnedEnd.value = num
            return true
        }

        // endregion

        // region IPv4AddressHelper

        private fun ipv4ParseCanonicalName(str: String, start: Int, end: Int): String {
            val num = ipv4ParseNonCanonical(str, start, IntRef(end), notImplicitFile = true)
            return "${(num shr 24) and 0xFF}.${(num shr 16) and 0xFF}.${(num shr 8) and 0xFF}.${num and 0xFF}"
        }

        private fun ipv4ParseHostNumber(str: String, startIn: Int, end: Int): Int {
            var start = startIn
            val span = IntArray(4)
            for (i in 0 until 4) {
                var num = 0
                while (start < end) {
                    val num2 = str[start].code
                    if (num2 == 46 || num2 == 58) {
                        break
                    }
                    num = num * 10 + num2 - 48
                    start++
                }
                span[i] = num and 0xFF
                start++
            }
            return (span[0] shl 24) or (span[1] shl 16) or (span[2] shl 8) or span[3]
        }

        private fun ipv4IsValid(name: String, start: Int, end: IntRef, allowIPv6: Boolean, notImplicitFile: Boolean, unknownScheme: Boolean): Boolean {
            if (allowIPv6 || unknownScheme) {
                return ipv4IsValidCanonical(name, start, end, allowIPv6, notImplicitFile)
            }
            return ipv4ParseNonCanonical(name, start, end, notImplicitFile) != -1L
        }

        private fun ipv4IsValidCanonical(name: String, startIn: Int, end: IntRef, allowIPv6: Boolean, notImplicitFile: Boolean): Boolean {
            var start = startIn
            var num = 0
            var num2 = 0L
            var flag = false
            var flag2 = false
            while (start < end.value) {
                val num3 = at(name, start).code
                if (allowIPv6) {
                    if (num3 == 93 || num3 == 47 || num3 == 37) {
                        break
                    }
                } else if (num3 == 47 || num3 == 92 || (notImplicitFile && (num3 == 58 || num3 == 63 || num3 == 35))) {
                    break
                }
                val num4 = num3 - 48
                if (num4 in 0..9) {
                    if (!flag && num4 == 0) {
                        if (start + 1 < end.value && at(name, start + 1) == '0') {
                            return false
                        }
                        flag2 = true
                    }
                    flag = true
                    num2 = num2 * 10 + num4
                    if (num2 > 255) {
                        return false
                    }
                } else {
                    if (num3 != 46) {
                        return false
                    }
                    if (!flag || (num2 > 0 && flag2)) {
                        return false
                    }
                    num++
                    flag = false
                    num2 = 0
                    flag2 = false
                }
                start++
            }
            val ok = num == 3 && flag
            if (ok) {
                end.value = start
            }
            return ok
        }

        private fun ipv4ParseNonCanonical(name: String, start: Int, end: IntRef, notImplicitFile: Boolean): Long {
            var num: Int
            var num2 = 0
            val span = LongArray(3)
            var num3 = 0L
            var flag = false
            var num4 = 0
            var i = start

            while (i < end.value) {
                num2 = name[i].code
                num3 = 0
                num = 10
                if (num2 == 48) {
                    i++
                    flag = true
                    if (i < end.value) {
                        num2 = name[i].code
                        if (num2 == 120 || num2 == 88) {
                            num = 16
                            i++
                            flag = false
                        } else {
                            num = 8
                        }
                    }
                }
                while (i < end.value) {
                    num2 = name[i].code
                    val num5 = if (num2 < 256) hexValue(num2.toChar()) else 255
                    if (num5 >= num) {
                        break
                    }
                    num3 = num3 * num + num5
                    if (num3 > 0xFFFFFFFFL) {
                        return -1
                    }
                    flag = true
                    i++
                }
                if (i >= end.value || num2 != 46) {
                    break
                }
                if (num4 >= 3 || !flag || num3 > 255) {
                    return -1
                }
                span[num4] = num3
                num4++
                flag = false
                i++
            }

            if (!flag) {
                return -1
            }

            if (i < end.value) {
                if (num2 != 47 && num2 != 92 && (!notImplicitFile || (num2 != 58 && num2 != 63 && num2 != 35))) {
                    return -1
                }
                end.value = i
            }

            return when (num4) {
                0 -> num3
                1 -> if (num3 > 16777215) -1 else (span[0] shl 24) or num3
                2 -> if (num3 > 65535) -1 else (span[0] shl 24) or (span[1] shl 16) or num3
                3 -> if (num3 > 255) -1 else (span[0] shl 24) or (span[1] shl 16) or (span[2] shl 8) or num3
                else -> -1
            }
        }

        // endregion

        // region IPv6AddressHelper

        private fun ipv6IsValid(name: String, startIn: Int, end: IntRef): Boolean {
            var start = startIn
            var num = 0
            var num2 = 0
            var flag = false
            var flag2 = false
            var flag3 = false
            var flag4 = true
            var start2 = 1

            if (at(name, start) == ':' && (start + 1 >= end.value || at(name, start + 1) != ':')) {
                return false
            }

            var i = start
            while (i < end.value) {
                val ch = at(name, i)
                if (if (flag3) ch in '0'..'9' else isAsciiHexDigit(ch)) {
                    num2++
                    flag4 = false
                    i++
                    continue
                }
                if (num2 > 4) {
                    return false
                }
                if (num2 != 0) {
                    num++
                    start2 = i - num2
                }

                // 0: IL_014d (reset the sequence length), 1: IL_00dd (found ']'), 2: IL_010b (prefix '/').
                var label: Int
                if (ch.code <= 46) {
                    if (ch == '%') {
                        while (true) {
                            if (++i == end.value) {
                                return false
                            }
                            if (at(name, i) == ']') {
                                label = 1
                                break
                            }
                            if (at(name, i) == '/') {
                                label = 2
                                break
                            }
                        }
                    } else if (ch != '.') {
                        return false
                    } else {
                        if (flag2) {
                            return false
                        }
                        val ipv4End = IntRef(end.value)
                        if (!ipv4IsValid(name, start2, ipv4End, allowIPv6 = true, notImplicitFile = false, unknownScheme = false)) {
                            return false
                        }
                        i = ipv4End.value
                        num++
                        flag2 = true
                        i--
                        label = 0
                    }
                } else if (ch == '/') {
                    label = 2
                } else if (ch != ':') {
                    if (ch == ']') {
                        label = 1
                    } else {
                        return false
                    }
                } else {
                    if (i > 0 && at(name, i - 1) == ':') {
                        if (flag) {
                            return false
                        }
                        flag = true
                        flag4 = false
                    } else {
                        flag4 = true
                    }
                    label = 0
                }

                when (label) {
                    1 -> {
                        start = i
                        i = end.value
                        i++
                        continue
                    }
                    2 -> {
                        if (num == 0 || flag3) {
                            return false
                        }
                        flag3 = true
                        flag4 = true
                    }
                }
                num2 = 0
                i++
            }

            if (flag3 && (num2 < 1 || num2 > 2)) {
                return false
            }
            val num3 = 8 + if (flag3) 1 else 0
            if (!flag4 && num2 <= 4 && (if (flag) num < num3 else num == num3)) {
                if (i == end.value + 1) {
                    end.value = start + 1
                    return true
                }
                return false
            }
            return false
        }

        private fun ipv6Parse(address: String, offset: Int, numbers: IntArray): String? {
            fun a(k: Int) = at(address, offset + k)
            val addressLength = address.length - offset

            var num = 0
            var num2 = 0
            var num3 = -1
            var flag = true
            var scopeId: String? = null
            var i = if (a(0) == '[') 1 else 0

            while (i < addressLength && a(i) != ']') {
                when (val num4 = a(i).code) {
                    37 -> {
                        if (flag) {
                            numbers[num2++] = num and 0xFFFF
                            flag = false
                        }
                        val num7 = i
                        i++
                        while (i < addressLength && a(i) != ']' && a(i) != '/') {
                            i++
                        }
                        scopeId = address.substring(offset + num7, offset + i)
                        while (i < addressLength && a(i) != ']') {
                            i++
                        }
                    }
                    58 -> {
                        numbers[num2++] = num and 0xFFFF
                        num = 0
                        i++
                        if (a(i) == ':') {
                            num3 = num2
                            i++
                        } else if (num3 < 0 && num2 < 6) {
                            continue
                        }
                        var j = i
                        while (j < addressLength && a(j) != ']' && a(j) != ':' && a(j) != '%' && a(j) != '/' && j < i + 4) {
                            if (a(j) == '.') {
                                while (j < addressLength && a(j) != ']' && a(j) != '/' && a(j) != '%') {
                                    j++
                                }
                                val num6 = ipv4ParseHostNumber(address, offset + i, offset + j)
                                numbers[num2++] = (num6 ushr 16) and 0xFFFF
                                numbers[num2++] = num6 and 0xFFFF
                                i = j
                                num = 0
                                flag = false
                                break
                            }
                            j++
                        }
                    }
                    47 -> {
                        if (flag) {
                            numbers[num2++] = num and 0xFFFF
                            flag = false
                        }
                        i++
                        while (i < addressLength && a(i) != ']') {
                            i++
                        }
                    }
                    else -> {
                        val num5 = if (num4 < 256) hexValue(num4.toChar()) else 255
                        num = num * 16 + num5
                        i++
                    }
                }
            }

            if (flag) {
                numbers[num2++] = num and 0xFFFF
            }

            if (num3 > 0) {
                var num8 = 7
                var num9 = num2 - 1
                if (num9 != num8) {
                    var num10 = num2 - num3
                    while (num10 > 0) {
                        numbers[num8--] = numbers[num9]
                        numbers[num9--] = 0
                        num10--
                    }
                }
            }

            return scopeId
        }

        private fun ipv6ParseCanonicalName(str: String, offset: Int, info: UriInfo): String {
            val span = IntArray(8)
            val scopeId = ipv6Parse(str, offset, span)
            if (!scopeId.isNullOrEmpty()) {
                info.scopeId = scopeId
            }

            // FindCompressionRange
            var longest = 0
            var longestStart = -1
            var run = 0
            for (i in 0 until 8) {
                if (span[i] == 0) {
                    run++
                    if (run > longest) {
                        longest = run
                        longestStart = i - run + 1
                    }
                } else {
                    run = 0
                }
            }
            val item: Int
            val item2: Int
            if (longest <= 1) {
                item = -1
                item2 = 0
            } else {
                item = longestStart
                item2 = longestStart + longest
            }

            val embedIpv4 = (span[0] == 0 && span[1] == 0 && span[2] == 0 && span[3] == 0 && span[6] != 0 &&
                ((span[4] == 0 && (span[5] == 0 || span[5] == 0xFFFF)) || (span[4] == 0xFFFF && span[5] == 0))) ||
                (span[4] == 0 && span[5] == 24318)

            val sb = StringBuilder("[")
            for (i in 0 until 8) {
                if (embedIpv4 && i == 6) {
                    sb.append(':')
                    sb.append(span[i] shr 8).append('.').append(span[i] and 0xFF).append('.')
                    sb.append(span[i + 1] shr 8).append('.').append(span[i + 1] and 0xFF)
                    break
                }
                if (item == i) {
                    sb.append(':')
                }
                if (item <= i && item2 == 8) {
                    sb.append(':')
                    break
                }
                if (item > i || i >= item2) {
                    if (i != 0) {
                        sb.append(':')
                    }
                    sb.append(Integer.toHexString(span[i]))
                }
            }
            sb.append(']')
            return sb.toString()
        }

        // endregion
    }

    // endregion
}
