package com.mrjackspade.deaddit.core.model

import org.junit.Assert.assertEquals
import org.junit.Test

class MediaMetadataTest {
    /**
     * Reddit.Api's model already decoded the URL, so what's left is literal: an "&amp;" here was
     * "&amp;amp;" in Reddit's JSON, and must not be decoded again.
     */
    @Test
    fun keepsTheUrlsTheModelDecoded() {
        val item = RedditJson.decodeFromString(
            MediaMetadata.serializer(),
            """
            {"status":"valid","e":"AnimatedImage","m":"image/gif","id":"ux66962ei20i1",
             "s":{"y":2400,"x":1690,"u":"https://preview.redd.it/a.jpeg?q=1&amp;s=07","gif":"https://i.redd.it/a.gif?x=&amp;","mp4":"https://preview.redd.it/a.gif?format=mp4&amp;s=1"}}
            """.trimIndent(),
        )
        assertEquals("https://preview.redd.it/a.jpeg?q=1&amp;s=07", item.source!!.url)
        assertEquals("https://i.redd.it/a.gif?x=&amp;", item.source!!.gif)
        assertEquals("https://preview.redd.it/a.gif?format=mp4&amp;s=1", item.source!!.mp4)
    }
}
