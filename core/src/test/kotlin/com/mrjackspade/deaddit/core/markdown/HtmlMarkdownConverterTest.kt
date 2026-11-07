package com.mrjackspade.deaddit.core.markdown

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Test

class HtmlMarkdownConverterTest {
    // region Basic HTML to Markdown

    @Test
    fun convert_EmptyString_ReturnsEmpty() {
        assertEquals("", HtmlMarkdownConverter.convert(""))
    }

    @Test
    fun convert_Null_ReturnsEmpty() {
        assertEquals("", HtmlMarkdownConverter.convert(null))
    }

    @Test
    fun convert_PlainParagraph_ReturnsText() {
        val html = "<div class=\"md\"><p>Hello world</p></div>"
        assertEquals("Hello world", HtmlMarkdownConverter.convert(html))
    }

    @Test
    fun convert_MultipleParagraphs_SeparatedByDoubleNewline() {
        val html = "<div class=\"md\"><p>First</p><p>Second</p><p>Third</p></div>"
        assertEquals("First\n\nSecond\n\nThird", HtmlMarkdownConverter.convert(html))
    }

    @Test
    fun convert_LineBreak_SingleNewline() {
        val html = "<div class=\"md\"><p>line one<br/>line two</p></div>"
        assertEquals("line one\nline two", HtmlMarkdownConverter.convert(html))
    }

    // endregion

    // region Inline Formatting

    @Test
    fun convert_Bold_ReturnsBoldMarkdown() {
        val html = "<div class=\"md\"><p><strong>bold text</strong></p></div>"
        assertEquals("**bold text**", HtmlMarkdownConverter.convert(html))
    }

    @Test
    fun convert_Italic_ReturnsItalicMarkdown() {
        val html = "<div class=\"md\"><p><em>italic text</em></p></div>"
        assertEquals("*italic text*", HtmlMarkdownConverter.convert(html))
    }

    @Test
    fun convert_BoldItalic_ReturnsBoldItalicMarkdown() {
        val html = "<div class=\"md\"><p><strong><em>bolditalic</em></strong></p></div>"
        assertEquals("***bolditalic***", HtmlMarkdownConverter.convert(html))
    }

    @Test
    fun convert_Strikethrough_ReturnsStrikethroughMarkdown() {
        val html = "<div class=\"md\"><p><del>struck</del></p></div>"
        assertEquals("~~struck~~", HtmlMarkdownConverter.convert(html))
    }

    @Test
    fun convert_InlineCode_ReturnsCodeMarkdown() {
        val html = "<div class=\"md\"><p><code>code</code></p></div>"
        assertEquals("`code`", HtmlMarkdownConverter.convert(html))
    }

    @Test
    fun convert_Superscript_ReturnsSuperscriptMarkdown() {
        val html = "<div class=\"md\"><p>text <sup>super</sup> more</p></div>"
        assertEquals("text ^(super) more", HtmlMarkdownConverter.convert(html))
    }

    @Test
    fun convert_Spoiler_ReturnsSpoilerMarkdown() {
        val html = "<div class=\"md\"><p><span class=\"md-spoiler-text\">spoiler text</span></p></div>"
        assertEquals(">!spoiler text!<", HtmlMarkdownConverter.convert(html))
    }

    @Test
    fun convert_MixedFormatting_ReturnsCorrectMarkdown() {
        val html = "<div class=\"md\"><p><strong>bold</strong> and <em>italic</em></p></div>"
        assertEquals("**bold** and *italic*", HtmlMarkdownConverter.convert(html))
    }

    // endregion

    // region Links

    @Test
    fun convert_Link_ReturnsMarkdownLink() {
        val html = "<div class=\"md\"><p><a href=\"https://example.com\">click here</a></p></div>"
        assertEquals("[click here](https://example.com)", HtmlMarkdownConverter.convert(html))
    }

    @Test
    fun convert_BareUrl_ReturnsUrlWithoutBrackets() {
        val html = "<div class=\"md\"><p><a href=\"https://example.com\">https://example.com</a></p></div>"
        assertEquals("https://example.com", HtmlMarkdownConverter.convert(html))
    }

    @Test
    fun convert_LinkInText_ReturnsCorrectMarkdown() {
        val html = "<div class=\"md\"><p>Check out <a href=\"https://example.com\">this site</a> for info</p></div>"
        assertEquals("Check out [this site](https://example.com) for info", HtmlMarkdownConverter.convert(html))
    }

    // endregion

    // region Code Blocks

    @Test
    fun convert_CodeBlock_ReturnsFencedCodeBlock() {
        val html = "<div class=\"md\"><pre><code>some code</code></pre></div>"
        assertEquals("```\nsome code\n```", HtmlMarkdownConverter.convert(html))
    }

    @Test
    fun convert_MultiLineCodeBlock_PreservesNewlines() {
        val html = "<div class=\"md\"><pre><code>line 1\nline 2\nline 3</code></pre></div>"
        assertEquals("```\nline 1\nline 2\nline 3\n```", HtmlMarkdownConverter.convert(html))
    }

