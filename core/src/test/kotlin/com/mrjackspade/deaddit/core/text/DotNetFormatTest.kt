package com.mrjackspade.deaddit.core.text

import org.junit.Assert.assertEquals
import org.junit.Assert.assertThrows
import org.junit.Test

/** Expected values are what .NET's string.Format(format, (object)arg) gives. */
class DotNetFormatTest {
    @Test
    fun replacesTheArgument() {
        assertEquals("Reply to: be kind", DotNetFormat.format("Reply to: {0}", "be kind"))
        assertEquals("a-x-x", DotNetFormat.format("a-{0}-{0}", "x"))
        assertEquals("[]", DotNetFormat.format("[{0}]", null))
        assertEquals("[ x]|[x ]", DotNetFormat.format("[{0,2}]|[{0,-2}]", "x"))
        assertEquals("x", DotNetFormat.format("{0:N2}", "x"))
        assertEquals("{x}", DotNetFormat.format("{{{0}}}", "x"))
    }

    @Test
    fun throwsWhereDotNetDoes() {
        assertThrows(FormatException::class.java) { DotNetFormat.format("{0} and {1}", "x") }
        assertThrows(FormatException::class.java) { DotNetFormat.format("{ \"json\": {0} }", "x") }
        assertThrows(FormatException::class.java) { DotNetFormat.format("a } b", "x") }
        assertThrows(FormatException::class.java) { DotNetFormat.format("{x}", "x") }
    }
}
