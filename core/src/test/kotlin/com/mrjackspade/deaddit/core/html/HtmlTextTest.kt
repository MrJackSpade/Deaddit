package com.mrjackspade.deaddit.core.html

import org.junit.Assert.assertEquals
import org.junit.Test

class HtmlTextTest {
    /** A title as /r/all served it on 2026-11-07, which the C# WebView showed as "&". */
    @Test
    fun decodesEntitiesForDisplay() {
        assertEquals("Making a list & Checking it twice", HtmlText.display("Making a list &amp; Checking it twice"))
        assertEquals("a < b > c \"d\"", HtmlText.display("a &lt; b &gt; c &quot;d&quot;"))
    }

    @Test
    fun leavesPlainTextAlone() {
        assertEquals("Q&A at 5", HtmlText.display("Q&A at 5"))
    }
}
