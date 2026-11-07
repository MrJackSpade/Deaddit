package com.mrjackspade.deaddit.core.model

import org.junit.Assert.assertEquals
import org.junit.Test

class MediaMetadataTest {
    /** A gallery image as the saved listing served it on 2026-11-07, with its URL HTML-escaped. */
    @Test
    fun decodesEscapedGalleryUrls() {
        val item = RedditJson.decodeFromString(
            MediaMetadata.serializer(),
            """
            {"status":"valid","e":"Image","m":"image/jpeg","id":"ux66962ei20i1",
             "s":{"y":2400,"x":1690,"u":"https://preview.redd.it/ux66962ei20i1.jpeg?width=1690&amp;format=pjpg&amp;auto=webp&amp;s=07787ad642a6521078306e57678a2d3fbe1caef6"}}
            """.trimIndent(),
        )
        assertEquals(
            "https://preview.redd.it/ux66962ei20i1.jpeg?width=1690&format=pjpg&auto=webp&s=07787ad642a6521078306e57678a2d3fbe1caef6",
            item.source!!.url,
        )
    }
}
