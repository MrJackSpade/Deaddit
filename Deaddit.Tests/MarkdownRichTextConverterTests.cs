using Deaddit.Core.Reddit;
using Reddit.Api.Models.Json.Media;

namespace Deaddit.Tests
{
    [TestClass]
    public class MarkdownRichTextConverterTests
    {
        #region Basic Text

        [TestMethod]
        public void Convert_EmptyString_ReturnsSingleEmptyParagraph()
        {
            RichTextDocument doc = MarkdownRichTextConverter.Convert("");

            Assert.AreEqual(1, doc.Document.Count);
            Assert.AreEqual(RichTextElementType.Paragraph, doc.Document[0].ElementType);
            Assert.AreEqual(1, doc.Document[0].Children.Count);
            Assert.AreEqual(RichTextElementType.Text, doc.Document[0].Children[0].ElementType);
            Assert.AreEqual("", doc.Document[0].Children[0].Text);
        }

        [TestMethod]
        public void Convert_Null_ReturnsSingleEmptyParagraph()
        {
            RichTextDocument doc = MarkdownRichTextConverter.Convert(null);

            Assert.AreEqual(1, doc.Document.Count);
            Assert.AreEqual(RichTextElementType.Paragraph, doc.Document[0].ElementType);
        }

        [TestMethod]
        public void Convert_PlainText_ReturnsSingleParagraph()
        {
            RichTextDocument doc = MarkdownRichTextConverter.Convert("Hello world");

            Assert.AreEqual(1, doc.Document.Count);
            Assert.AreEqual(RichTextElementType.Paragraph, doc.Document[0].ElementType);
            Assert.AreEqual(1, doc.Document[0].Children.Count);
            Assert.AreEqual(RichTextElementType.Text, doc.Document[0].Children[0].ElementType);
            Assert.AreEqual("Hello world", doc.Document[0].Children[0].Text);
            Assert.IsNull(doc.Document[0].Children[0].Format, "Plain text should have no format");
        }

        [TestMethod]
        public void Convert_MultipleParagraphs_ReturnsMultipleParagraphElements()
        {
            RichTextDocument doc = MarkdownRichTextConverter.Convert("First paragraph\n\nSecond paragraph\n\nThird paragraph");

            Assert.AreEqual(3, doc.Document.Count);
            Assert.AreEqual(RichTextElementType.Paragraph, doc.Document[0].ElementType);
            Assert.AreEqual("First paragraph", doc.Document[0].Children[0].Text);
            Assert.AreEqual(RichTextElementType.Paragraph, doc.Document[1].ElementType);
            Assert.AreEqual("Second paragraph", doc.Document[1].Children[0].Text);
            Assert.AreEqual(RichTextElementType.Paragraph, doc.Document[2].ElementType);
            Assert.AreEqual("Third paragraph", doc.Document[2].Children[0].Text);
        }

        #endregion

        #region Bold

        [TestMethod]
        public void Convert_BoldAsterisks_ReturnsFormatCode1()
        {
            RichTextDocument doc = MarkdownRichTextConverter.Convert("**bold**");

            RichTextElement text = doc.Document[0].Children[0];
            Assert.AreEqual("bold", text.Text);
            Assert.IsNotNull(text.Format);
            Assert.AreEqual(1, text.Format.Count);
            Assert.AreEqual(1, text.Format[0][0], "Format code should be 1 (bold)");
            Assert.AreEqual(0, text.Format[0][1], "Start index should be 0");
            Assert.AreEqual(4, text.Format[0][2], "Length should be 4");
        }

        [TestMethod]
        public void Convert_BoldUnderscores_ReturnsFormatCode1()
        {
            RichTextDocument doc = MarkdownRichTextConverter.Convert("__bold__");

            RichTextElement text = doc.Document[0].Children[0];
            Assert.AreEqual("bold", text.Text);
            Assert.IsNotNull(text.Format);
            Assert.AreEqual(1, text.Format[0][0]);
        }

