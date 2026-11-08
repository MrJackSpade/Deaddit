package com.mrjackspade.deaddit.core.api

import com.mrjackspade.deaddit.core.model.RedditJson
import org.junit.Assert.assertEquals
import kotlinx.serialization.builtins.serializer
import org.junit.Test

/** Posts as the bridge delivers them: from Reddit's JSON (selftext always there) or from www's HTML (no selftext). */
class DecodePostTest {
    private val html = "<p>A <strong>bold</strong> <a href=\"https://example.com/\">link</a></p>"

    private fun post(selftext: String?): ListingItem.PostItem {
        val selftextField = if (selftext == null) "" else ",\"selftext\":${RedditJson.encodeToString(String.serializer(), selftext)}"
        val json = "{\"kind\":\"t3\",\"data\":{\"id\":\"x\",\"name\":\"t3_x\",\"selftext_html\":${RedditJson.encodeToString(String.serializer(), html)}$selftextField}}"
        return ListingItem.parse(RedditJson.parseToJsonElement(json)) as ListingItem.PostItem
    }

    @Test
    fun rebuildsTheMarkdownOfAPostFromWww() {
        assertEquals("A **bold** [link](https://example.com/)", post(selftext = null).post.body)
    }

    @Test
    fun keepsTheMarkdownRedditsJsonSent() {
        assertEquals("A **bold** [link]( https://example.com/ )", post(selftext = "A **bold** [link]( https://example.com/ )").post.body)
    }

    @Test
    fun leavesAPostWithoutABodyEmpty() {
        val json = "{\"kind\":\"t3\",\"data\":{\"id\":\"x\",\"name\":\"t3_x\"}}"
        assertEquals("", (ListingItem.parse(RedditJson.parseToJsonElement(json)) as ListingItem.PostItem).post.body)
    }
}
