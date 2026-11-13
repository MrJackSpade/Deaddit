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
        assertEquals("3y", ago(Duration.ofDays(365 * 3 + 10)))
        assertEquals("2y", ago(Duration.ofDays(730)))
        assertEquals("1y", ago(Duration.ofDays(365)))
        assertEquals("1y", ago(Duration.ofDays(729)))
    }

    @Test
    fun months() {
        assertEquals("12mo", ago(Duration.ofDays(364)))
        assertEquals("2mo", ago(Duration.ofDays(60)))
        assertEquals("1mo", ago(Duration.ofDays(30)))
        assertEquals("1mo", ago(Duration.ofDays(59)))
    }

    @Test
    fun days() {
        assertEquals("29d", ago(Duration.ofDays(29)))
        assertEquals("2d", ago(Duration.ofDays(2)))
        assertEquals("1d", ago(Duration.ofDays(1)))
        assertEquals("1d", ago(Duration.ofHours(47)))
    }

    @Test
    fun hours() {
        assertEquals("23h", ago(Duration.ofHours(24).minusNanos(100)))
        assertEquals("2h", ago(Duration.ofHours(2)))
        assertEquals("1h", ago(Duration.ofHours(1)))
        assertEquals("1h", ago(Duration.ofMinutes(119)))
    }

    @Test
    fun minutes() {
        assertEquals("59m", ago(Duration.ofHours(1).minusNanos(100)))
        assertEquals("2m", ago(Duration.ofMinutes(2)))
        assertEquals("1m", ago(Duration.ofMinutes(1)))
        assertEquals("1m", ago(Duration.ofSeconds(119)))
    }

    @Test
    fun seconds() {
        assertEquals("59s", ago(Duration.ofMinutes(1).minusNanos(100)))
        assertEquals("2s", ago(Duration.ofSeconds(2)))
        assertEquals("1s", ago(Duration.ofSeconds(1)))
        assertEquals("1s", ago(Duration.ofMillis(1999)))
    }

    @Test
    fun justNow() {
        assertEquals("now", ago(Duration.ZERO))
        assertEquals("now", ago(Duration.ofMillis(999)))
        // A time in the future truncates toward zero and falls through every branch.
        assertEquals("now", ago(Duration.ofDays(-400)))
    }

    @Test
    fun nullTimeHasNoElapsed() {
        val missing: Instant? = null

        assertNull(missing.elapsed())
    }
}
