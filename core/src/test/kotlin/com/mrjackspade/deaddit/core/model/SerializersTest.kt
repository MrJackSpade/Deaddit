package com.mrjackspade.deaddit.core.model

import kotlinx.serialization.SerializationException
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertThrows
import org.junit.Test

class SerializersTest {
    private fun comment(fields: String): Comment =
        RedditJson.decodeFromString(Comment.serializer(), """{"id":"c","name":"t1_c",$fields}""")

    @Test
    fun readsEnumsAsEmptyStringEnumConverterDid() {
        assertNull(comment(""""distinguished":null""").distinguished)
        assertEquals(Distinguished.Empty, comment("\"distinguished\":\"\"").distinguished)
        assertEquals(Distinguished.Moderator, comment(""""distinguished":"MODERATOR"""").distinguished)
        assertEquals(CollapsedReasonCode.Empty, comment("\"collapsed_reason_code\":\"\"").collapsedReasonCode)
        assertEquals(CollapsedReasonCode.BlockedAuthor, comment(""""collapsed_reason_code":"blocked_author"""").collapsedReasonCode)
    }

    @Test
    fun unknownEnumValuesFailTheRead() {
        assertThrows(SerializationException::class.java) { comment(""""distinguished":"special"""") }
        assertThrows(SerializationException::class.java) { comment(""""collapsed_reason_code":"spam"""") }
        assertThrows(SerializationException::class.java) { comment(""""distinguished":1""") }
    }

    @Test
    fun timestampsAtOrBeforeTheEpochAreNoDate() {
        assertNull(comment(""""created_utc":0""").createdUtc)
        assertNull(comment(""""created_utc":-86400""").createdUtc)
        assertNull(comment(""""created_utc":null""").createdUtc)
        assertNull(comment(""""edited":false""").edited)
        assertNull(comment(""""created_utc":"-5"""").createdUtc)
        assertNull(comment("\"created_utc\":\"\"").createdUtc)
        assertEquals(1730900000L, comment(""""created_utc":1730900000.0""").createdUtc!!.epochSecond)
        assertEquals(1730900000L, comment(""""created_utc":"1730900000"""").createdUtc!!.epochSecond)
        assertThrows(SerializationException::class.java) { comment(""""created_utc":{}""") }
    }

    @Test
    fun readsJsonBoolLikeTheCSharpApp() {
        fun post(value: String) = RedditJson.decodeFromString(Post.serializer(), """{"id":"p","name":"t3_p","is_gallery":$value}""")
        assertEquals(true, post("true").isGallery)
        assertFalse(post("false").isGallery)
        assertFalse(post("null").isGallery)
        assertFalse(post("\"\"").isGallery)
    }

    @Test
    fun flairColorsKeepAnyValueButEmpty() {
        fun post(value: String) =
            RedditJson.decodeFromString(Post.serializer(), """{"id":"p","name":"t3_p","link_flair_background_color":$value}""").linkFlairBackgroundColor
        assertNull(post("null"))
        assertNull(post("\"\""))
        assertEquals(" ", post("\" \""))
        assertEquals("red", post("\"red\""))
    }
}
