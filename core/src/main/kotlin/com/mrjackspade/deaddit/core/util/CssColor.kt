package com.mrjackspade.deaddit.core.util

import kotlin.math.PI
import kotlin.math.abs
import kotlin.math.cos
import kotlin.math.floor
import kotlin.math.max
import kotlin.math.min
import kotlin.math.pow
import kotlin.math.sin

/** A CSS color value: a color, or `currentcolor` (the element's own text color). */
sealed interface CssColorValue {
    data class Argb(val argb: Int) : CssColorValue

    data object CurrentColor : CssColorValue
}

/**
 * Flair colors as Reddit sends them: CSS colors, which the C# app put straight into a style
 * declaration in its WebView. This reads them as a browser does, CSS Color 4: named colors,
 * `transparent`, `currentcolor`, hex, `rgb()`/`rgba()`, `hsl()`/`hsla()`, `hwb()`, `lab()`, `lch()`,
 * `oklab()` and `oklch()`, in their legacy (commas) and modern (spaces, `/ alpha`, `none`) forms.
 *
 * A value the browser rejects makes it ignore the declaration, so [parse] returns null for it and the
 * caller applies no color, as the WebView did. Forms a browser accepts that aren't implemented here
 * (system colors, CSS-wide keywords, `color()`, `color-mix()`, `light-dark()`, relative colors, and
 * `calc()` or `var()` inside a color) throw, rather than being guessed at.
 */
object CssColor {
    /** [value] as a browser reads `property: value`; null when the browser would ignore it. */
    fun parse(value: String): CssColorValue? {
        val v = declarationValue(value) ?: return null
        if (v.isEmpty()) return null

        if (v.startsWith('#')) return hex(v.substring(1))?.let(CssColorValue::Argb)

        val open = v.indexOf('(')
        if (open < 0) return keyword(v.asciiLowercase(), value)

        val name = v.substring(0, open).asciiLowercase()
        if (!IDENT.matches(name)) return null

        // A function left open at the end of the value is closed by it, as CSS does at EOF.
        val inner = if (v.endsWith(')')) v.substring(open + 1, v.length - 1) else v.substring(open + 1)
        if (inner.contains('(') || inner.contains(')')) {
            if (inner.contains('(')) throw UnsupportedOperationException("CSS color '$value' has a nested function, which isn't implemented.")
            return null
        }

        if (name in UNSUPPORTED_FUNCTIONS) throw UnsupportedOperationException("CSS color function $name() in '$value' isn't implemented.")

        val args = tokens(inner) ?: return null
        if (args.firstOrNull() == Token.Ident("from")) throw UnsupportedOperationException("Relative CSS color '$value' isn't implemented.")

        val argb = when (name) {
            "rgb", "rgba" -> rgb(args)
            "hsl", "hsla" -> hsl(args)
            "hwb" -> hwb(args)
            "lab" -> lab(args)
            "lch" -> lch(args)
            "oklab" -> oklab(args)
            "oklch" -> oklch(args)
            else -> null
        }
        return argb?.let(CssColorValue::Argb)
    }

    /**
     * The value a declaration in a style attribute gets from [raw]: up to the first ';' outside
     * brackets, without comments or `!important`, trimmed. Null when it can't be a value.
     */
    private fun declarationValue(raw: String): String? {
        val noComments = COMMENT.replace(raw, " ")
        var depth = 0
        var end = noComments.length
        for ((i, c) in noComments.withIndex()) {
            when (c) {
                '(' -> depth++
                ')' -> depth--
                ';' -> if (depth <= 0) { end = i; break }
            }
        }

        var v = noComments.substring(0, end).trim(::isCssSpace)
        val important = IMPORTANT.find(v)
        if (important != null) v = v.substring(0, important.range.first).trim(::isCssSpace)
        if (v.contains('!')) return null
        return v
    }

    private fun keyword(name: String, value: String): CssColorValue? {
        if (name == "transparent") return CssColorValue.Argb(0)
        if (name == "currentcolor") return CssColorValue.CurrentColor
        NAMED[name]?.let { return CssColorValue.Argb(it or (0xFF shl 24)) }
        if (name in SYSTEM_COLORS || name in CSS_WIDE_KEYWORDS || name.startsWith("-webkit-") || name.startsWith("-internal-")) {
            throw UnsupportedOperationException("CSS color keyword '$value' isn't implemented.")
        }
        return null
    }

