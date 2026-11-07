package com.mrjackspade.deaddit.media

import com.mrjackspade.deaddit.core.model.isAbsoluteUrl
import com.mrjackspade.deaddit.core.util.DotNetHttpHeaders
import kotlinx.coroutines.async
import kotlinx.coroutines.awaitAll
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.sync.Semaphore
import kotlinx.coroutines.sync.withPermit
import okhttp3.OkHttpClient
import okhttp3.Request
import java.util.concurrent.ConcurrentHashMap

/**
 * Thumbnails' ETags, for spotting the same image posted under different URLs ("Duplicate Thumb
 * Handling"). One HEAD request per URL, remembered for the session.
 */
class ETagCache(private val http: OkHttpClient) {
    private val cache = ConcurrentHashMap<String, String>()

    /**
     * Fetches the URLs' ETags, at most one request per processor at a time: the C# Parallel.ForEachAsync, whose
     * default degree of parallelism is Environment.ProcessorCount.
     */
    suspend fun cache(urls: List<String>) = coroutineScope {
        val permits = Semaphore(Runtime.getRuntime().availableProcessors())
        urls.map { async { permits.withPermit { get(it) } } }.awaitAll()
    }

    /**
     * The URL's ETag as the C# stored it (`response.Headers.ETag?.Tag ?? ""`): the quoted tag without any `W/`, so a
     * weak and a strong ETag with the same tag match; empty when it isn't an absolute URL, or the server sends no
     * ETag or one .NET can't parse.
     */
    suspend fun get(url: String): String {
        if (!isAbsoluteUrl(url)) return ""
        cache[url]?.let { return it }

        val etag = http.fetch(Request.Builder().url(url).head().build()) { DotNetHttpHeaders.entityTag(it.headers("ETag")) ?: "" }

        cache.putIfAbsent(url, etag)
        return etag
    }
}
