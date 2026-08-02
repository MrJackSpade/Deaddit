using Deaddit.Core.Reddit;

namespace Deaddit.Tests
{
    [TestClass]
    public class MarkdownAnalyzerTests
    {
        [TestMethod]
        public void HasNestedBlockquote_NullOrEmpty_ReturnsFalse()
        {
            Assert.IsFalse(MarkdownAnalyzer.HasNestedBlockquote(null));
            Assert.IsFalse(MarkdownAnalyzer.HasNestedBlockquote(""));
        }

        [TestMethod]
        public void HasNestedBlockquote_PlainText_ReturnsFalse()
        {
            Assert.IsFalse(MarkdownAnalyzer.HasNestedBlockquote("just plain text\nwith another line"));
        }

        [TestMethod]
        public void HasNestedBlockquote_SingleLevelQuote_ReturnsFalse()
        {
            Assert.IsFalse(MarkdownAnalyzer.HasNestedBlockquote("> a single-level quote"));
        }

        [TestMethod]
        public void HasNestedBlockquote_TwoLevelQuote_ReturnsTrue()
        {
            Assert.IsTrue(MarkdownAnalyzer.HasNestedBlockquote("> > a nested quote"));
        }

        [TestMethod]
        public void HasNestedBlockquote_ThreeLevelQuote_ReturnsTrue()
        {
            Assert.IsTrue(MarkdownAnalyzer.HasNestedBlockquote("> > > deep quote"));
        }

        [TestMethod]
        public void HasNestedBlockquote_NestedQuoteOnLaterLine_ReturnsTrue()
        {
            string md = "Some intro paragraph.\n\n> top-level\n> > nested\n\nOutro.";
            Assert.IsTrue(MarkdownAnalyzer.HasNestedBlockquote(md));
        }

        [TestMethod]
        public void ContainsImagePlaceholder_NullOrEmpty_ReturnsFalse()
        {
            Assert.IsFalse(MarkdownAnalyzer.ContainsImagePlaceholder(null));
            Assert.IsFalse(MarkdownAnalyzer.ContainsImagePlaceholder(""));
        }

        [TestMethod]
        public void ContainsImagePlaceholder_PlainText_ReturnsFalse()
        {
            Assert.IsFalse(MarkdownAnalyzer.ContainsImagePlaceholder("no image here"));
        }

        [TestMethod]
        public void ContainsImagePlaceholder_NamedImagePlaceholder_ReturnsTrue()
        {
            Assert.IsTrue(MarkdownAnalyzer.ContainsImagePlaceholder("before ![image](abc123) after"));
        }

        [TestMethod]
        public void ContainsImagePlaceholder_GenericMarkdownImage_ReturnsFalse()
        {
            // The richtext pipeline only consumes the literal "![image](key)" form
            Assert.IsFalse(MarkdownAnalyzer.ContainsImagePlaceholder("![cat](https://example.com/cat.jpg)"));
        }
    }
}
