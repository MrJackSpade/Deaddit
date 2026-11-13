package com.mrjackspade.deaddit.core.util

import java.time.Duration
import java.time.Instant

private const val TICKS_PER_SECOND = 10_000_000L
private const val TICKS_PER_MINUTE = TICKS_PER_SECOND * 60
private const val TICKS_PER_HOUR = TICKS_PER_MINUTE * 60
private const val TICKS_PER_DAY = TICKS_PER_HOUR * 24

/** How long ago this was, as "3h"; null when there is no time. */
@JvmName("elapsedOrNull")
fun Instant?.elapsed(): String? = this?.elapsed()

/** How long ago this was, as "3h". */
fun Instant.elapsed(): String = elapsed(Instant.now())

/**
 * How long before [now] this was, in its largest whole unit: "2y", "11mo", "3d", "4h", "5m", "6s", or "now". Each
 * unit is truncated toward zero, computed as .NET's TimeSpan.TotalDays/TotalHours/... did: 100ns ticks divided as
 * doubles. Years are 365 days and months 30. A time in the future reads "now".
 */
internal fun Instant.elapsed(now: Instant): String {
    val difference = Duration.between(this, now)

    // TimeSpan ticks. Duration's seconds are floored and its nanos non-negative, so this is exact for any
    // tick-aligned value; it throws on overflow, where .NET could not have represented the dates at all.
    val ticks = Math.addExact(Math.multiplyExact(difference.seconds, TICKS_PER_SECOND), (difference.nano / 100).toLong())

    val totalDays = ticks.toDouble() / TICKS_PER_DAY

    val years = (totalDays / 365).toInt()
    if (years > 0) {
        return "${years}y"
    }

    val months = (totalDays / 30).toInt()
    if (months > 0) {
        return "${months}mo"
    }

    val days = totalDays.toInt()
    if (days > 0) {
        return "${days}d"
    }

    val hours = (ticks.toDouble() / TICKS_PER_HOUR).toInt()
    if (hours > 0) {
        return "${hours}h"
    }

    val minutes = (ticks.toDouble() / TICKS_PER_MINUTE).toInt()
    if (minutes > 0) {
        return "${minutes}m"
    }

    val seconds = (ticks.toDouble() / TICKS_PER_SECOND).toInt()
    if (seconds > 0) {
        return "${seconds}s"
    }

    return "now"
}