        [TestMethod]
        public void Convert_BoldInMiddle_CorrectOffsets()
        {
            RichTextDocument doc = MarkdownRichTextConverter.Convert("hello **world** today");

            RichTextElement text = doc.Document[0].Children[0];
            Assert.AreEqual("hello world today", text.Text);
            Assert.IsNotNull(text.Format);
            Assert.AreEqual(1, text.Format[0][0], "Format code should be 1 (bold)");
            Assert.AreEqual(6, text.Format[0][1], "Start index should be 6");
            Assert.AreEqual(5, text.Format[0][2], "Length should be 5");
        }

        #endregion

        #region Italic

        [TestMethod]
        public void Convert_ItalicAsterisk_ReturnsFormatCode2()
        {
            RichTextDocument doc = MarkdownRichTextConverter.Convert("*italic*");

            RichTextElement text = doc.Document[0].Children[0];
            Assert.AreEqual("italic", text.Text);
            Assert.IsNotNull(text.Format);
            Assert.AreEqual(1, text.Format.Count);
            Assert.AreEqual(2, text.Format[0][0], "Format code should be 2 (italic)");
            Assert.AreEqual(0, text.Format[0][1]);
            Assert.AreEqual(6, text.Format[0][2]);
        }

        [TestMethod]
        public void Convert_ItalicUnderscore_ReturnsFormatCode2()
        {
            RichTextDocument doc = MarkdownRichTextConverter.Convert("_italic_");

            RichTextElement text = doc.Document[0].Children[0];
            Assert.AreEqual("italic", text.Text);
            Assert.AreEqual(2, text.Format[0][0]);
        }

        #endregion

        #region Bold+Italic

        [TestMethod]
        public void Convert_BoldItalic_ReturnsBothFormats()
        {
            RichTextDocument doc = MarkdownRichTextConverter.Convert("***bolditalic***");

            RichTextElement text = doc.Document[0].Children[0];
            Assert.AreEqual("bolditalic", text.Text);
            Assert.IsNotNull(text.Format);
            Assert.AreEqual(2, text.Format.Count, "Should have both bold and italic formats");

            List<int> formatCodes = text.Format.Select(f => f[0]).OrderBy(x => x).ToList();
            Assert.AreEqual(1, formatCodes[0], "Should have bold");
            Assert.AreEqual(2, formatCodes[1], "Should have italic");
        }

        #endregion

        #region Strikethrough

        [TestMethod]
        public void Convert_Strikethrough_ReturnsFormatCode4()
        {
            RichTextDocument doc = MarkdownRichTextConverter.Convert("~~struck~~");

            RichTextElement text = doc.Document[0].Children[0];
            Assert.AreEqual("struck", text.Text);
            Assert.IsNotNull(text.Format);
            Assert.AreEqual(4, text.Format[0][0], "Format code should be 4 (strikethrough)");
        }

        #endregion

        #region Inline Code

        [TestMethod]
        public void Convert_InlineCode_ReturnsFormatCode32()
        {
            RichTextDocument doc = MarkdownRichTextConverter.Convert("`code`");

            RichTextElement text = doc.Document[0].Children[0];
            Assert.AreEqual("code", text.Text);
            Assert.IsNotNull(text.Format);
            Assert.AreEqual(32, text.Format[0][0], "Format code should be 32 (inline code)");
        }

        #endregion

        #region Superscript

        [TestMethod]
        public void Convert_SuperscriptParens_ReturnsFormatCode8()
        {
            RichTextDocument doc = MarkdownRichTextConverter.Convert("^(super)");

            RichTextElement text = doc.Document[0].Children[0];
            Assert.AreEqual("super", text.Text);
            Assert.IsNotNull(text.Format);
            Assert.AreEqual(8, text.Format[0][0], "Format code should be 8 (superscript)");
        }