    private fun hex(digits: String): Int? {
        if (digits.isEmpty() || !digits.all { it in '0'..'9' || it.lowercaseChar() in 'a'..'f' }) return null
        fun channel(s: String) = (if (s.length == 1) s + s else s).toInt(16)

        val (r, g, b, a) = when (digits.length) {
            3 -> listOf(channel(digits.substring(0, 1)), channel(digits.substring(1, 2)), channel(digits.substring(2, 3)), 255)
            4 -> listOf(channel(digits.substring(0, 1)), channel(digits.substring(1, 2)), channel(digits.substring(2, 3)), channel(digits.substring(3, 4)))
            6 -> listOf(channel(digits.substring(0, 2)), channel(digits.substring(2, 4)), channel(digits.substring(4, 6)), 255)
            // CSS's 8-digit form is RRGGBBAA.
            8 -> listOf(channel(digits.substring(0, 2)), channel(digits.substring(2, 4)), channel(digits.substring(4, 6)), channel(digits.substring(6, 8)))
            else -> return null
        }
        return (a shl 24) or (r shl 16) or (g shl 8) or b
    }

    // ----- Function arguments -----

    private sealed interface Token {
        data class Number(val value: Double, val unit: String?) : Token
        data class Ident(val name: String) : Token
        data object Comma : Token
        data object Slash : Token
    }

    private fun tokens(inner: String): List<Token>? {
        val result = mutableListOf<Token>()
        var i = 0
        while (i < inner.length) {
            val c = inner[i]
            when {
                isCssSpace(c) -> i++
                c == ',' -> { result += Token.Comma; i++ }
                c == '/' -> { result += Token.Slash; i++ }
                else -> {
                    val number = NUMBER.matchAt(inner, i)
                    if (number != null) {
                        i += number.value.length
                        val unit = when {
                            i < inner.length && inner[i] == '%' -> { i++; "%" }
                            else -> UNIT.matchAt(inner, i)?.value?.also { i += it.length }?.asciiLowercase()
                        }
                        result += Token.Number(number.value.toDouble(), unit)
                    } else {
                        val ident = UNIT.matchAt(inner, i) ?: return null
                        result += Token.Ident(ident.value.asciiLowercase())
                        i += ident.value.length
                    }
                }
            }
        }
        return result
    }

    /** Three values (numbers, or `none` in the modern syntax) and an optional alpha. */
    private class Args(val values: List<Token>, val alpha: Token?, val legacy: Boolean)

    /**
     * Splits [tokens] into three values and an optional alpha: "a, b, c[, alpha]" (legacy, when
     * [legacyAllowed]) or "a b c[ / alpha]". Null when they're neither.
     */
    private fun split(tokens: List<Token>, legacyAllowed: Boolean): Args? {
        if (tokens.any { it == Token.Comma }) {
            if (!legacyAllowed) return null
            if (tokens.size != 5 && tokens.size != 7) return null
            for ((i, t) in tokens.withIndex()) if ((i % 2 == 1) != (t == Token.Comma)) return null
            val values = tokens.filterIndexed { i, _ -> i % 2 == 0 }
            if (values.any { it !is Token.Number }) return null
            return Args(values.take(3), values.getOrNull(3), legacy = true)
        }

        val slash = tokens.indexOf(Token.Slash)
        val values = if (slash < 0) tokens else tokens.subList(0, slash)
        if (values.size != 3) return null
        val alpha = if (slash < 0) null else {
            if (tokens.size != slash + 2) return null
            tokens[slash + 1]
        }
        if ((values + listOfNotNull(alpha)).any { it is Token.Comma || it is Token.Slash || (it is Token.Ident && it.name != "none") }) return null
        return Args(values, alpha, legacy = false)
    }

    private val Token.isNone get() = this is Token.Ident && name == "none"

    /** A number or percentage, as a number where 100% is [percentScale]; 0 for `none`. */
    private fun numberOrPercent(t: Token, percentScale: Double): Double? = when {
        t.isNone -> 0.0
        t is Token.Number && t.unit == null -> t.value
        t is Token.Number && t.unit == "%" -> t.value / 100 * percentScale
        else -> null
    }

