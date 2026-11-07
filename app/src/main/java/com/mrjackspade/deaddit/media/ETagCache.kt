package com.mrjackspade.deaddit.media

import com.mrjackspade.deaddit.core.model.isAbsoluteUrl
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.async
import kotlinx.coroutines.awaitAll
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.withContext
import okhttp3.OkHttpClient
import okhttp3.Request
import java.util.concurrent.ConcurrentHashMap

/**
 * Thumbnails' ETags, for spotting the same image posted under different URLs ("Duplicate Thumb
 * Handling"). One HEAD request per URL, remembered for the session.
 */
class ETagCache(private val http: OkHttpClient) {
    private val cache = ConcurrentHashMap<String, String>()

    suspend fun cache(urls: List<String>) = coroutineScope {
        urls.map { async { get(it) } }.awaitAll()
    }

    /** The URL's ETag; empty when it isn't an absolute URL or the server sends none. */
    suspend fun get(url: String): String {
        if (!isAbsoluteUrl(url)) return ""
        cache[url]?.let { return it }

        val etag = withContext(Dispatchers.IO) {
            http.newCall(Request.Builder().url(url).head().build()).execute().use { it.header("ETag") ?: "" }
        }

        cache.putIfAbsent(url, etag)
        return etag
    }
}
