package com.mrjackspade.deaddit.core.api

import kotlinx.coroutines.runBlocking
import kotlinx.serialization.json.JsonElement
import kotlinx.serialization.json.JsonNull
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.Json
import org.junit.Assert.assertEquals
import org.junit.Assert.assertThrows
import org.junit.Test

/** Upload failures carry the C# app's messages (RedditService.UploadMedia). */
class UploadMediaTest {
    private val lease = Json.parseToJsonElement("""{"args":{"action":"//s3","fields":[{"name":"key","value":"k1"}]},"asset":{"asset_id":"a1"}}""")

    private fun api(lease: JsonElement, uploaded: Boolean) = RedditApi { method, _ ->
        when (method) {
            "GetMediaAssetUploadLease" -> lease
            "UploadMediaToS3" -> JsonPrimitive(uploaded)
            else -> throw IllegalArgumentException(method)
        }
    }

    @Test
    fun returnsTheMediaKey() {
        assertEquals("k1", runBlocking { api(lease, uploaded = true).uploadMedia(byteArrayOf(1), "a.png", "image/png") })
    }

    @Test
    fun noLease() {
        val error = assertThrows(IllegalStateException::class.java) { runBlocking { api(JsonNull, uploaded = true).uploadMedia(byteArrayOf(1), "a.png", "image/png") } }
        assertEquals("Failed to get media upload lease", error.message)
    }

    @Test
    fun uploadRefused() {
        val error = assertThrows(IllegalStateException::class.java) { runBlocking { api(lease, uploaded = false).uploadMedia(byteArrayOf(1), "a.png", "image/png") } }
        assertEquals("Failed to upload media to S3", error.message)
    }
}