    private fun hue(t: Token): Double? = when {
        t.isNone -> 0.0
        t is Token.Number -> when (t.unit) {
            null, "deg" -> t.value
            "grad" -> t.value * 0.9
            "rad" -> t.value * 180 / PI
            "turn" -> t.value * 360
            else -> null
        }
        else -> null
    }

    /** The alpha, 0..1; 1 when absent. */
    private fun alpha(t: Token?): Double? = if (t == null) 1.0 else numberOrPercent(t, 1.0)?.coerceIn(0.0, 1.0)

    private fun rgb(tokens: List<Token>): Int? {
        val args = split(tokens, legacyAllowed = true) ?: return null
        if (args.legacy) {
            val units = args.values.map { (it as Token.Number).unit }
            if (units.any { it != null && it != "%" } || units.distinct().size != 1) return null
        }

        val channels = args.values.map { numberOrPercent(it, 255.0) ?: return null }
        val a = alpha(args.alpha) ?: return null
        return argb(a, channels[0] / 255, channels[1] / 255, channels[2] / 255)
    }

    private fun hsl(tokens: List<Token>): Int? {
        val args = split(tokens, legacyAllowed = true) ?: return null
        if (args.legacy && args.values.drop(1).any { (it as Token.Number).unit != "%" }) return null

        val h = hue(args.values[0]) ?: return null
        val s = numberOrPercent(args.values[1], 100.0) ?: return null
        val l = numberOrPercent(args.values[2], 100.0) ?: return null
        val a = alpha(args.alpha) ?: return null
        val (r, g, b) = hslToRgb(h, s.coerceIn(0.0, 100.0) / 100, l.coerceIn(0.0, 100.0) / 100)
        return argb(a, r, g, b)
    }

    private fun hwb(tokens: List<Token>): Int? {
        val args = split(tokens, legacyAllowed = false) ?: return null
        val h = hue(args.values[0]) ?: return null
        val w = (numberOrPercent(args.values[1], 100.0) ?: return null).coerceIn(0.0, 100.0) / 100
        val bl = (numberOrPercent(args.values[2], 100.0) ?: return null).coerceIn(0.0, 100.0) / 100
        val a = alpha(args.alpha) ?: return null

        if (w + bl >= 1) {
            val gray = w / (w + bl)
            return argb(a, gray, gray, gray)
        }
        val (r, g, b) = hslToRgb(h, 1.0, 0.5).map { it * (1 - w - bl) + w }
        return argb(a, r, g, b)
    }

    private fun lab(tokens: List<Token>): Int? {
        val args = split(tokens, legacyAllowed = false) ?: return null
        val l = (numberOrPercent(args.values[0], 100.0) ?: return null).coerceIn(0.0, 100.0)
        val a = numberOrPercent(args.values[1], 125.0) ?: return null
        val b = numberOrPercent(args.values[2], 125.0) ?: return null
        val alpha = alpha(args.alpha) ?: return null
        val (r, g, bl) = labToSrgb(l, a, b)
        return argb(alpha, r, g, bl)
    }

    private fun lch(tokens: List<Token>): Int? {
        val args = split(tokens, legacyAllowed = false) ?: return null
        val l = (numberOrPercent(args.values[0], 100.0) ?: return null).coerceIn(0.0, 100.0)
        val c = (numberOrPercent(args.values[1], 150.0) ?: return null).coerceAtLeast(0.0)
        val h = (hue(args.values[2]) ?: return null) * PI / 180
        val alpha = alpha(args.alpha) ?: return null
        val (r, g, b) = labToSrgb(l, c * cos(h), c * sin(h))
        return argb(alpha, r, g, b)
    }

    private fun oklab(tokens: List<Token>): Int? {
        val args = split(tokens, legacyAllowed = false) ?: return null
        val l = (numberOrPercent(args.values[0], 1.0) ?: return null).coerceIn(0.0, 1.0)
        val a = numberOrPercent(args.values[1], 0.4) ?: return null
        val b = numberOrPercent(args.values[2], 0.4) ?: return null
        val alpha = alpha(args.alpha) ?: return null
        val (r, g, bl) = oklabToSrgb(l, a, b)
        return argb(alpha, r, g, bl)
    }

