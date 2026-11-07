package com.mrjackspade.deaddit.core.text

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test

class StringExtensionsTests {
    @Test
    fun trimsMatchingChars() {
        assertEquals("text", "\"text\"".tryTrim('"'))
        assertEquals("text", "(text)".tryTrim('(', ')'))
        assertEquals("", "()".tryTrim('(', ')'))
        assertNull("(".tryTrim('(', '('))
        assertNull("(text".tryTrim('(', ')'))
        assertNull("text)".tryTrim('(', ')'))
    }

    @Test
    fun trimsMatchingStrings() {
        assertEquals("bold", "**bold**".tryTrim("**"))
        assertEquals("x", "<b>x</b>".tryTrim("<b>", "</b>"))
        assertEquals("", "****".tryTrim("**"))
        assertNull("***".tryTrim("**"))
        assertNull("**bold*".tryTrim("**"))
    }
}
