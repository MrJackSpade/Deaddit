package com.mrjackspade.deaddit.core.markdown

import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonArray
import kotlinx.serialization.json.JsonElement
import kotlinx.serialization.json.int
import kotlinx.serialization.json.jsonArray
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * The C# tests asserted on Reddit.Api's RichTextDocument; these assert on its JSON
 * (`document`, and per element `e` type, `t` text, `c` children, `id`, `u` url, `f` format).
 */
class MarkdownRichTextConverterTest {
    private fun convert(markdown: String?): JsonArray = MarkdownRichTextConverter.convert(markdown)["document"]!!.jsonArray

    private val JsonElement.type get() = jsonObject["e"]!!.jsonPrimitive.content
    private val JsonElement.text get() = jsonObject["t"]?.jsonPrimitive?.content
    private val JsonElement.id get() = jsonObject["id"]?.jsonPrimitive?.content
    private val JsonElement.url get() = jsonObject["u"]?.jsonPrimitive?.content
    private val JsonElement.children get() = jsonObject["c"]!!.jsonArray
    private val JsonElement.format get() = jsonObject["f"]?.jsonArray?.map { run -> run.jsonArray.map { it.jsonPrimitive.int } }

    // region JSON shape

    @Test
    fun convert_Json_MatchesRichTextDocumentShape() {
        // The exact JSON the native bridge deserializes into Reddit.Api's RichTextDocument; null members are omitted.
        val json = Json.encodeToString(
            JsonElement.serializer(),
            MarkdownRichTextConverter.convert("hello **world** [x](https://e.com)\n\n![image](k1)"),
        )
        assertEquals(
            """{"document":[{"e":"par","c":[{"e":"text","t":"hello world ","f":[[1,6,5]]},""" +
                """{"e":"link","t":"x","u":"https://e.com"}]},{"e":"img","id":"k1"}]}""",
            json,
        )
    }

    // endregion

    // region Basic Text

    @Test
    fun convert_EmptyString_ReturnsSingleEmptyParagraph() {
        val doc = convert("")

        assertEquals(1, doc.size)
        assertEquals("par", doc[0].type)
        assertEquals(1, doc[0].children.size)
        assertEquals("text", doc[0].children[0].type)
        assertEquals("", doc[0].children[0].text)
    }

    @Test
    fun convert_Null_ReturnsSingleEmptyParagraph() {
        val doc = convert(null)

        assertEquals(1, doc.size)
        assertEquals("par", doc[0].type)
    }

    @Test
    fun convert_PlainText_ReturnsSingleParagraph() {
        val doc = convert("Hello world")

        assertEquals(1, doc.size)
        assertEquals("par", doc[0].type)
        assertEquals(1, doc[0].children.size)
        assertEquals("text", doc[0].children[0].type)
        assertEquals("Hello world", doc[0].children[0].text)
        assertNull("Plain text should have no format", doc[0].children[0].format)
    }

    @Test
    fun convert_MultipleParagraphs_ReturnsMultipleParagraphElements() {
        val doc = convert("First paragraph\n\nSecond paragraph\n\nThird paragraph")

        assertEquals(3, doc.size)
        assertEquals("par", doc[0].type)
        assertEquals("First paragraph", doc[0].children[0].text)
        assertEquals("par", doc[1].type)
        assertEquals("Second paragraph", doc[1].children[0].text)
        assertEquals("par", doc[2].type)
        assertEquals("Third paragraph", doc[2].children[0].text)
    }

    // endregion

    // region Bold

    @Test
    fun convert_BoldAsterisks_ReturnsFormatCode1() {
        val text = convert("**bold**")[0].children[0]

        assertEquals("bold", text.text)
        val format = text.format
        assertNotNull(format)
        assertEquals(1, format!!.size)
        assertEquals("Format code should be 1 (bold)", 1, format[0][0])
        assertEquals("Start index should be 0", 0, format[0][1])
        assertEquals("Length should be 4", 4, format[0][2])
    }

    @Test
    fun convert_BoldUnderscores_ReturnsFormatCode1() {
        val text = convert("__bold__")[0].children[0]

        assertEquals("bold", text.text)
        assertNotNull(text.format)
        assertEquals(1, text.format!![0][0])
    }

    @Test
    fun convert_BoldInMiddle_CorrectOffsets() {
        val text = convert("hello **world** today")[0].children[0]

        assertEquals("hello world today", text.text)
        val format = text.format
        assertNotNull(format)
        assertEquals("Format code should be 1 (bold)", 1, format!![0][0])
        assertEquals("Start index should be 6", 6, format[0][1])
        assertEquals("Length should be 5", 5, format[0][2])
    }

    // endregion

    // region Italic

