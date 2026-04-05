using Deaddit.Core.Reddit;
using Reddit.Api.Models.Json.Media;

namespace Deaddit.Tests
{
    [TestClass]
    public class HtmlMarkdownConverterTests
    {
        #region Basic HTML to Markdown

        [TestMethod]
        public void Convert_EmptyString_ReturnsEmpty()
        {
            Assert.AreEqual("", HtmlMarkdownConverter.Convert(""));
        }

        [TestMethod]
        public void Convert_Null_ReturnsEmpty()
        {
            Assert.AreEqual("", HtmlMarkdownConverter.Convert(null));
        }

        [TestMethod]
        public void Convert_PlainParagraph_ReturnsText()
        {
            string html = "<div class=\"md\"><p>Hello world</p></div>";
            Assert.AreEqual("Hello world", HtmlMarkdownConverter.Convert(html));
        }

        [TestMethod]
        public void Convert_MultipleParagraphs_SeparatedByDoubleNewline()
        {
            string html = "<div class=\"md\"><p>First</p><p>Second</p><p>Third</p></div>";
            Assert.AreEqual("First\n\nSecond\n\nThird", HtmlMarkdownConverter.Convert(html));
        }

        [TestMethod]
        public void Convert_LineBreak_SingleNewline()
        {
            string html = "<div class=\"md\"><p>line one<br/>line two</p></div>";
            Assert.AreEqual("line one\nline two", HtmlMarkdownConverter.Convert(html));
        }

        #endregion

        #region Inline Formatting

        [TestMethod]
        public void Convert_Bold_ReturnsBoldMarkdown()
        {
            string html = "<div class=\"md\"><p><strong>bold text</strong></p></div>";
            Assert.AreEqual("**bold text**", HtmlMarkdownConverter.Convert(html));
        }

        [TestMethod]
        public void Convert_Italic_ReturnsItalicMarkdown()
        {
            string html = "<div class=\"md\"><p><em>italic text</em></p></div>";
            Assert.AreEqual("*italic text*", HtmlMarkdownConverter.Convert(html));
        }

        [TestMethod]
        public void Convert_BoldItalic_ReturnsBoldItalicMarkdown()
        {
            string html = "<div class=\"md\"><p><strong><em>bolditalic</em></strong></p></div>";
            Assert.AreEqual("***bolditalic***", HtmlMarkdownConverter.Convert(html));
        }

        [TestMethod]
        public void Convert_Strikethrough_ReturnsStrikethroughMarkdown()
        {
            string html = "<div class=\"md\"><p><del>struck</del></p></div>";
            Assert.AreEqual("~~struck~~", HtmlMarkdownConverter.Convert(html));
        }

        [TestMethod]
        public void Convert_InlineCode_ReturnsCodeMarkdown()
        {
            string html = "<div class=\"md\"><p><code>code</code></p></div>";
            Assert.AreEqual("`code`", HtmlMarkdownConverter.Convert(html));
        }

        [TestMethod]
        public void Convert_Superscript_ReturnsSuperscriptMarkdown()
        {
            string html = "<div class=\"md\"><p>text <sup>super</sup> more</p></div>";
            Assert.AreEqual("text ^(super) more", HtmlMarkdownConverter.Convert(html));
        }

        [TestMethod]
        public void Convert_Spoiler_ReturnsSpoilerMarkdown()
        {
            string html = "<div class=\"md\"><p><span class=\"md-spoiler-text\">spoiler text</span></p></div>";
            Assert.AreEqual(">!spoiler text!<", HtmlMarkdownConverter.Convert(html));
        }

        [TestMethod]
        public void Convert_MixedFormatting_ReturnsCorrectMarkdown()
        {
            string html = "<div class=\"md\"><p><strong>bold</strong> and <em>italic</em></p></div>";
            Assert.AreEqual("**bold** and *italic*", HtmlMarkdownConverter.Convert(html));
        }

        #endregion

        #region Links

        [TestMethod]
        public void Convert_Link_ReturnsMarkdownLink()
        {
            string html = "<div class=\"md\"><p><a href=\"https://example.com\">click here</a></p></div>";
            Assert.AreEqual("[click here](https://example.com)", HtmlMarkdownConverter.Convert(html));
        }

        [TestMethod]
        public void Convert_BareUrl_ReturnsUrlWithoutBrackets()
        {
            string html = "<div class=\"md\"><p><a href=\"https://example.com\">https://example.com</a></p></div>";
            Assert.AreEqual("https://example.com", HtmlMarkdownConverter.Convert(html));
        }

        [TestMethod]
        public void Convert_LinkInText_ReturnsCorrectMarkdown()
        {
            string html = "<div class=\"md\"><p>Check out <a href=\"https://example.com\">this site</a> for info</p></div>";
            Assert.AreEqual("Check out [this site](https://example.com) for info", HtmlMarkdownConverter.Convert(html));
        }

        #endregion

        #region Code Blocks

        [TestMethod]
        public void Convert_CodeBlock_ReturnsFencedCodeBlock()
        {
            string html = "<div class=\"md\"><pre><code>some code</code></pre></div>";
            Assert.AreEqual("```\nsome code\n```", HtmlMarkdownConverter.Convert(html));
        }

