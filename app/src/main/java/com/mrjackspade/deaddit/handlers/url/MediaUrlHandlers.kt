package com.mrjackspade.deaddit.handlers.url

import com.mrjackspade.deaddit.Services
import com.mrjackspade.deaddit.core.model.isAbsoluteUrl
import com.mrjackspade.deaddit.core.util.DotNetStrings
import com.mrjackspade.deaddit.core.util.DotNetUri
import com.mrjackspade.deaddit.core.util.UrlHelper
import com.mrjackspade.deaddit.handlers.FileDownload
import com.mrjackspade.deaddit.handlers.PostHandlers
import com.mrjackspade.deaddit.handlers.UrlHandler
import com.mrjackspade.deaddit.ui.Pages
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import kotlinx.coroutines.withContext
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.contentOrNull
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive
import okhttp3.Request

/**
 * RedGifs pages: asks RedGifs' API (as its own site does) for the video's HD file and plays it.
 * The C# scraped the watch page instead, which only links the silent file and found nothing at all
 * for links ending in "/" or carrying a "#rel=..." fragment.
 */
class RedGifsHandler(private val services: Services) : UrlHandler {
    private val json = Json { ignoreUnknownKeys = true }
    private val tokenLock = Mutex()
    private var token: String? = null

    // RedGifs requires URL resolution, not directly downloadable
    override fun canDownload(url: String?, posts: PostHandlers?) = false
    override fun canInline(url: String) = false
    override fun canLaunch(url: String?, posts: PostHandlers?) = isRedGifs(requireNotNull(url))
    override suspend fun download(url: String, posts: PostHandlers?) = throw UnsupportedOperationException()
    override fun inlineUrl(url: String): String? = null

    override suspend fun launch(url: String, posts: PostHandlers?) {
        val id = gifId(url)
        val gif = withContext(Dispatchers.IO) { getGif(id, url) }
        val urls = gif["urls"]?.jsonObject ?: throw IllegalStateException("RedGifs has no files for $url")
        val videoUrl = listOf("hd", "sd", "silent").firstNotNullOfOrNull { urls[it]?.jsonPrimitive?.contentOrNull?.takeIf(String::isNotBlank) }
            ?: throw IllegalStateException("RedGifs has no video file for $url")
        Pages.openVideo(services, FileDownload.of(videoUrl))
    }

    /** The gif's id: the last path segment of /watch/<id>, /ifr/<id>, ..., lowercased as RedGifs' API wants it. */
    private fun gifId(url: String): String {
        val path = url.substringBefore('#').substringBefore('?').substringAfter("://").substringAfter('/', "")
        val segment = path.split('/').lastOrNull { it.isNotBlank() } ?: throw IllegalArgumentException("No RedGifs id in $url")
        return segment.substringBefore('.').substringBefore('-').lowercase()
    }

    /** GET /v2/gifs/<id>'s "gif" object, fetching a temporary token first and again if it has expired. */
    private suspend fun getGif(id: String, url: String): JsonObject {
        for (attempt in 0..1) {
            val bearer = tokenLock.withLock { token ?: fetchToken().also { token = it } }
            val request = Request.Builder().url("https://api.redgifs.com/v2/gifs/$id").header("Authorization", "Bearer $bearer").build()
            services.http.newCall(request).execute().use { response ->
                when {
                    response.isSuccessful -> return json.parseToJsonElement(response.body!!.string()).jsonObject["gif"]?.jsonObject
                        ?: throw IllegalStateException("RedGifs answered without the gif for $url")
                    (response.code == 401 || response.code == 403) && attempt == 0 -> tokenLock.withLock { if (token == bearer) token = null }
                    response.code == 404 || response.code == 410 -> throw IllegalStateException("RedGifs no longer has $url (HTTP ${response.code})")
                    else -> throw IllegalStateException("RedGifs answered HTTP ${response.code} for $url")
                }
            }
        }
        throw IllegalStateException("RedGifs refused its own token for $url")
    }

    private fun fetchToken(): String =
        services.http.newCall(Request.Builder().url("https://api.redgifs.com/v2/auth/temporary").build()).execute().use { response ->
            if (!response.isSuccessful) throw IllegalStateException("RedGifs answered HTTP ${response.code} for a token")
            json.parseToJsonElement(response.body!!.string()).jsonObject["token"]?.jsonPrimitive?.contentOrNull
                ?: throw IllegalStateException("RedGifs answered without a token")
        }

