package com.mrjackspade.deaddit.handlers.url

import com.mrjackspade.deaddit.Services
import com.mrjackspade.deaddit.core.model.isAbsoluteUrl
import com.mrjackspade.deaddit.core.util.UrlHelper
import com.mrjackspade.deaddit.handlers.FileDownload
import com.mrjackspade.deaddit.handlers.PostHandlers
import com.mrjackspade.deaddit.handlers.UrlHandler
import com.mrjackspade.deaddit.ui.Pages
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import okhttp3.Request
import java.net.URI

/**
 * RedGifs pages: finds the video file's properly capitalized name in the page, then plays the
 * first of the known file URLs the page mentions (or, failing that, that exists).
 */
class RedGifsHandler(private val services: Services) : UrlHandler {
    // RedGifs requires URL resolution, not directly downloadable
    override fun canDownload(url: String?, posts: PostHandlers?) = false
    override fun canInline(url: String) = false
    override fun canLaunch(url: String?, posts: PostHandlers?) = isRedGifs(requireNotNull(url))
    override suspend fun download(url: String, posts: PostHandlers?) = throw UnsupportedOperationException()
    override fun inlineUrl(url: String): String? = null

    override suspend fun launch(url: String, posts: PostHandlers?) {
        var name = url.split('/').last()

        if (name.contains('?')) {
            name = name.split('?').first()
        }

        val source = withContext(Dispatchers.IO) {
            services.http.newCall(Request.Builder().url(url).build()).execute().use { response ->
                if (!response.isSuccessful) throw IllegalStateException("RedGifs answered HTTP ${response.code} for $url")
                response.body!!.string()
            }
        }

        var properlyCapitalized = ""

        for (chunk in source.split("\"").drop(1)) {
            if (!isAbsoluteUrl(chunk)) continue

            if ((chunk.contains("files.redgifs.com", ignoreCase = true) || chunk.contains("media.redgifs.com", ignoreCase = true)) &&
                chunk.contains(name, ignoreCase = true)
            ) {
                val indexOfName = chunk.indexOf(name, ignoreCase = true)
                properlyCapitalized = chunk.substring(indexOfName, indexOfName + name.length)
                break
            }
        }

        val newUrls = listOf(
            "https://files.redgifs.com/$properlyCapitalized.mp4",
            "https://files.redgifs.com/$properlyCapitalized-mobile.mp4",
            "https://files.redgifs.com/$properlyCapitalized-silent.mp4",
            "https://media.redgifs.com/$properlyCapitalized.mp4",
            "https://media.redgifs.com/$properlyCapitalized-mobile.mp4",
            "https://media.redgifs.com/$properlyCapitalized-silent.mp4",
        )

        var newUrl = newUrls.firstOrNull { source.contains(it) } ?: ""

        if (newUrl.isEmpty()) {
            for (candidate in newUrls) {
                if (exists(candidate)) {
                    newUrl = candidate
                    break
                }
            }
        }

        Pages.openVideo(services, FileDownload.of(newUrl))
    }

    /** Whether a HEAD request for [url] succeeds. Network failures propagate (the C# swallowed them as "doesn't exist"). */
    private suspend fun exists(url: String): Boolean = withContext(Dispatchers.IO) {
        services.http.newCall(Request.Builder().url(url).head().build()).execute().use { it.isSuccessful }
    }

    private fun isRedGifs(url: String): Boolean {
        // As in the C#, the MIME check runs first, so a string that isn't a URI throws here.
        if (UrlHelper.getMimeTypeFromUri(url).startsWith("image/")) return false
        return hostOf(url)?.endsWith("redgifs.com", ignoreCase = true) == true
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
        val uri = URI(url)
        val host = uri.host ?: ""

        // Already a direct media URL
        if (host.contains("media") || host == "i.giphy.com") return url

        // https://giphy.com/gifs/some-slug-ID -> extract ID (last segment after last hyphen)
        val lastSegment = (uri.rawPath ?: "").split('/').last()
        val id = if (lastSegment.contains('-')) lastSegment.substring(lastSegment.lastIndexOf('-') + 1) else lastSegment

        return "https://i.giphy.com/$id.gif"
    }

    private fun isGiphy(url: String?) = url != null && isAbsoluteUrl(url) && hostOf(url)?.endsWith("giphy.com", ignoreCase = true) == true
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

/** The URL's host, or null when it has none (the C# Uri.TryCreate(..., Absolute) then Uri.Host). */
private fun hostOf(url: String): String? {
    if (!isAbsoluteUrl(url)) return null
    return URI(url).host
}