        [TestMethod]
        public void Convert_MultiLineCodeBlock_PreservesNewlines()
        {
            string html = "<div class=\"md\"><pre><code>line 1\nline 2\nline 3</code></pre></div>";
            Assert.AreEqual("```\nline 1\nline 2\nline 3\n```", HtmlMarkdownConverter.Convert(html));
        }

        [TestMethod]
        public void Convert_CodeBlockBetweenParagraphs_CorrectStructure()
        {
            string html = "<div class=\"md\"><p>before</p><pre><code>code</code></pre><p>after</p></div>";
            Assert.AreEqual("before\n\n```\ncode\n```\n\nafter", HtmlMarkdownConverter.Convert(html));
        }

        #endregion

        #region Blockquotes

        [TestMethod]
        public void Convert_Blockquote_ReturnsQuotedMarkdown()
        {
            string html = "<div class=\"md\"><blockquote><p>quoted text</p></blockquote></div>";
            Assert.AreEqual("> quoted text", HtmlMarkdownConverter.Convert(html));
        }

        [TestMethod]
        public void Convert_BlockquoteBetweenParagraphs_CorrectStructure()
        {
            string html = "<div class=\"md\"><p>before</p><blockquote><p>quoted</p></blockquote><p>after</p></div>";
            Assert.AreEqual("before\n\n> quoted\n\nafter", HtmlMarkdownConverter.Convert(html));
        }

        [TestMethod]
        public void Convert_NestedBlockquote_ReturnsNestedQuotes()
        {
            string html = "<div class=\"md\"><blockquote><blockquote><p>nested quote</p></blockquote></blockquote></div>";
            Assert.AreEqual("> > nested quote", HtmlMarkdownConverter.Convert(html));
        }

        [TestMethod]
        public void Convert_BlockquoteFromRedditHtml_NoExtraNewlinesOrEmptyQuoteLine()
        {
            // Actual Reddit body_html structure: whitespace text nodes between block elements
            // must not produce extra blank lines or empty "> " lines
            string html =
                "<div class=\"md\">" +
                "<p>before</p>\n\n" +
                "<blockquote>\n" +
                "<p>quoted</p>\n" +
                "</blockquote>\n\n" +
                "<p>after</p>\n" +
                "</div>";
            Assert.AreEqual("before\n\n> quoted\n\nafter", HtmlMarkdownConverter.Convert(html));
        }

        #endregion

        #region Tables

        [TestMethod]
        public void Convert_SimpleTable_ReturnsMarkdownTable()
        {
            string html = "<div class=\"md\"><table><thead><tr><th>A</th><th>B</th></tr></thead><tbody><tr><td>1</td><td>2</td></tr></tbody></table></div>";
            string expected = "| A | B |\n| --- | --- |\n| 1 | 2 |";
            Assert.AreEqual(expected, HtmlMarkdownConverter.Convert(html));
        }

        #endregion

        #region Lists

        [TestMethod]
        public void Convert_UnorderedList_ReturnsBulletList()
        {
            string html = "<div class=\"md\"><ul><li>one</li><li>two</li><li>three</li></ul></div>";
            Assert.AreEqual("- one\n- two\n- three", HtmlMarkdownConverter.Convert(html));
        }

        [TestMethod]
        public void Convert_OrderedList_ReturnsNumberedList()
        {
            string html = "<div class=\"md\"><ol><li>one</li><li>two</li><li>three</li></ol></div>";
            Assert.AreEqual("1. one\n2. two\n3. three", HtmlMarkdownConverter.Convert(html));
        }

        #endregion

        #region Headings

        [TestMethod]
        public void Convert_H1_ReturnsHeadingMarkdown()
        {
            string html = "<div class=\"md\"><h1>Title</h1></div>";
            Assert.AreEqual("# Title", HtmlMarkdownConverter.Convert(html));
        }

        [TestMethod]
        public void Convert_H3_ReturnsHeadingMarkdown()
        {
            string html = "<div class=\"md\"><h3>Heading</h3></div>";
            Assert.AreEqual("### Heading", HtmlMarkdownConverter.Convert(html));
        }

        #endregion

        #region HTML Entity Decoding

        [TestMethod]
        public void Convert_HtmlEntities_DecodedCorrectly()
        {
            string html = "<div class=\"md\"><p>1 &amp; 2 &lt; 3</p></div>";
            Assert.AreEqual("1 & 2 < 3", HtmlMarkdownConverter.Convert(html));
        }

        #endregion

        #region Brackets (the original bug)

        [TestMethod]
        public void Convert_BracketsInText_NoEscaping()
        {
            // Reddit HTML doesn't escape brackets in plain text
            string html = "<div class=\"md\"><p>array[0] and array[1]</p></div>";
            string markdown = HtmlMarkdownConverter.Convert(html);
            Assert.IsFalse(markdown.Contains("\\["), "Should not contain escaped brackets");
            Assert.IsFalse(markdown.Contains("\\]"), "Should not contain escaped brackets");
            Assert.AreEqual("array[0] and array[1]", markdown);
        }

        #endregion
    }
}