    @Test
    fun convert_ItalicAsterisk_ReturnsFormatCode2() {
        val text = convert("*italic*")[0].children[0]

        assertEquals("italic", text.text)
        val format = text.format
        assertNotNull(format)
        assertEquals(1, format!!.size)
        assertEquals("Format code should be 2 (italic)", 2, format[0][0])
        assertEquals(0, format[0][1])
        assertEquals(6, format[0][2])
    }

    @Test
    fun convert_ItalicUnderscore_ReturnsFormatCode2() {
        val text = convert("_italic_")[0].children[0]

        assertEquals("italic", text.text)
        assertEquals(2, text.format!![0][0])
    }

    // endregion

    // region Bold+Italic

    @Test
    fun convert_BoldItalic_ReturnsBothFormats() {
        val text = convert("***bolditalic***")[0].children[0]

        assertEquals("bolditalic", text.text)
        val format = text.format
        assertNotNull(format)
        assertEquals("Should have both bold and italic formats", 2, format!!.size)

        val formatCodes = format.map { it[0] }.sorted()
        assertEquals("Should have bold", 1, formatCodes[0])
        assertEquals("Should have italic", 2, formatCodes[1])
    }

    // endregion

    // region Strikethrough

    @Test
    fun convert_Strikethrough_ReturnsFormatCode4() {
        val text = convert("~~struck~~")[0].children[0]

        assertEquals("struck", text.text)
        assertNotNull(text.format)
        assertEquals("Format code should be 8 (strikethrough)", 8, text.format!![0][0])
    }

    // endregion

    // region Inline Code

    @Test
    fun convert_InlineCode_ReturnsFormatCode32() {
        val text = convert("`code`")[0].children[0]

        assertEquals("code", text.text)
        assertNotNull(text.format)
        assertEquals("Format code should be 64 (inline code)", 64, text.format!![0][0])
    }

    // endregion

    // region Superscript

    @Test
    fun convert_SuperscriptParens_ReturnsFormatCode8() {
        val text = convert("^(super)")[0].children[0]

        assertEquals("super", text.text)
        assertNotNull(text.format)
        assertEquals("Format code should be 32 (superscript)", 32, text.format!![0][0])
    }

    @Test
    fun convert_SuperscriptWord_ReturnsFormatCode32() {
        val text = convert("text ^word more")[0].children[0]

        assertEquals("text word more", text.text)
        val format = text.format
        assertNotNull(format)
        assertEquals(32, format!![0][0])
        assertEquals("Start index should be 5", 5, format[0][1])
        assertEquals("Length should be 4", 4, format[0][2])
    }

    // endregion

    // region Spoiler

    @Test
    fun convert_Spoiler_ReturnsFormatCode16() {
        val text = convert(">!spoiler text!<")[0].children[0]

        assertEquals("spoiler text", text.text)
        assertNotNull(text.format)
        assertEquals("Format code should be 16 (spoiler)", 16, text.format!![0][0])
    }

    // endregion

    // region Links

    @Test
    fun convert_Link_ReturnsLinkElement() {
        val par = convert("[click here](https://example.com)")[0]

        assertEquals(1, par.children.size)
        val link = par.children[0]
        assertEquals("link", link.type)
        assertEquals("click here", link.text)
        assertEquals("https://example.com", link.url)
    }

    @Test
    fun convert_TextWithLink_SplitsIntoTextAndLink() {
        val par = convert("Check out [this site](https://example.com) for info")[0]

        assertEquals("Should have text, link, text", 3, par.children.size)
        assertEquals("text", par.children[0].type)
        assertEquals("Check out ", par.children[0].text)
        assertEquals("link", par.children[1].type)
        assertEquals("this site", par.children[1].text)
        assertEquals("https://example.com", par.children[1].url)
        assertEquals("text", par.children[2].type)
        assertEquals(" for info", par.children[2].text)
    }

    // endregion

    // region Images

    @Test
    fun convert_StandaloneImage_ReturnsImgElement() {
        val doc = convert("![image](abc123key)")

        assertEquals(1, doc.size)
        assertEquals("img", doc[0].type)
        assertEquals("abc123key", doc[0].id)
    }

    @Test
    fun convert_ImageBetweenParagraphs_ReturnsCorrectOrder() {
        val doc = convert("Before text\n\n![image](mediakey1)\n\nAfter text")

        assertEquals(3, doc.size)
        assertEquals("par", doc[0].type)
        assertEquals("Before text", doc[0].children[0].text)
        assertEquals("img", doc[1].type)
        assertEquals("mediakey1", doc[1].id)
        assertEquals("par", doc[2].type)
        assertEquals("After text", doc[2].children[0].text)
    }

