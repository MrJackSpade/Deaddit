package com.mrjackspade.deaddit.core.util

/**
 * A color as Reddit sends it for flair: a CSS color, which the C# app put straight into a style
 * attribute. Supports what CSS's hex forms and `transparent` mean; anything else throws with the
 * value, so an unsupported form shows up rather than being guessed at.
 */
object CssColor {
    /** The color as ARGB. */
    fun parse(value: String): Int {
        val v = value.trim()
        if (v.equals("transparent", ignoreCase = true)) return 0

        if (v.startsWith('#')) {
            val hex = v.substring(1)
            require(hex.all { it.isDigit() || it.lowercaseChar() in 'a'..'f' }) { "'$value' isn't a CSS hex color." }

            fun channel(s: String) = (if (s.length == 1) s + s else s).toInt(16)

            val (r, g, b, a) = when (hex.length) {
                3 -> listOf(channel(hex.substring(0, 1)), channel(hex.substring(1, 2)), channel(hex.substring(2, 3)), 255)
                4 -> listOf(channel(hex.substring(0, 1)), channel(hex.substring(1, 2)), channel(hex.substring(2, 3)), channel(hex.substring(3, 4)))
                6 -> listOf(channel(hex.substring(0, 2)), channel(hex.substring(2, 4)), channel(hex.substring(4, 6)), 255)
                // CSS's 8-digit form is RRGGBBAA.
                8 -> listOf(channel(hex.substring(0, 2)), channel(hex.substring(2, 4)), channel(hex.substring(4, 6)), channel(hex.substring(6, 8)))
                else -> throw IllegalArgumentException("'$value' isn't a CSS hex color.")
            }
            return (a shl 24) or (r shl 16) or (g shl 8) or b
        }

        throw IllegalArgumentException("Unsupported flair color '$value'.")
    }
}
