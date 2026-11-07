package com.mrjackspade.deaddit.handlers.url

import com.mrjackspade.deaddit.Services
import com.mrjackspade.deaddit.core.model.Comment
import com.mrjackspade.deaddit.core.service.CommentFocus
import com.mrjackspade.deaddit.handlers.FileDownload
import com.mrjackspade.deaddit.handlers.PostHandlers
import com.mrjackspade.deaddit.core.util.DotNetHttpHeaders
import com.mrjackspade.deaddit.handlers.UrlHandler
import com.mrjackspade.deaddit.media.fetch
import com.mrjackspade.deaddit.ui.Pages
import okhttp3.Request

/**
 * A video embedded in a comment. Reddit writes it into the body as a placeholder link,
 * reddit.com/link/{commentId}/video/{mediaId}/player, which only reddit's own site turns into
 * a player; visiting it lands on a "subreddit is banned" page. The media id is the
 * v.redd.it video, so play that directly.
 */
class RedditInlineVideoUrlHandler(private val services: Services) : UrlHandler {
    override fun canDownload(url: String?, posts: PostHandlers?) = isInlineVideo(url)
    override fun canInline(url: String) = false
    override fun canLaunch(url: String?, posts: PostHandlers?) = isInlineVideo(url)
    override suspend fun download(url: String, posts: PostHandlers?) = getDownload(url)
    override fun inlineUrl(url: String): String? = null
    override suspend fun launch(url: String, posts: PostHandlers?) = Pages.openVideo(services, getDownload(url))

    private companion object {
        val INLINE_VIDEO = Regex("^https?://(?:[a-z]+\\.)?reddit\\.com/link/[a-z0-9]+/video/([a-z0-9]+)/player/?$", RegexOption.IGNORE_CASE)

        fun getDownload(url: String): FileDownload {
            val match = INLINE_VIDEO.find(url) ?: throw IllegalArgumentException("Not a reddit inline video url: $url")
            val mediaId = match.groupValues[1]
            val dashUrl = "https://v.redd.it/$mediaId/DASHPlaylist.mpd"
            return FileDownload("$mediaId.mp4", dashUrl, dashUrl)
        }

        fun isInlineVideo(url: String?) = url != null && INLINE_VIDEO.containsMatchIn(url)
    }
}

/** Links to a thread (and optionally a comment in it) open the thread in the app. */
class RedditCommentsPostUrlHandler(private val services: Services) : UrlHandler {
    override fun canDownload(url: String?, posts: PostHandlers?) = false
    override fun canInline(url: String) = false

    override fun canLaunch(url: String?, posts: PostHandlers?): Boolean {
        if (posts == null) return false
        // Regex.IsMatch threw on a null URL in the C#.
        return COMMENTS.containsMatchIn(requireNotNull(url))
    }

    override suspend fun download(url: String, posts: PostHandlers?) = throw UnsupportedOperationException()
    override fun inlineUrl(url: String): String? = null

    override suspend fun launch(url: String, posts: PostHandlers?) {
        requireNotNull(posts)

        // Match post ID and optional comment ID: /r/sub/comments/postid/slug/commentid/
        val match = POST_AND_COMMENT.find(url)
        val postId = match?.groupValues?.get(1) ?: ""
        val commentId = match?.groups?.get(2)?.value

        val post = services.reddit.getPost(postId) ?: throw IllegalStateException("Reddit returned no post for id '$postId'")
        // As the C# `new ApiComment { Id = commentId }`: only the ID, no name. The thread is fetched by that ID, and
        // the focus is then only the parent its top-level comments and "load more"s hang from, which reads no name.
        val focus = commentId?.let { CommentFocus(Comment(id = it, name = "")) }

        Pages.openPost(services, post, focus)
    }