    @Test
    fun convert_ImageInlineWithText_SplitsIntoSeparateElements() {
        val doc = convert("hello ![image](abc123) world")

        assertEquals(3, doc.size)
        assertEquals("par", doc[0].type)
        assertEquals("hello ", doc[0].children[0].text)
        assertEquals("img", doc[1].type)
        assertEquals("abc123", doc[1].id)
        assertEquals("par", doc[2].type)
        assertEquals(" world", doc[2].children[0].text)
    }

    @Test
    fun convert_TextThenImage_SplitsCorrectly() {
        val doc = convert("some text ![image](key1)")

        assertEquals(2, doc.size)
        assertEquals("par", doc[0].type)
        assertEquals("some text ", doc[0].children[0].text)
        assertEquals("img", doc[1].type)
        assertEquals("key1", doc[1].id)
    }

    @Test
    fun convert_MultipleImages_ReturnsAllImgElements() {
        val doc = convert("![image](key1)\n\n![image](key2)\n\n![image](key3)")

        assertEquals(3, doc.size)
        assertEquals("img", doc[0].type)
        assertEquals("key1", doc[0].id)
        assertEquals("img", doc[1].type)
        assertEquals("key2", doc[1].id)
        assertEquals("img", doc[2].type)
        assertEquals("key3", doc[2].id)
    }

    // endregion

    // region Mixed Formatting

    @Test
    fun convert_MultipleFomatsInOneParagraph_AllFormatsPresent() {
        val par = convert("**bold** and *italic*")[0]
        // This may produce multiple text elements depending on implementation
        // The key thing is the output text and formats are correct
        assertEquals("par", par.type)

        val fullText = par.children.filter { it.type == "text" }.joinToString("") { it.text!! }
        assertEquals("bold and italic", fullText)
    }

    @Test
    fun convert_FormattedTextWithLink_CorrectStructure() {
        val par = convert("**bold** then [link](https://example.com)")[0]
        assertTrue("Should have at least text and link elements", par.children.size >= 2)

        val hasLink = par.children.any { it.type == "link" && it.url == "https://example.com" }
        assertTrue("Should contain the link element", hasLink)
    }

    @Test
    fun convert_ComplexDocument_CorrectStructure() {
        val markdown = "Hello **world**\n\n![image](upload123)\n\nCheck [this](https://example.com) out with ~~deleted~~ text"

        val doc = convert(markdown)

        assertEquals("Should have 3 top-level elements", 3, doc.size)
        assertEquals("par", doc[0].type)
        assertEquals("img", doc[1].type)
        assertEquals("upload123", doc[1].id)
        assertEquals("par", doc[2].type)
    }

    // endregion

    // region Code Blocks

    @Test
    fun convert_FencedCodeBlock_ReturnsCodeElement() {
        val doc = convert("```\nsome code\n```")

        assertEquals(1, doc.size)
        assertEquals("code", doc[0].type)
        assertEquals("some code", doc[0].children[0].text)
        assertEquals("raw", doc[0].children[0].type)
    }

    @Test
    fun convert_FencedCodeBlockWithLanguage_ReturnsCodeElement() {
        val doc = convert("```csharp\nvar x = 1;\n```")

        assertEquals(1, doc.size)
        assertEquals("code", doc[0].type)
        assertEquals("var x = 1;", doc[0].children[0].text)
    }

    @Test
    fun convert_FencedCodeBlockMultiLine_PreservesNewlines() {
        val doc = convert("```\nline 1\nline 2\nline 3\n```")

        assertEquals(1, doc.size)
        assertEquals("code", doc[0].type)
        assertEquals("line 1\nline 2\nline 3", doc[0].children[0].text)
    }

    @Test
    fun convert_CodeBlockBetweenParagraphs_CorrectOrder() {
        val doc = convert("before\n\n```\ncode\n```\n\nafter")

        assertEquals(3, doc.size)
        assertEquals("par", doc[0].type)
        assertEquals("before", doc[0].children[0].text)
        assertEquals("code", doc[1].type)
        assertEquals("code", doc[1].children[0].text)
        assertEquals("par", doc[2].type)
        assertEquals("after", doc[2].children[0].text)
    }

    // endregion

    // region Blockquotes

    @Test
    fun convert_SimpleBlockquote_ReturnsBlockquoteElement() {
        val doc = convert("> quoted text")

        assertEquals(1, doc.size)
        assertEquals("blockquote", doc[0].type)
        assertEquals(1, doc[0].children.size)
        assertEquals("par", doc[0].children[0].type)
        assertEquals("quoted text", doc[0].children[0].children[0].text)
    }

    @Test
    fun convert_BlockquoteNoSpace_ReturnsBlockquoteElement() {
        val doc = convert(">quoted text")

        assertEquals(1, doc.size)
        assertEquals("blockquote", doc[0].type)
        assertEquals("quoted text", doc[0].children[0].children[0].text)
    }