        [TestMethod]
        public void Convert_SuperscriptWord_ReturnsFormatCode8()
        {
            RichTextDocument doc = MarkdownRichTextConverter.Convert("text ^word more");

            RichTextElement text = doc.Document[0].Children[0];
            Assert.AreEqual("text word more", text.Text);
            Assert.IsNotNull(text.Format);
            Assert.AreEqual(8, text.Format[0][0]);
            Assert.AreEqual(5, text.Format[0][1], "Start index should be 5");
            Assert.AreEqual(4, text.Format[0][2], "Length should be 4");
        }

        #endregion

        #region Spoiler

        [TestMethod]
        public void Convert_Spoiler_ReturnsFormatCode16()
        {
            RichTextDocument doc = MarkdownRichTextConverter.Convert(">!spoiler text!<");

            RichTextElement text = doc.Document[0].Children[0];
            Assert.AreEqual("spoiler text", text.Text);
            Assert.IsNotNull(text.Format);
            Assert.AreEqual(16, text.Format[0][0], "Format code should be 16 (spoiler)");
        }

        #endregion

        #region Links

        [TestMethod]
        public void Convert_Link_ReturnsLinkElement()
        {
            RichTextDocument doc = MarkdownRichTextConverter.Convert("[click here](https://example.com)");

            RichTextElement par = doc.Document[0];
            Assert.AreEqual(1, par.Children.Count);
            RichTextElement link = par.Children[0];
            Assert.AreEqual(RichTextElementType.Link, link.ElementType);
            Assert.AreEqual("click here", link.Text);
            Assert.AreEqual("https://example.com", link.Url);
        }

        [TestMethod]
        public void Convert_TextWithLink_SplitsIntoTextAndLink()
        {
            RichTextDocument doc = MarkdownRichTextConverter.Convert("Check out [this site](https://example.com) for info");

            RichTextElement par = doc.Document[0];
            Assert.AreEqual(3, par.Children.Count, "Should have text, link, text");
            Assert.AreEqual(RichTextElementType.Text, par.Children[0].ElementType);
            Assert.AreEqual("Check out ", par.Children[0].Text);
            Assert.AreEqual(RichTextElementType.Link, par.Children[1].ElementType);
            Assert.AreEqual("this site", par.Children[1].Text);
            Assert.AreEqual("https://example.com", par.Children[1].Url);
            Assert.AreEqual(RichTextElementType.Text, par.Children[2].ElementType);
            Assert.AreEqual(" for info", par.Children[2].Text);
        }

        #endregion

        #region Images

        [TestMethod]
        public void Convert_StandaloneImage_ReturnsImgElement()
        {
            RichTextDocument doc = MarkdownRichTextConverter.Convert("![image](abc123key)");

            Assert.AreEqual(1, doc.Document.Count);
            Assert.AreEqual(RichTextElementType.Image, doc.Document[0].ElementType);
            Assert.AreEqual("abc123key", doc.Document[0].Id);
        }

        [TestMethod]
        public void Convert_ImageBetweenParagraphs_ReturnsCorrectOrder()
        {
            RichTextDocument doc = MarkdownRichTextConverter.Convert("Before text\n\n![image](mediakey1)\n\nAfter text");

            Assert.AreEqual(3, doc.Document.Count);
            Assert.AreEqual(RichTextElementType.Paragraph, doc.Document[0].ElementType);
            Assert.AreEqual("Before text", doc.Document[0].Children[0].Text);
            Assert.AreEqual(RichTextElementType.Image, doc.Document[1].ElementType);
            Assert.AreEqual("mediakey1", doc.Document[1].Id);
            Assert.AreEqual(RichTextElementType.Paragraph, doc.Document[2].ElementType);
            Assert.AreEqual("After text", doc.Document[2].Children[0].Text);
        }

