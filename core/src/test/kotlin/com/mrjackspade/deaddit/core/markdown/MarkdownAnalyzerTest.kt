package com.mrjackspade.deaddit.core.markdown

import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class MarkdownAnalyzerTest {
    @Test
    fun hasNestedBlockquote_NullOrEmpty_ReturnsFalse() {
        assertFalse(MarkdownAnalyzer.hasNestedBlockquote(null))
        assertFalse(MarkdownAnalyzer.hasNestedBlockquote(""))
    }

    @Test
    fun hasNestedBlockquote_PlainText_ReturnsFalse() {
        assertFalse(MarkdownAnalyzer.hasNestedBlockquote("just plain text\nwith another line"))
    }

    @Test
    fun hasNestedBlockquote_SingleLevelQuote_ReturnsFalse() {
        assertFalse(MarkdownAnalyzer.hasNestedBlockquote("> a single-level quote"))
    }

    @Test
    fun hasNestedBlockquote_TwoLevelQuote_ReturnsTrue() {
        assertTrue(MarkdownAnalyzer.hasNestedBlockquote("> > a nested quote"))
    }

    @Test
    fun hasNestedBlockquote_ThreeLevelQuote_ReturnsTrue() {
        assertTrue(MarkdownAnalyzer.hasNestedBlockquote("> > > deep quote"))
    }

    @Test
    fun hasNestedBlockquote_NestedQuoteOnLaterLine_ReturnsTrue() {
        val md = "Some intro paragraph.\n\n> top-level\n> > nested\n\nOutro."
        assertTrue(MarkdownAnalyzer.hasNestedBlockquote(md))
    }

    @Test
    fun containsImagePlaceholder_NullOrEmpty_ReturnsFalse() {
        assertFalse(MarkdownAnalyzer.containsImagePlaceholder(null))
        assertFalse(MarkdownAnalyzer.containsImagePlaceholder(""))
    }

    @Test
    fun containsImagePlaceholder_PlainText_ReturnsFalse() {
        assertFalse(MarkdownAnalyzer.containsImagePlaceholder("no image here"))
    }

    @Test
    fun containsImagePlaceholder_NamedImagePlaceholder_ReturnsTrue() {
        assertTrue(MarkdownAnalyzer.containsImagePlaceholder("before ![image](abc123) after"))
    }

    @Test
    fun containsImagePlaceholder_GenericMarkdownImage_ReturnsFalse() {
        // The richtext pipeline only consumes the literal "![image](key)" form
        assertFalse(MarkdownAnalyzer.containsImagePlaceholder("![cat](https://example.com/cat.jpg)"))
    }
}
