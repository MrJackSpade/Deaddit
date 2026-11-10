package com.mrjackspade.deaddit.core.util

import kotlin.math.abs

/**
 * A count as the Lantern design shows scores and comment counts: 912, 3.1k, 18.4k, 184k, 1.2M.
 * One decimal below a hundred of the unit, none from there; a trailing ".0" is left off.
 */
object CompactCount {
    fun format(value: Int): String {
        val magnitude = abs(value.toLong())
        return when {
            magnitude < 1_000 -> value.toString()
            magnitude < 1_000_000 -> scaled(value, 1_000.0, "k")
            else -> scaled(value, 1_000_000.0, "M")
        }
    }

    private fun scaled(value: Int, unit: Double, suffix: String): String {
        val amount = value / unit
        val text = if (abs(amount) < 100) {
            // Rounded down, so 999,950 doesn't read as 1000.0k.
            val tenths = (amount * 10).toLong()
            if (tenths % 10 == 0L) (tenths / 10).toString() else "${tenths / 10}.${abs(tenths % 10)}"
        } else {
            amount.toLong().toString()
        }
        return text + suffix
    }
}