    private fun isRedGifs(url: String): Boolean {
        // As in the C#, the MIME check runs first, so a string that isn't a URI throws here.
        if (UrlHelper.getMimeTypeFromUri(url).startsWith("image/")) return false
        return hostOf(url)?.let { DotNetStrings.endsWithOrdinalIgnoreCase(it, "redgifs.com") } == true
    }
}

/** Giphy pages and media: shown as the GIF itself. */
class GiphyHandler(private val services: Services) : UrlHandler {
    override fun canDownload(url: String?, posts: PostHandlers?) = isGiphy(url)
    override fun canInline(url: String) = isGiphy(url)
    override fun canLaunch(url: String?, posts: PostHandlers?) = isGiphy(url)
    override suspend fun download(url: String, posts: PostHandlers?) = FileDownload.of("giphy.gif", getDirectUrl(url))
    override fun inlineUrl(url: String): String = getDirectUrl(url)
    override suspend fun launch(url: String, posts: PostHandlers?) = Pages.openImages(services, listOf(FileDownload.of(getDirectUrl(url))))

    private fun getDirectUrl(url: String): String {
        // .NET's Uri: the host is lowercased, so "MEDIA.giphy.com" is a direct media URL too.
        val uri = DotNetUri.create(url)
        val host = uri.host

        // Already a direct media URL
        if (host.contains("media") || host == "i.giphy.com") return url

        // https://giphy.com/gifs/some-slug-ID -> extract ID (last segment after last hyphen)
        val lastSegment = uri.absolutePath.split('/').last()
        val id = if (lastSegment.contains('-')) lastSegment.substring(lastSegment.lastIndexOf('-') + 1) else lastSegment

        return "https://i.giphy.com/$id.gif"
    }

    // Uri.TryCreate answers false for null; Host.EndsWith(..., OrdinalIgnoreCase).
    private fun isGiphy(url: String?) = url != null && hostOf(url)?.let { DotNetStrings.endsWithOrdinalIgnoreCase(it, "giphy.com") } == true
}

/** Any link whose extension is an image type. */
class GenericImageHandler(private val services: Services) : UrlHandler {
    override fun canDownload(url: String?, posts: PostHandlers?) = isImage(url)
    override fun canInline(url: String) = isImage(url)
    override fun canLaunch(url: String?, posts: PostHandlers?) = isImage(url)
    override suspend fun download(url: String, posts: PostHandlers?) = FileDownload.of(UrlHelper.getFileName(url), url)
    override fun inlineUrl(url: String): String = url
    override suspend fun launch(url: String, posts: PostHandlers?) = Pages.openImages(services, listOf(FileDownload.of(url)))

    private fun isImage(url: String?): Boolean {
        if (url == null || !isAbsoluteUrl(url)) return false
        return UrlHelper.getMimeTypeFromUri(url).startsWith("image/")
    }
}

/** Any link whose extension is a video type. */
class GenericVideoHandler(private val services: Services) : UrlHandler {
    override fun canDownload(url: String?, posts: PostHandlers?) = isVideo(url)
    override fun canInline(url: String) = false
    override fun canLaunch(url: String?, posts: PostHandlers?) = isVideo(url)
    override suspend fun download(url: String, posts: PostHandlers?) = FileDownload.of(UrlHelper.getFileName(url), url)
    override fun inlineUrl(url: String): String? = null
    override suspend fun launch(url: String, posts: PostHandlers?) = Pages.openVideo(services, FileDownload.of(url))

    private fun isVideo(url: String?): Boolean {
        if (url == null || !isAbsoluteUrl(url)) return false
        return UrlHelper.getMimeTypeFromUri(url).startsWith("video/")
    }
}

/** Everything else that's a URL: the in-app browser. */
class GenericUrlHandler(private val services: Services) : UrlHandler {
    override fun canDownload(url: String?, posts: PostHandlers?) = false
    override fun canInline(url: String) = false
    override fun canLaunch(url: String?, posts: PostHandlers?) = url != null && isAbsoluteUrl(url)
    override suspend fun download(url: String, posts: PostHandlers?) = throw UnsupportedOperationException()
    override fun inlineUrl(url: String): String? = null
    override suspend fun launch(url: String, posts: PostHandlers?) = Pages.openBrowser(services, url)
}

/**
 * The URL's host, or null when it isn't an absolute URI: the C# `Uri.TryCreate(url, UriKind.Absolute, out uri)` then
 * `uri.Host`, with .NET's parsing (lowercased host, underscores allowed; "" for a URI without one).
 */
private fun hostOf(url: String): String? = DotNetUri.tryCreate(url)?.host
