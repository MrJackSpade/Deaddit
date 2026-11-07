package com.mrjackspade.deaddit.core.model

import com.mrjackspade.deaddit.core.api.ListingItem
import com.mrjackspade.deaddit.core.blocking.Blocking
import com.mrjackspade.deaddit.core.config.BlockRule
import com.mrjackspade.deaddit.core.html.Block
import com.mrjackspade.deaddit.core.html.RedditHtml
import com.mrjackspade.deaddit.core.markdown.HtmlMarkdownConverter
import kotlinx.serialization.json.JsonElement
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * Things as the bridge delivers them. The resources in `bridge/` named "-raw.json" are Reddit's
 * JSON, and those named "-bridged.json" what native/RedditApi.Native's TypedThings and Bridge.ModelJson made of
 * them (.NET 10, the native project built managed), which is what reaches Kotlin: every text field
 * decoded exactly once by Reddit.Api's models, as in the C# app.
 */
class BridgedThingsTest {
    private fun resource(name: String): JsonElement {
        val path = "/com/mrjackspade/deaddit/core/bridge/$name"
        val text = (BridgedThingsTest::class.java.getResourceAsStream(path) ?: throw IllegalStateException("Missing resource $path"))
            .use { it.readBytes().toString(Charsets.UTF_8) }
        return RedditJson.parseToJsonElement(text)
    }

    private val listing = ListingItem.parseListing(resource("listing-bridged.json"))
    private val selfPost = (listing[0] as ListingItem.PostItem).post
    private val galleryPost = (listing[1] as ListingItem.PostItem).post
    private val comment = (listing[2] as ListingItem.CommentItem).comment
    private val message = (listing[3] as ListingItem.MessageItem).message

    @Test
    fun decodesPostTextAndFlair() {
        assertEquals("Tom & Jerry <3\n\n    line one\n    line two", selfPost.body)
        assertEquals("C# & Kotlin", selfPost.authorFlairText)
        assertEquals("C# & Kotlin", selfPost.authorFlairRichtext.single().text)
        assertEquals("Q&A", selfPost.linkFlairText)
        assertEquals("Q&A", selfPost.linkFlairRichtext[0].text)
        assertEquals("https://emoji.redditmedia.com/x/snoo.png?a=1&b=2", selfPost.linkFlairRichtext[1].url)
    }

    @Test
    fun leavesTheTitleAsRedditSentIt() {
        // Reddit.Api's Link doesn't decode titles, and the C# app showed them as they came.
        assertEquals("Cats &amp; dogs", selfPost.title)
        assertEquals("Cats &amp; dogs", comment.linkTitle)
        assertEquals("Hi &amp; bye", message.subject)
    }

    @Test
    fun keepsCodeBlockLines() {
        assertEquals(
            listOf(Block.Paragraph(listOf(com.mrjackspade.deaddit.core.html.Span("Tom & Jerry <3"))), Block.Code("line one\nline two")),
            RedditHtml.parse(selfPost.bodyHtml!!),
        )
        assertEquals(Block.Code("code one\ncode two"), RedditHtml.parse(comment.bodyHtml!!).last())
    }

    @Test
    fun editingShowsMarkdown() {
        val markdown = HtmlMarkdownConverter.convert(comment.bodyHtml)
        assertFalse(markdown, markdown.contains("<"))
        assertTrue(markdown, markdown.contains("x & y > z"))
        assertTrue(markdown, markdown.contains("code one\n"))
    }

    @Test
    fun blocklistSeesDecodedText() {
        assertTrue(Blocking.isMatch(BlockRule(body = "Tom & Jerry"), selfPost))
        assertTrue(Blocking.isMatch(BlockRule(flair = "Q&A"), selfPost))
        assertTrue(Blocking.isMatch(BlockRule(body = "y > z"), comment))
    }

    @Test
    fun decodesCommentsAndMessages() {
        assertEquals("x & y > z\n\n    code one\n    code two", comment.body)
        assertEquals("Mod & Friend", comment.authorFlairText)
        assertEquals("a < b", message.body)
        assertEquals("<!-- SC_OFF --><div class=\"md\"><p>a &lt; b</p>\n</div><!-- SC_ON -->", message.bodyHtml)
    }

    @Test
    fun decodesEveryImageUrlOnce() {
        // Decoded by Reddit.Api's models.
        assertEquals("https://preview.redd.it/abc.jpg?auto=webp&s=123", selfPost.preview!!.images.single().source!!.url)
        assertEquals("https://preview.redd.it/abc.jpg?width=108&crop=smart&auto=webp&s=456", selfPost.preview!!.images.single().resolutions.single().url)
        val image = galleryPost.mediaMetadata!!.getValue("g1")
        assertEquals("https://preview.redd.it/g1.jpg?width=1690&format=pjpg&auto=webp&s=bb", image.source!!.url)
        assertEquals("https://preview.redd.it/g1.jpg?width=108&crop=smart&auto=webp&s=aa", image.previews.single().url)

        // Passed on escaped by the models, decoded here.
        assertEquals("https://www.reddit.com/r/programming/comments/p1/cats_dogs/?a=1&b=2", selfPost.url)
        assertEquals("https://v.redd.it/v1/DASHPlaylist.mpd?a=1&v=1&f=sd", galleryPost.secureMedia!!.redditVideo!!.dashUrl)
        assertEquals("https://v.redd.it/v1/HLSPlaylist.m3u8?a=1&v=1&f=sd", galleryPost.secureMedia!!.redditVideo!!.hlsUrl)
        assertEquals("https://i.ytimg.com/vi/x/hqdefault.jpg?a=1&b=2", galleryPost.media!!.oembed!!.thumbnailUrl)
        assertEquals("https://example.com/?x=1&y=2", galleryPost.galleryData!!.items.single().outboundUrl)
    }

    @Test
    fun readsRedditApiShapes() {
        assertFalse(selfPost.isGallery)
        assertTrue(galleryPost.isGallery)
        assertNull(selfPost.distinguished)
        assertEquals(Distinguished.Moderator, galleryPost.distinguished)
        assertEquals("#ff4500", selfPost.authorFlairBackgroundColor)
        assertNull(selfPost.linkFlairBackgroundColor)
        assertEquals(1730900000L, selfPost.createdUtc!!.epochSecond)
        assertNull(comment.edited)
        assertNull(comment.collapsedReasonCode)
    }

    @Test
    fun repliesAreDecodedAllTheWayDown() {
        val comments = ListingItem.parseListing(resource("comments-bridged.json"))
        val top = (comments[0] as ListingItem.CommentItem).comment
        assertEquals("Top & level", top.body)
        assertEquals("Flair & more", top.authorFlairText)
        assertEquals("Flair & more", top.authorFlairRichtext.single().text)

        val reply = (ListingItem.parseListing(top.replies!!).single() as ListingItem.CommentItem).comment
        assertEquals("Nested <b> &amp;\n\n    a\n    b", reply.body)
        assertEquals(Block.Code("a\nb"), RedditHtml.parse(reply.bodyHtml!!).last())
        assertEquals(CollapsedReasonCode.Empty, reply.collapsedReasonCode)
        assertEquals(Distinguished.Admin, reply.distinguished)
        assertEquals(1730900200L, reply.edited!!.epochSecond)

        val more = (ListingItem.parseListing(reply.replies!!).single() as ListingItem.MoreItem).more
        assertEquals(listOf("c3", "c4", "c5"), more.children)
        assertEquals(3, more.count)

        val continueThread = (comments[1] as ListingItem.MoreItem).more
        assertEquals("t1__", continueThread.name)
        assertTrue(continueThread.children.isEmpty())
    }
}
