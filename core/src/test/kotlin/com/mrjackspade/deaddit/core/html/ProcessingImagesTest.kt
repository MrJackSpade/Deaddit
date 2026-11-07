package com.mrjackspade.deaddit.core.html

import com.mrjackspade.deaddit.core.model.Comment
import com.mrjackspade.deaddit.core.model.RedditJson
import org.junit.Assert.assertEquals
import org.junit.Test

class ProcessingImagesTest {
    /**
     * The user's comment as www.reddit.com served it on 2026-11-07, trimmed to what matters, with
     * body_html decoded as Reddit.Api's Comment model delivers it.
     */
    private val comment = RedditJson.decodeFromString(
        Comment.serializer(),
        """
        {"id":"19s92t68yz56o","name":"t1_19s92t68yz56o",
         "body":"*Processing img akmw191b530i1...*",
         "body_html":"<div class=\"md\"><p><em>Processing img akmw191b530i1...</em></p>\n</div>",
         "media_metadata":{"akmw191b530i1":{"status":"valid","e":"AnimatedImage","m":"image/gif",
           "s":{"y":438,"gif":"https://i.redd.it/akmw191b530i1.gif","mp4":"https://preview.redd.it/akmw191b530i1.gif?format=mp4&s=591b","x":640},
           "id":"akmw191b530i1"}}}
        """.trimIndent(),
    )

    @Test
    fun replacesAStalePlaceholderWithTheImage() {
        val html = ProcessingImages.resolve(comment.bodyHtml!!, comment.mediaMetadata)
        assertEquals(
            listOf(Block.Paragraph(listOf(Span("https://i.redd.it/akmw191b530i1.gif", link = "https://i.redd.it/akmw191b530i1.gif")))),
            RedditHtml.parse(html),
        )
    }

    @Test
    fun leavesImagesThatAreStillProcessing() {
        val processing = comment.mediaMetadata!!.mapValues { it.value.copy(status = "unprocessed") }
        assertEquals(
            listOf(Block.Paragraph(listOf(Span("Processing img akmw191b530i1...", italic = true)))),
            RedditHtml.parse(ProcessingImages.resolve(comment.bodyHtml!!, processing)),
        )
    }

    @Test
    fun usesTheFullImageForStillImagesAndEscapesIt() {
        val still = comment.mediaMetadata!!.mapValues {
            it.value.copy(kind = "Image", source = it.value.source!!.copy(url = "https://preview.redd.it/x.png?width=640&s=abc", gif = null))
        }
        val html = ProcessingImages.resolve(comment.bodyHtml!!, still)
        assertEquals(
            listOf(Block.Paragraph(listOf(Span("https://preview.redd.it/x.png?width=640&s=abc", link = "https://preview.redd.it/x.png?width=640&s=abc")))),
            RedditHtml.parse(html),
        )
    }
}