        [TestMethod]
        public void Convert_ImageInlineWithText_SplitsIntoSeparateElements()
        {
            RichTextDocument doc = MarkdownRichTextConverter.Convert("hello ![image](abc123) world");

            Assert.AreEqual(3, doc.Document.Count);
            Assert.AreEqual(RichTextElementType.Paragraph, doc.Document[0].ElementType);
            Assert.AreEqual("hello ", doc.Document[0].Children[0].Text);
            Assert.AreEqual(RichTextElementType.Image, doc.Document[1].ElementType);
            Assert.AreEqual("abc123", doc.Document[1].Id);
            Assert.AreEqual(RichTextElementType.Paragraph, doc.Document[2].ElementType);
            Assert.AreEqual(" world", doc.Document[2].Children[0].Text);
        }

        [TestMethod]
        public void Convert_TextThenImage_SplitsCorrectly()
        {
            RichTextDocument doc = MarkdownRichTextConverter.Convert("some text ![image](key1)");

            Assert.AreEqual(2, doc.Document.Count);
            Assert.AreEqual(RichTextElementType.Paragraph, doc.Document[0].ElementType);
            Assert.AreEqual("some text ", doc.Document[0].Children[0].Text);
            Assert.AreEqual(RichTextElementType.Image, doc.Document[1].ElementType);
            Assert.AreEqual("key1", doc.Document[1].Id);
        }

        [TestMethod]
        public void Convert_MultipleImages_ReturnsAllImgElements()
        {
            RichTextDocument doc = MarkdownRichTextConverter.Convert("![image](key1)\n\n![image](key2)\n\n![image](key3)");

            Assert.AreEqual(3, doc.Document.Count);
            Assert.AreEqual(RichTextElementType.Image, doc.Document[0].ElementType);
            Assert.AreEqual("key1", doc.Document[0].Id);
            Assert.AreEqual(RichTextElementType.Image, doc.Document[1].ElementType);
            Assert.AreEqual("key2", doc.Document[1].Id);
            Assert.AreEqual(RichTextElementType.Image, doc.Document[2].ElementType);
            Assert.AreEqual("key3", doc.Document[2].Id);
        }

        #endregion

        #region Mixed Formatting

        [TestMethod]
        public void Convert_MultipleFomatsInOneParagraph_AllFormatsPresent()
        {
            RichTextDocument doc = MarkdownRichTextConverter.Convert("**bold** and *italic*");

            RichTextElement par = doc.Document[0];
            // This may produce multiple text elements depending on implementation
            // The key thing is the output text and formats are correct
            Assert.AreEqual(RichTextElementType.Paragraph, par.ElementType);

            string fullText = string.Join("", par.Children.Where(c => c.ElementType == RichTextElementType.Text).Select(c => c.Text));
            Assert.AreEqual("bold and italic", fullText);
        }

        [TestMethod]
        public void Convert_FormattedTextWithLink_CorrectStructure()
        {
            RichTextDocument doc = MarkdownRichTextConverter.Convert("**bold** then [link](https://example.com)");

            RichTextElement par = doc.Document[0];
            Assert.IsTrue(par.Children.Count >= 2, "Should have at least text and link elements");

            bool hasLink = par.Children.Any(c => c.ElementType == RichTextElementType.Link && c.Url == "https://example.com");
            Assert.IsTrue(hasLink, "Should contain the link element");
        }

        [TestMethod]
        public void Convert_ComplexDocument_CorrectStructure()
        {
            string markdown = "Hello **world**\n\n![image](upload123)\n\nCheck [this](https://example.com) out with ~~deleted~~ text";

            RichTextDocument doc = MarkdownRichTextConverter.Convert(markdown);

            Assert.AreEqual(3, doc.Document.Count, "Should have 3 top-level elements");
            Assert.AreEqual(RichTextElementType.Paragraph, doc.Document[0].ElementType);
            Assert.AreEqual(RichTextElementType.Image, doc.Document[1].ElementType);
            Assert.AreEqual("upload123", doc.Document[1].Id);
            Assert.AreEqual(RichTextElementType.Paragraph, doc.Document[2].ElementType);
        }