    private fun oklch(tokens: List<Token>): Int? {
        val args = split(tokens, legacyAllowed = false) ?: return null
        val l = (numberOrPercent(args.values[0], 1.0) ?: return null).coerceIn(0.0, 1.0)
        val c = (numberOrPercent(args.values[1], 0.4) ?: return null).coerceAtLeast(0.0)
        val h = (hue(args.values[2]) ?: return null) * PI / 180
        val alpha = alpha(args.alpha) ?: return null
        val (r, g, b) = oklabToSrgb(l, c * cos(h), c * sin(h))
        return argb(alpha, r, g, b)
    }

    // ----- Conversions (CSS Color 4's sample code) -----

    /** sRGB 0..1 from a hue in degrees and saturation and lightness 0..1. */
    private fun hslToRgb(hue: Double, s: Double, l: Double): List<Double> {
        val h = ((hue % 360) + 360) % 360
        fun f(n: Int): Double {
            val k = (n + h / 30) % 12
            val a = s * min(l, 1 - l)
            return l - a * max(-1.0, min(min(k - 3, 9 - k), 1.0))
        }
        return listOf(f(0), f(8), f(4))
    }

    private fun labToSrgb(l: Double, a: Double, b: Double): List<Double> {
        val kappa = 24389.0 / 27
        val epsilon = 216.0 / 24389
        val f1 = (l + 16) / 116
        val f0 = a / 500 + f1
        val f2 = f1 - b / 200
        val x = if (f0.pow(3) > epsilon) f0.pow(3) else (116 * f0 - 16) / kappa
        val y = if (l > kappa * epsilon) ((l + 16) / 116).pow(3) else l / kappa
        val z = if (f2.pow(3) > epsilon) f2.pow(3) else (116 * f2 - 16) / kappa

        val d50 = doubleArrayOf(x * 0.3457 / 0.3585, y, z * (1.0 - 0.3457 - 0.3585) / 0.3585)
        val d65 = multiply(D50_TO_D65, d50).toDoubleArray()
        return multiply(XYZ_D65_TO_LINEAR_SRGB, d65).map(::gammaEncode)
    }

    private fun oklabToSrgb(l: Double, a: Double, b: Double): List<Double> {
        val lms = doubleArrayOf(
            (l + 0.3963377774 * a + 0.2158037573 * b).pow(3),
            (l - 0.1055613458 * a - 0.0638541728 * b).pow(3),
            (l - 0.0894841775 * a - 1.2914855480 * b).pow(3),
        )
        return multiply(LMS_TO_LINEAR_SRGB, lms).map(::gammaEncode)
    }

    private fun gammaEncode(c: Double): Double {
        val sign = if (c < 0) -1 else 1
        val v = abs(c)
        return sign * if (v <= 0.0031308) 12.92 * v else 1.055 * v.pow(1 / 2.4) - 0.055
    }

    private fun multiply(m: Array<DoubleArray>, v: DoubleArray): List<Double> =
        m.map { row -> row[0] * v[0] + row[1] * v[1] + row[2] * v[2] }

    /** ARGB from alpha and sRGB channels 0..1, clipped to the gamut and rounded to 8 bits. */
    private fun argb(alpha: Double, r: Double, g: Double, b: Double): Int {
        fun byte(v: Double) = floor(v.coerceIn(0.0, 1.0) * 255 + 0.5).toInt()
        return (byte(alpha) shl 24) or (byte(r) shl 16) or (byte(g) shl 8) or byte(b)
    }

    /** CSS keywords, functions and units are case-insensitive in ASCII only. */
    private fun String.asciiLowercase() = String(CharArray(length) { i -> this[i].let { if (it in 'A'..'Z') it + 32 else it } })

    private fun isCssSpace(c: Char) =c == ' ' || c == '\t' || c == '\n' || c == '\r' || c == '\u000C'

    private val COMMENT = Regex("""/\*.*?(\*/|$)""", RegexOption.DOT_MATCHES_ALL)
    private val IMPORTANT = Regex("""![ \t\n\r\u000C]*important$""", RegexOption.IGNORE_CASE)
    private val IDENT = Regex("""-?[a-z_][a-z0-9_-]*""")
    private val NUMBER = Regex("""[+-]?(\d*\.\d+|\d+)([eE][+-]?\d+)?""")
    private val UNIT = Regex("""-?[a-zA-Z_][a-zA-Z0-9_-]*""")