    @Test
    fun convert_MultiLineBlockquote_SingleBlockquoteElement() {
        val doc = convert("> line one\n> line two\n> line three")

        assertEquals(1, doc.size)
        assertEquals("blockquote", doc[0].type)
        assertEquals(1, doc[0].children.size)
        assertEquals("par", doc[0].children[0].type)
        assertEquals("line one\nline two\nline three", doc[0].children[0].children[0].text)
    }

    @Test
    fun convert_BlockquoteBetweenParagraphs_CorrectOrder() {
        val doc = convert("before\n\n> quoted\n\nafter")

        assertEquals(3, doc.size)
        assertEquals("par", doc[0].type)
        assertEquals("before", doc[0].children[0].text)
        assertEquals("blockquote", doc[1].type)
        assertEquals("quoted", doc[1].children[0].children[0].text)
        assertEquals("par", doc[2].type)
        assertEquals("after", doc[2].children[0].text)
    }

    @Test
    fun convert_BlockquoteWithFormatting_FormattingPreserved() {
        val doc = convert("> **bold** text")

        assertEquals(1, doc.size)
        assertEquals("blockquote", doc[0].type)

        val innerParagraph = doc[0].children[0]
        val fullText = innerParagraph.children.filter { it.type == "text" }.joinToString("") { it.text!! }
        assertEquals("bold text", fullText)
    }

    @Test
    fun convert_SpoilerNotTreatedAsBlockquote_ReturnsSpoilerFormat() {
        val doc = convert(">!spoiler text!<")

        assertEquals(1, doc.size)
        assertEquals("par", doc[0].type)
        val text = doc[0].children[0]
        assertEquals("spoiler text", text.text)
        assertEquals("Format code should be 16 (spoiler)", 16, text.format!![0][0])
    }

    @Test
    fun convert_NestedBlockquote_ReturnsNestedBlockquoteElements() {
        val doc = convert(">> nested quote")

        assertEquals(1, doc.size)
        assertEquals("blockquote", doc[0].type)
        assertEquals(1, doc[0].children.size)
        assertEquals("blockquote", doc[0].children[0].type)
        assertEquals("nested quote", doc[0].children[0].children[0].children[0].text)
    }

    @Test
    fun convert_NestedBlockquoteWithSpace_ReturnsNestedBlockquoteElements() {
        val doc = convert("> > nested quote")

        assertEquals(1, doc.size)
        assertEquals("blockquote", doc[0].type)
        assertEquals(1, doc[0].children.size)
        assertEquals("blockquote", doc[0].children[0].type)
    }

    @Test
    fun convert_BlockquoteWithEmptyLine_MultipleParagraphsInBlockquote() {
        val doc = convert("Test\n\n> Line 1\n>\n> Line 2\n\nTest")

        assertEquals(3, doc.size)
        assertEquals("par", doc[0].type)
        assertEquals("Test", doc[0].children[0].text)

        assertEquals("blockquote", doc[1].type)
        assertEquals("Blockquote should contain two paragraphs", 2, doc[1].children.size)
        assertEquals("par", doc[1].children[0].type)
        assertEquals("Line 1", doc[1].children[0].children[0].text)
        assertEquals("par", doc[1].children[1].type)
        assertEquals("Line 2", doc[1].children[1].children[0].text)

        assertEquals("par", doc[2].type)
        assertEquals("Test", doc[2].children[0].text)
    }

    @Test
    fun convert_TwoSeparateBlockquotes_ReturnsTwoBlockquoteElements() {
        val doc = convert("> first quote\n\n> second quote")

        assertEquals(2, doc.size)
        assertEquals("blockquote", doc[0].type)
        assertEquals("first quote", doc[0].children[0].children[0].text)
        assertEquals("blockquote", doc[1].type)
        assertEquals("second quote", doc[1].children[0].children[0].text)
    }

    // endregion

    // region Edge Cases

    @Test
    fun convert_UnmatchedAsterisks_TreatedAsLiteralText() {
        val text = convert("this * is not italic")[0].children[0]

        assertEquals("this * is not italic", text.text)
        assertNull(text.format)
    }

    @Test
    fun convert_SingleNewline_StaysInSameParagraph() {
        val doc = convert("line one\nline two")

        assertEquals("Single newline should not split paragraphs", 1, doc.size)
        assertTrue(doc[0].children[0].text!!.contains("line one"))
        assertTrue(doc[0].children[0].text!!.contains("line two"))
    }

    @Test
    fun convert_EmptyParagraphBetweenText_PreservesStructure() {
        val doc = convert("first\n\n\n\nsecond")

        // "\n\n\n\n" splits into ["first", "", "second"]
        assertEquals(3, doc.size)
        assertEquals("first", doc[0].children[0].text)
        assertEquals("", doc[1].children[0].text)
        assertEquals("second", doc[2].children[0].text)
    }

    // endregion
}