    @Test
    fun convert_CodeBlockBetweenParagraphs_CorrectStructure() {
        val html = "<div class=\"md\"><p>before</p><pre><code>code</code></pre><p>after</p></div>"
        assertEquals("before\n\n```\ncode\n```\n\nafter", HtmlMarkdownConverter.convert(html))
    }

    // endregion

    // region Blockquotes

    @Test
    fun convert_Blockquote_ReturnsQuotedMarkdown() {
        val html = "<div class=\"md\"><blockquote><p>quoted text</p></blockquote></div>"
        assertEquals("> quoted text", HtmlMarkdownConverter.convert(html))
    }

    @Test
    fun convert_BlockquoteBetweenParagraphs_CorrectStructure() {
        val html = "<div class=\"md\"><p>before</p><blockquote><p>quoted</p></blockquote><p>after</p></div>"
        assertEquals("before\n\n> quoted\n\nafter", HtmlMarkdownConverter.convert(html))
    }

    @Test
    fun convert_NestedBlockquote_ReturnsNestedQuotes() {
        val html = "<div class=\"md\"><blockquote><blockquote><p>nested quote</p></blockquote></blockquote></div>"
        assertEquals("> > nested quote", HtmlMarkdownConverter.convert(html))
    }

    @Test
    fun convert_NestedBlockquoteThenSiblingParagraph_BumpsSeparatorAndShallowSibling() {
        // Reddit's markdown parser drops deeper-quoted content when the next paragraph
        // sits two or more quote levels shallower across an empty-quote separator —
        // the separator becomes a blank line after one strip and splits the shallow
        // paragraph out of the inner blockquote. Bumping the separator and the
        // immediately-following shallow paragraph up to (deepDepth - 1) keeps the
        // inner blockquote intact through Reddit's round-trip.
        val html =
            "<div class=\"md\"><blockquote>" +
                "<blockquote><blockquote><p>deep</p></blockquote></blockquote>" +
                "<p>shallow</p>" +
                "</blockquote></div>"
        assertEquals("> > > deep\n> > \n> > shallow", HtmlMarkdownConverter.convert(html))
    }

    @Test
    fun convert_BlockquoteFromRedditHtml_NoExtraNewlinesOrEmptyQuoteLine() {
        // Actual Reddit body_html structure: whitespace text nodes between block elements
        // must not produce extra blank lines or empty "> " lines
        val html =
            "<div class=\"md\">" +
                "<p>before</p>\n\n" +
                "<blockquote>\n" +
                "<p>quoted</p>\n" +
                "</blockquote>\n\n" +
                "<p>after</p>\n" +
                "</div>"
        assertEquals("before\n\n> quoted\n\nafter", HtmlMarkdownConverter.convert(html))
    }

    // endregion

    // region Tables

    @Test
    fun convert_SimpleTable_ReturnsMarkdownTable() {
        val html = "<div class=\"md\"><table><thead><tr><th>A</th><th>B</th></tr></thead><tbody><tr><td>1</td><td>2</td></tr></tbody></table></div>"
        val expected = "| A | B |\n| --- | --- |\n| 1 | 2 |"
        assertEquals(expected, HtmlMarkdownConverter.convert(html))
    }

    // endregion

    // region Lists

    @Test
    fun convert_UnorderedList_ReturnsBulletList() {
        val html = "<div class=\"md\"><ul><li>one</li><li>two</li><li>three</li></ul></div>"
        assertEquals("- one\n- two\n- three", HtmlMarkdownConverter.convert(html))
    }

    @Test
    fun convert_OrderedList_ReturnsNumberedList() {
        val html = "<div class=\"md\"><ol><li>one</li><li>two</li><li>three</li></ol></div>"
        assertEquals("1. one\n2. two\n3. three", HtmlMarkdownConverter.convert(html))
    }

    // endregion

    // region Headings

    @Test
    fun convert_H1_ReturnsHeadingMarkdown() {
        val html = "<div class=\"md\"><h1>Title</h1></div>"
        assertEquals("# Title", HtmlMarkdownConverter.convert(html))
    }

    @Test
    fun convert_H3_ReturnsHeadingMarkdown() {
        val html = "<div class=\"md\"><h3>Heading</h3></div>"
        assertEquals("### Heading", HtmlMarkdownConverter.convert(html))
    }

    // endregion

    // region HTML Entity Decoding

    @Test
    fun convert_HtmlEntities_DecodedCorrectly() {
        val html = "<div class=\"md\"><p>1 &amp; 2 &lt; 3</p></div>"
        assertEquals("1 & 2 < 3", HtmlMarkdownConverter.convert(html))
    }

    // endregion

    // region Brackets (the original bug)

    @Test
    fun convert_BracketsInText_NoEscaping() {
        // Reddit HTML doesn't escape brackets in plain text
        val html = "<div class=\"md\"><p>array[0] and array[1]</p></div>"
        val markdown = HtmlMarkdownConverter.convert(html)
        assertFalse("Should not contain escaped brackets", markdown.contains("\\["))
        assertFalse("Should not contain escaped brackets", markdown.contains("\\]"))
        assertEquals("array[0] and array[1]", markdown)
    }

    // endregion
}