    private val D50_TO_D65 = arrayOf(
        doubleArrayOf(0.955473421488075, -0.02309845494876471, 0.06325924320057072),
        doubleArrayOf(-0.0283697093338637, 1.0099953980813041, 0.021041441191917323),
        doubleArrayOf(0.012314014864481998, -0.020507649298898964, 1.330365926242124),
    )

    private val XYZ_D65_TO_LINEAR_SRGB = arrayOf(
        doubleArrayOf(3.2409699419045226, -1.537383177570094, -0.4986107602930034),
        doubleArrayOf(-0.9692436362808796, 1.8759675015077202, 0.04155505740717559),
        doubleArrayOf(0.05563007969699366, -0.20397695888897652, 1.0569715142428786),
    )

    private val LMS_TO_LINEAR_SRGB = arrayOf(
        doubleArrayOf(4.0767416621, -3.3077115913, 0.2309699292),
        doubleArrayOf(-1.2684380046, 2.6097574011, -0.3413193965),
        doubleArrayOf(-0.0041960863, -0.7034186147, 1.7076147010),
    )

    private val UNSUPPORTED_FUNCTIONS = setOf("color", "color-mix", "light-dark", "device-cmyk", "contrast-color")

    private val CSS_WIDE_KEYWORDS = setOf("inherit", "initial", "unset", "revert", "revert-layer")

    private val SYSTEM_COLORS = setOf(
        "accentcolor", "accentcolortext", "activetext", "buttonborder", "buttonface", "buttontext", "canvas", "canvastext",
        "field", "fieldtext", "graytext", "highlight", "highlighttext", "linktext", "mark", "marktext", "selecteditem",
        "selecteditemtext", "visitedtext",
        // Deprecated, still accepted.
        "activeborder", "activecaption", "appworkspace", "background", "buttonhighlight", "buttonshadow", "captiontext",
        "inactiveborder", "inactivecaption", "inactivecaptiontext", "infobackground", "infotext", "menu", "menutext",
        "scrollbar", "threeddarkshadow", "threedface", "threedhighlight", "threedlightshadow", "threedshadow", "window",
        "windowframe", "windowtext",
    )

