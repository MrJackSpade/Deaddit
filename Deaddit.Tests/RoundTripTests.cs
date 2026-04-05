using Deaddit.Core.Reddit;
using Microsoft.Extensions.Configuration;
using Reddit.Api;
using Reddit.Api.Client;
using Reddit.Api.Models.Json.Common;
using Reddit.Api.Models.Json.Listings;
using Reddit.Api.Models.Json.Media;

namespace Deaddit.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class RoundTripTests
    {
        private static RedditClient? _client;
        private static bool _authenticated;
        private static string? _testPostFullname;

        [ClassInitialize]
        public static async Task ClassInit(TestContext context)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddUserSecrets<RoundTripTests>()
                .Build();

            string? username = config["Reddit:Username"];
            string? password = config["Reddit:Password"];
            string? appKey = config["Reddit:AppKey"];
            string? appSecret = config["Reddit:AppSecret"];

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) ||
                string.IsNullOrEmpty(appKey) || string.IsNullOrEmpty(appSecret))
            {
                return;
            }

            _client = new RedditClient(new RedditCredentials
            {
                Username = username,
                Password = password,
                AppKey = appKey,
                AppSecret = appSecret,
                UserAgent = "Deaddit.Tests/1.0"
            });

            _authenticated = await _client.AuthenticateAsync();

            if (_authenticated)
            {
                Listing<Thing<Link>>? posts = await _client.GetNewAsync("DeadditApp", new ListingParameters { Limit = 1 });

                if (posts?.Data?.Children?.Count > 0)
                {
                    _testPostFullname = posts.Data.Children[0].Data!.Name;
                }
            }
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            _client?.Dispose();
        }

        private void EnsureReady()
        {
            if (_client == null || !_authenticated)
            {
                Assert.Inconclusive("Reddit API credentials not configured or auth failed.");
            }

            if (_testPostFullname == null)
            {
                Assert.Inconclusive("No posts found in r/DeadditApp.");
            }
        }

        [TestMethod]
        public async Task RoundTrip_PlainText_NoDrift()
        {
            await AssertNoDrift("Hello world, this is plain text.");
        }

        [TestMethod]
        public async Task RoundTrip_Bold_NoDrift()
        {
            await AssertNoDrift("This is **bold** text.");
        }

        [TestMethod]
        public async Task RoundTrip_Italic_NoDrift()
        {
            await AssertNoDrift("This is *italic* text.");
        }

        [TestMethod]
        public async Task RoundTrip_BoldItalic_NoDrift()
        {
            await AssertNoDrift("This is ***bold italic*** text.");
        }

        [TestMethod]
        public async Task RoundTrip_Strikethrough_NoDrift()
        {
            await AssertNoDrift("This is ~~struck~~ text.");
        }

        [TestMethod]
        public async Task RoundTrip_Link_NoDrift()
        {
            await AssertNoDrift("Check out [this site](https://example.com) for info.");
        }

        [TestMethod]
        public async Task RoundTrip_InlineCode_NoDrift()
        {
            await AssertNoDrift("Use `console.log()` to debug.");
        }

        [TestMethod]
        public async Task RoundTrip_CodeBlock_NoDrift()
        {
            await AssertNoDrift("Before\n\n```\nvar x = 1;\nvar y = 2;\n```\n\nAfter");
        }

        [TestMethod]
        public async Task RoundTrip_Blockquote_NoDrift()
        {
            await AssertNoDrift("Before\n\n> This is a quote\n\nAfter");
        }

        [TestMethod]
        public async Task RoundTrip_MultipleParagraphs_NoDrift()
        {
            await AssertNoDrift("First paragraph.\n\nSecond paragraph.\n\nThird paragraph.");
        }

        [TestMethod]
        public async Task RoundTrip_Brackets_NoDrift()
        {
            await AssertNoDrift("array[0] and array[1] are elements.");
        }

        [TestMethod]
        public async Task RoundTrip_MixedFormatting_NoDrift()
        {
            await AssertNoDrift("**bold** and *italic* and ~~struck~~ and `code` together.");
        }

        [TestMethod]
        public async Task RoundTrip_Spoiler_PassesThrough()
        {
            // Reddit richtext format has no spoiler format code, so >!text!< is passed as literal text
            await AssertNoDrift("This is >!spoiler text!< hidden.");
        }

        [TestMethod]
        public async Task RoundTrip_Superscript_NoDrift()
        {
            await AssertNoDrift("This is ^(superscript) text.");
        }

        [TestMethod]
        public async Task RoundTrip_UnorderedList_NoDrift()
        {
            await AssertNoDrift("Items:\n\n- one\n- two\n- three");
        }

        [TestMethod]
        public async Task RoundTrip_OrderedList_NoDrift()
        {
            await AssertNoDrift("Steps:\n\n1. first\n2. second\n3. third");
        }

        [TestMethod]
        public async Task RoundTrip_Heading_NoDrift()
        {
            await AssertNoDrift("# Title\n\nSome text after heading.");
        }

        [TestMethod]
        public async Task RoundTrip_HorizontalRule_NoDrift()
        {
            await AssertNoDrift("Above\n\n---\n\nBelow");
        }

        [TestMethod]
        public async Task RoundTrip_BlockquoteFollowedByBareUrl_NoDrift()
        {
            await AssertNoDrift("> This is a quote\n\nhttps://www.example.com");
        }

        // Nested blockquotes (> > text) are not supported by Reddit's richtext format.
        // Single-level blockquotes are tested by RoundTrip_Blockquote_NoDrift.

        [TestMethod]
        public async Task RoundTrip_ComplexDocument_NoDrift()
        {
            await AssertNoDrift(
                "**Hello** *world*\n\n" +
                "> A famous quote\n\n" +
                "Check [this link](https://example.com) and `some code`.\n\n" +
                "```\nfunction test() {\n  return true;\n}\n```\n\n" +
                "Final paragraph with ~~deleted~~ text.");
        }

        /// <summary>
        /// Posts a comment with the given markdown, fetches it back,
        /// converts body_html → markdown → richtext, edits with that richtext,
        /// fetches again, and asserts the body_html hasn't drifted.
        /// </summary>
        private async Task AssertNoDrift(string originalMarkdown)
        {
            EnsureReady();

            // Step 1: Post original comment using richtext
            RichTextDocument originalRichtext = MarkdownRichTextConverter.Convert(originalMarkdown);
            Thing<Comment>? posted = await _client!.CommentAsync(_testPostFullname!, originalRichtext);
            Assert.IsNotNull(posted?.Data, "Failed to post comment");
            Assert.IsFalse(string.IsNullOrEmpty(posted.Data.Name), "Comment fullname not returned");
            Assert.IsFalse(string.IsNullOrEmpty(posted.Data.BodyHtml), "body_html not returned in post response");

            string commentFullname = posted.Data.Name;

            try
            {
                string firstBodyHtml = posted.Data.BodyHtml!;

                // Step 2: HTML → Markdown → Richtext (simulating an edit round-trip)
                string convertedMarkdown = HtmlMarkdownConverter.Convert(firstBodyHtml);
                RichTextDocument convertedRichtext = MarkdownRichTextConverter.Convert(convertedMarkdown);

                // Step 3: Edit the comment with the converted richtext
                Thing<Comment>? edited = await _client.EditAsync(commentFullname, convertedRichtext);
                Assert.IsNotNull(edited?.Data, "Failed to edit comment");
                Assert.IsFalse(string.IsNullOrEmpty(edited.Data.BodyHtml), "body_html not returned in edit response");

                string secondBodyHtml = edited.Data.BodyHtml!;

                // Step 4: Assert no drift
                Assert.AreEqual(firstBodyHtml, secondBodyHtml,
                    $"body_html drifted after round-trip.\n" +
                    $"Original markdown: {originalMarkdown}\n" +
                    $"Converted markdown: {convertedMarkdown}\n" +
                    $"First HTML:  {firstBodyHtml}\n" +
                    $"Second HTML: {secondBodyHtml}");
            }
            finally
            {
                await _client.DeleteThingAsync(commentFullname);
                // Reddit rate-limits comment posting separately from API calls
                await Task.Delay(10_000);
            }
        }
    }
}
