package com.mrjackspade.deaddit.core.util

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test
import java.time.Duration
import java.time.Instant

class DateTimeExtensionsTests {
    private val now = Instant.parse("2026-11-06T12:00:00Z")

    private fun ago(duration: Duration) = now.minus(duration).elapsed(now)

    @Test
    fun years() {
        assertEquals("3 years ago", ago(Duration.ofDays(365 * 3 + 10)))
        assertEquals("2 years ago", ago(Duration.ofDays(730)))
        assertEquals("1 year ago", ago(Duration.ofDays(365)))
        assertEquals("1 year ago", ago(Duration.ofDays(729)))
    }

    @Test
    fun months() {
        assertEquals("12 months ago", ago(Duration.ofDays(364)))
        assertEquals("2 months ago", ago(Duration.ofDays(60)))
        assertEquals("1 month ago", ago(Duration.ofDays(30)))
        assertEquals("1 month ago", ago(Duration.ofDays(59)))
    }

    @Test
    fun days() {
        assertEquals("29 days ago", ago(Duration.ofDays(29)))
        assertEquals("2 days ago", ago(Duration.ofDays(2)))
        assertEquals("1 day ago", ago(Duration.ofDays(1)))
        assertEquals("1 day ago", ago(Duration.ofHours(47)))
    }

    @Test
    fun hours() {
        assertEquals("23 hours ago", ago(Duration.ofHours(24).minusNanos(100)))
        assertEquals("2 hours ago", ago(Duration.ofHours(2)))
        assertEquals("1 hour ago", ago(Duration.ofHours(1)))
        assertEquals("1 hour ago", ago(Duration.ofMinutes(119)))
    }

    @Test
    fun minutes() {
        assertEquals("59 minutes ago", ago(Duration.ofHours(1).minusNanos(100)))
        assertEquals("2 minutes ago", ago(Duration.ofMinutes(2)))
        assertEquals("1 minute ago", ago(Duration.ofMinutes(1)))
        assertEquals("1 minute ago", ago(Duration.ofSeconds(119)))
    }

    @Test
    fun seconds() {
        assertEquals("59 seconds ago", ago(Duration.ofMinutes(1).minusNanos(100)))
        assertEquals("2 seconds ago", ago(Duration.ofSeconds(2)))
        assertEquals("1 second ago", ago(Duration.ofSeconds(1)))
        assertEquals("1 second ago", ago(Duration.ofMillis(1999)))
    }

    @Test
    fun justNow() {
        assertEquals("just now", ago(Duration.ZERO))
        assertEquals("just now", ago(Duration.ofMillis(999)))
        // A time in the future truncates toward zero and falls through every branch.
        assertEquals("just now", ago(Duration.ofDays(-400)))
    }

    @Test
    fun nullTimeHasNoElapsed() {
        val missing: Instant? = null

        assertNull(missing.elapsed())
    }
}
