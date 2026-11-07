package com.mrjackspade.deaddit.core.util

import java.time.Duration
import java.time.Instant

private const val TICKS_PER_SECOND = 10_000_000L
private const val TICKS_PER_MINUTE = TICKS_PER_SECOND * 60
private const val TICKS_PER_HOUR = TICKS_PER_MINUTE * 60
private const val TICKS_PER_DAY = TICKS_PER_HOUR * 24

/** How long ago this was, as "3 hours ago"; null when there is no time. */
@JvmName("elapsedOrNull")
fun Instant?.elapsed(): String? = this?.elapsed()

/** How long ago this was, as "3 hours ago". */
fun Instant.elapsed(): String = elapsed(Instant.now())

/**
 * How long before [now] this was. Each unit is the whole number of that unit, truncated toward zero, computed as
 * .NET's TimeSpan.TotalDays/TotalHours/... did: 100ns ticks divided as doubles. Years are 365 days and months 30.
 * A time in the future reads "just now".
 */
internal fun Instant.elapsed(now: Instant): String {
    val difference = Duration.between(this, now)

    // TimeSpan ticks. Duration's seconds are floored and its nanos non-negative, so this is exact for any
    // tick-aligned value; it throws on overflow, where .NET could not have represented the dates at all.
    val ticks = Math.addExact(Math.multiplyExact(difference.seconds, TICKS_PER_SECOND), (difference.nano / 100).toLong())

    val totalDays = ticks.toDouble() / TICKS_PER_DAY

    val years = (totalDays / 365).toInt()
    if (years > 1) {
        return "$years years ago"
    }

    if (years > 0) {
        return "$years year ago"
    }

    val months = (totalDays / 30).toInt()
    if (months > 1) {
        return "$months months ago"
    }

    if (months > 0) {
        return "$months month ago"
    }

    val days = totalDays.toInt()
    if (days > 1) {
        return "$days days ago"
    }

    if (days > 0) {
        return "$days day ago"
    }

    val hours = (ticks.toDouble() / TICKS_PER_HOUR).toInt()
    if (hours > 1) {
        return "$hours hours ago"
    }

    if (hours > 0) {
        return "$hours hour ago"
    }

    val minutes = (ticks.toDouble() / TICKS_PER_MINUTE).toInt()
    if (minutes > 1) {
        return "$minutes minutes ago"
    }

    if (minutes > 0) {
        return "$minutes minute ago"
    }

    val seconds = (ticks.toDouble() / TICKS_PER_SECOND).toInt()
    if (seconds > 1) {
        return "$seconds seconds ago"
    }

    if (seconds > 0) {
        return "$seconds second ago"
    }

    return "just now"
}