    /** CSS's named colors, as RGB. */
    private val NAMED: Map<String, Int> = mapOf(
        "aliceblue" to 0xF0F8FF, "antiquewhite" to 0xFAEBD7, "aqua" to 0x00FFFF, "aquamarine" to 0x7FFFD4,
        "azure" to 0xF0FFFF, "beige" to 0xF5F5DC, "bisque" to 0xFFE4C4, "black" to 0x000000,
        "blanchedalmond" to 0xFFEBCD, "blue" to 0x0000FF, "blueviolet" to 0x8A2BE2, "brown" to 0xA52A2A,
        "burlywood" to 0xDEB887, "cadetblue" to 0x5F9EA0, "chartreuse" to 0x7FFF00, "chocolate" to 0xD2691E,
        "coral" to 0xFF7F50, "cornflowerblue" to 0x6495ED, "cornsilk" to 0xFFF8DC, "crimson" to 0xDC143C,
        "cyan" to 0x00FFFF, "darkblue" to 0x00008B, "darkcyan" to 0x008B8B, "darkgoldenrod" to 0xB8860B,
        "darkgray" to 0xA9A9A9, "darkgreen" to 0x006400, "darkgrey" to 0xA9A9A9, "darkkhaki" to 0xBDB76B,
        "darkmagenta" to 0x8B008B, "darkolivegreen" to 0x556B2F, "darkorange" to 0xFF8C00, "darkorchid" to 0x9932CC,
        "darkred" to 0x8B0000, "darksalmon" to 0xE9967A, "darkseagreen" to 0x8FBC8F, "darkslateblue" to 0x483D8B,
        "darkslategray" to 0x2F4F4F, "darkslategrey" to 0x2F4F4F, "darkturquoise" to 0x00CED1, "darkviolet" to 0x9400D3,
        "deeppink" to 0xFF1493, "deepskyblue" to 0x00BFFF, "dimgray" to 0x696969, "dimgrey" to 0x696969,
        "dodgerblue" to 0x1E90FF, "firebrick" to 0xB22222, "floralwhite" to 0xFFFAF0, "forestgreen" to 0x228B22,
        "fuchsia" to 0xFF00FF, "gainsboro" to 0xDCDCDC, "ghostwhite" to 0xF8F8FF, "gold" to 0xFFD700,
        "goldenrod" to 0xDAA520, "gray" to 0x808080, "green" to 0x008000, "greenyellow" to 0xADFF2F,
        "grey" to 0x808080, "honeydew" to 0xF0FFF0, "hotpink" to 0xFF69B4, "indianred" to 0xCD5C5C,
        "indigo" to 0x4B0082, "ivory" to 0xFFFFF0, "khaki" to 0xF0E68C, "lavender" to 0xE6E6FA,
        "lavenderblush" to 0xFFF0F5, "lawngreen" to 0x7CFC00, "lemonchiffon" to 0xFFFACD, "lightblue" to 0xADD8E6,
        "lightcoral" to 0xF08080, "lightcyan" to 0xE0FFFF, "lightgoldenrodyellow" to 0xFAFAD2, "lightgray" to 0xD3D3D3,
        "lightgreen" to 0x90EE90, "lightgrey" to 0xD3D3D3, "lightpink" to 0xFFB6C1, "lightsalmon" to 0xFFA07A,
        "lightseagreen" to 0x20B2AA, "lightskyblue" to 0x87CEFA, "lightslategray" to 0x778899, "lightslategrey" to 0x778899,
        "lightsteelblue" to 0xB0C4DE, "lightyellow" to 0xFFFFE0, "lime" to 0x00FF00, "limegreen" to 0x32CD32,
        "linen" to 0xFAF0E6, "magenta" to 0xFF00FF, "maroon" to 0x800000, "mediumaquamarine" to 0x66CDAA,
        "mediumblue" to 0x0000CD, "mediumorchid" to 0xBA55D3, "mediumpurple" to 0x9370DB, "mediumseagreen" to 0x3CB371,
        "mediumslateblue" to 0x7B68EE, "mediumspringgreen" to 0x00FA9A, "mediumturquoise" to 0x48D1CC, "mediumvioletred" to 0xC71585,
        "midnightblue" to 0x191970, "mintcream" to 0xF5FFFA, "mistyrose" to 0xFFE4E1, "moccasin" to 0xFFE4B5,
        "navajowhite" to 0xFFDEAD, "navy" to 0x000080, "oldlace" to 0xFDF5E6, "olive" to 0x808000,
        "olivedrab" to 0x6B8E23, "orange" to 0xFFA500, "orangered" to 0xFF4500, "orchid" to 0xDA70D6,
        "palegoldenrod" to 0xEEE8AA, "palegreen" to 0x98FB98, "paleturquoise" to 0xAFEEEE, "palevioletred" to 0xDB7093,
        "papayawhip" to 0xFFEFD5, "peachpuff" to 0xFFDAB9, "peru" to 0xCD853F, "pink" to 0xFFC0CB,
        "plum" to 0xDDA0DD, "powderblue" to 0xB0E0E6, "purple" to 0x800080, "rebeccapurple" to 0x663399,
        "red" to 0xFF0000, "rosybrown" to 0xBC8F8F, "royalblue" to 0x4169E1, "saddlebrown" to 0x8B4513,
        "salmon" to 0xFA8072, "sandybrown" to 0xF4A460, "seagreen" to 0x2E8B57, "seashell" to 0xFFF5EE,
        "sienna" to 0xA0522D, "silver" to 0xC0C0C0, "skyblue" to 0x87CEEB, "slateblue" to 0x6A5ACD,
        "slategray" to 0x708090, "slategrey" to 0x708090, "snow" to 0xFFFAFA, "springgreen" to 0x00FF7F,
        "steelblue" to 0x4682B4, "tan" to 0xD2B48C, "teal" to 0x008080, "thistle" to 0xD8BFD8,
        "tomato" to 0xFF6347, "turquoise" to 0x40E0D0, "violet" to 0xEE82EE, "wheat" to 0xF5DEB3,
        "white" to 0xFFFFFF, "whitesmoke" to 0xF5F5F5, "yellow" to 0xFFFF00, "yellowgreen" to 0x9ACD32,
    )
}
