package com.mrjackspade.deaddit.core.util

import org.junit.Assert.assertEquals
import org.junit.Test

class CompactCountTest {
    @Test
    fun formatsAcrossTheRanges() {
        assertEquals("0", CompactCount.format(0))
        assertEquals("912", CompactCount.format(912))
        assertEquals("-5", CompactCount.format(-5))
        assertEquals("1k", CompactCount.format(1_000))
        assertEquals("3.1k", CompactCount.format(3_141))
        assertEquals("18.4k", CompactCount.format(18_449))
        assertEquals("99.9k", CompactCount.format(99_999))
        assertEquals("184k", CompactCount.format(184_700))
        assertEquals("999k", CompactCount.format(999_999))
        assertEquals("1.2M", CompactCount.format(1_234_567))
        assertEquals("-3.1k", CompactCount.format(-3_141))
    }
}