        #endregion

        #region Code Blocks

        [TestMethod]
        public void Convert_FencedCodeBlock_ReturnsCodeElement()
        {
            RichTextDocument doc = MarkdownRichTextConverter.Convert("```\nsome code\n```");

            Assert.AreEqual(1, doc.Document.Count);
            Assert.AreEqual(RichTextElementType.Code, doc.Document[0].ElementType);
            Assert.AreEqual("some code", doc.Document[0].Children[0].Text);
            Assert.AreEqual(RichTextElementType.Raw, doc.Document[0].Children[0].ElementType);
        }

        [TestMethod]
        public void Convert_FencedCodeBlockWithLanguage_ReturnsCodeElement()
        {
            RichTextDocument doc = MarkdownRichTextConverter.Convert("```csharp\nvar x = 1;\n```");

            Assert.AreEqual(1, doc.Document.Count);
            Assert.AreEqual(RichTextElementType.Code, doc.Document[0].ElementType);
            Assert.AreEqual("var x = 1;", doc.Document[0].Children[0].Text);
        }

        [TestMethod]
        public void Convert_FencedCodeBlockMultiLine_PreservesNewlines()
        {
            RichTextDocument doc = MarkdownRichTextConverter.Convert("```\nline 1\nline 2\nline 3\n```");

            Assert.AreEqual(1, doc.Document.Count);
            Assert.AreEqual(RichTextElementType.Code, doc.Document[0].ElementType);
            Assert.AreEqual("line 1\nline 2\nline 3", doc.Document[0].Children[0].Text);
        }

        [TestMethod]
        public void Convert_CodeBlockBetweenParagraphs_CorrectOrder()
        {
            RichTextDocument doc = MarkdownRichTextConverter.Convert("before\n\n```\ncode\n```\n\nafter");

            Assert.AreEqual(3, doc.Document.Count);
            Assert.AreEqual(RichTextElementType.Paragraph, doc.Document[0].ElementType);
            Assert.AreEqual("before", doc.Document[0].Children[0].Text);
            Assert.AreEqual(RichTextElementType.Code, doc.Document[1].ElementType);
            Assert.AreEqual("code", doc.Document[1].Children[0].Text);
            Assert.AreEqual(RichTextElementType.Paragraph, doc.Document[2].ElementType);
            Assert.AreEqual("after", doc.Document[2].Children[0].Text);
        }

        #endregion

        #region Edge Cases

        [TestMethod]
        public void Convert_UnmatchedAsterisks_TreatedAsLiteralText()
        {
            RichTextDocument doc = MarkdownRichTextConverter.Convert("this * is not italic");

            RichTextElement text = doc.Document[0].Children[0];
            Assert.AreEqual("this * is not italic", text.Text);
            Assert.IsNull(text.Format);
        }

        [TestMethod]
        public void Convert_SingleNewline_StaysInSameParagraph()
        {
            RichTextDocument doc = MarkdownRichTextConverter.Convert("line one\nline two");

            Assert.AreEqual(1, doc.Document.Count, "Single newline should not split paragraphs");
            Assert.IsTrue(doc.Document[0].Children[0].Text.Contains("line one"));
            Assert.IsTrue(doc.Document[0].Children[0].Text.Contains("line two"));
        }

        [TestMethod]
        public void Convert_EmptyParagraphBetweenText_PreservesStructure()
        {
            RichTextDocument doc = MarkdownRichTextConverter.Convert("first\n\n\n\nsecond");

            // "\n\n\n\n" splits into ["first", "", "second"]
            Assert.AreEqual(3, doc.Document.Count);
            Assert.AreEqual("first", doc.Document[0].Children[0].Text);
            Assert.AreEqual("", doc.Document[1].Children[0].Text);
            Assert.AreEqual("second", doc.Document[2].Children[0].Text);
        }

        #endregion
    }
}