    private companion object {
        val COMMENTS = Regex(".*/r\\/[^\\/]+\\/comments\\/[^\\/]+\\/[^\\/]+\\/?.*$")
        val POST_AND_COMMENT = Regex("\\/r\\/[^\\/]+\\/comments\\/([a-z0-9]+)(?:\\/[^\\/]*\\/([a-z0-9]+))?", RegexOption.IGNORE_CASE)
    }
}

/** Reddit's share links (/r/sub/s/xyz) redirect to the real thread: follow that one redirect, then open where it points. */
class RedditSPostUrlHandler(private val services: Services) : UrlHandler {
    override fun canDownload(url: String?, posts: PostHandlers?) = false
    override fun canInline(url: String) = false

    override fun canLaunch(url: String?, posts: PostHandlers?): Boolean {
        if (posts == null) return false
        return SHARE_LINK.containsMatchIn(requireNotNull(url))
    }

    override suspend fun download(url: String, posts: PostHandlers?) = throw UnsupportedOperationException()
    override fun inlineUrl(url: String): String? = null

    override suspend fun launch(url: String, posts: PostHandlers?) {
        val caller = requireNotNull(posts)

        val request = Request.Builder().url(url).header("User-Agent", "Deaddit").build()
        // `Headers.Location.ToString()`: the target as .NET's Uri displays it, %-escapes decoded but for '#', '?' and '%'.
        // Location holds one value, so of several .NET reads the first; one it can't parse threw (a null Location).
        val newUrl = services.httpNoRedirects.fetch(request) { response ->
            val location = response.headers("Location").firstOrNull()
                ?: throw IllegalStateException("Reddit sent no redirect for $url (HTTP ${response.code})")
            DotNetHttpHeaders.location(location)
                ?: throw IllegalStateException("Reddit's redirect for $url is not a URI .NET can parse: '$location'")
        }

        if (newUrl != url) {
            caller.urls.launch(newUrl, caller)
        }
    }

    private companion object {
        val SHARE_LINK = Regex(".*/r\\/[^\\/]+\\/s\\/[^\\/]+/?.*$", RegexOption.IGNORE_CASE)
    }
}

/** Links to a subreddit open it in the app. */
class SubredditUrlHandler(private val services: Services) : UrlHandler {
    override fun canDownload(url: String?, posts: PostHandlers?) = false
    override fun canInline(url: String) = false

    override fun canLaunch(url: String?, posts: PostHandlers?): Boolean {
        if (posts == null) return false
        return Regex(SUBREDDIT).containsMatchIn(requireNotNull(url))
    }

    override suspend fun download(url: String, posts: PostHandlers?) = throw UnsupportedOperationException()
    override fun inlineUrl(url: String): String? = null

    override suspend fun launch(url: String, posts: PostHandlers?) {
        requireNotNull(posts)
        val id = Regex(SUBREDDIT, RegexOption.IGNORE_CASE).find(url)?.groupValues?.get(1) ?: ""
        Pages.openSubreddit(services, id)
    }

    private companion object {
        const val SUBREDDIT = "\\/r\\/([a-zA-Z0-9_]+)/?$"
    }
}

/** Links to a user open their profile in the app. */
class UserUrlHandler(private val services: Services) : UrlHandler {
    override fun canDownload(url: String?, posts: PostHandlers?) = false
    override fun canInline(url: String) = false

    override fun canLaunch(url: String?, posts: PostHandlers?): Boolean {
        if (posts == null) return false
        return Regex(USER).containsMatchIn(requireNotNull(url))
    }

    override suspend fun download(url: String, posts: PostHandlers?) = throw UnsupportedOperationException()
    override fun inlineUrl(url: String): String? = null

    override suspend fun launch(url: String, posts: PostHandlers?) {
        requireNotNull(posts)
        val id = Regex(USER, RegexOption.IGNORE_CASE).find(url)?.groupValues?.get(1) ?: ""
        Pages.openUser(services, id)
    }

    private companion object {
        const val USER = "\\/u\\/([a-zA-Z0-9_]+)/?$"
    }
}
