package com.mrjackspade.deaddit.handlers

import com.mrjackspade.deaddit.core.model.Post
import com.mrjackspade.deaddit.core.model.link

/** A file behind a post or link: what to call it, what to show, and what to save. */
data class FileDownload(val fileName: String, val launchUrl: String, val downloadUrl: String) {
    init {
        require(fileName.isNotBlank()) { "A download needs a file name." }
        require(launchUrl.isNotBlank()) { "A download needs a URL to show." }
        require(downloadUrl.isNotBlank()) { "A download needs a URL to save." }
    }

    companion object {
        fun of(fileName: String, url: String) = FileDownload(fileName, url, url)

        /** Named after the URL's last path segment, without its query. */
        fun of(url: String) = FileDownload(url.substringAfterLast('/').substringBefore('?'), url, url)
    }
}

/** Handles one kind of link (an image host, a subreddit link, ...). Order matters: first match wins. */
interface UrlHandler {
    fun canDownload(url: String?, posts: PostHandlers?): Boolean
    fun canInline(url: String): Boolean
    fun canLaunch(url: String?, posts: PostHandlers?): Boolean
    suspend fun download(url: String, posts: PostHandlers?): FileDownload
    fun inlineUrl(url: String): String?
    suspend fun launch(url: String, posts: PostHandlers?)
}

/** Handles one kind of post (self, gallery, Reddit video, crosspost). Order matters: first match wins. */
interface PostHandler {
    fun canDownload(post: Post, caller: PostHandlers): Boolean
    fun canLaunch(post: Post, caller: PostHandlers): Boolean
    fun canShare(post: Post, caller: PostHandlers): Boolean
    suspend fun download(post: Post, caller: PostHandlers)
    suspend fun launch(post: Post, caller: PostHandlers)
    suspend fun share(post: Post, caller: PostHandlers)
}

/** The URL handlers, tried in order (the C# AggregateUrlHandler). */
class UrlHandlers(private val handlers: List<UrlHandler>) {
    fun canDownload(url: String?, posts: PostHandlers?) = handlers.any { it.canDownload(url, posts) }

    fun canInline(url: String) = handlers.any { it.canInline(url) }

    fun canLaunch(url: String?, posts: PostHandlers?) = handlers.any { it.canLaunch(url, posts) }

    suspend fun download(url: String?, posts: PostHandlers?): FileDownload {
        val handler = handlers.firstOrNull { it.canDownload(url, posts) } ?: throw UnsupportedOperationException("Nothing can download $url")
        return handler.download(url!!, posts)
    }

    fun inlineUrl(url: String): String? = handlers.firstOrNull { it.canInline(url) }?.inlineUrl(url)

    suspend fun launch(url: String?, posts: PostHandlers?) {
        val handler = handlers.firstOrNull { it.canLaunch(url, posts) } ?: throw UnsupportedOperationException("Nothing can open $url")
        handler.launch(url!!, posts)
    }
}

/** The post handlers, then the URL handlers for the post's link (the C# AggregatePostHandler). */
class PostHandlers(private val handlers: List<PostHandler>, val urls: UrlHandlers, private val saveDownload: suspend (FileDownload) -> Unit) {
    fun canDownload(post: Post) = handlers.any { it.canDownload(post, this) } || urls.canDownload(post.link, this)

    fun canLaunch(post: Post): Boolean {
        if (post.link.isNullOrBlank()) return false
        return urls.canLaunch(post.link, this) || handlers.any { it.canLaunch(post, this) }
    }

    fun canShare(post: Post) = handlers.any { it.canShare(post, this) }

    suspend fun download(post: Post) {
        handlers.firstOrNull { it.canDownload(post, this) }?.let { return it.download(post, this) }
        if (urls.canDownload(post.link, this)) return saveDownload(urls.download(post.link, this))
        throw UnsupportedOperationException("Nothing can download ${post.name}")
    }

    suspend fun launch(post: Post) {
        handlers.firstOrNull { it.canLaunch(post, this) }?.let { return it.launch(post, this) }
        if (urls.canLaunch(post.link, this)) return urls.launch(post.link, this)
        throw UnsupportedOperationException("Nothing can open ${post.name}")
    }

    suspend fun share(post: Post) {
        val handler = handlers.firstOrNull { it.canShare(post, this) } ?: throw UnsupportedOperationException("Nothing can share ${post.name}")
        handler.share(post, this)
    }
}

/** Crossposts are handled as the post they repost. */
class CrossPostHandler : PostHandler {
    override fun canDownload(post: Post, caller: PostHandlers) = post.crosspostParentList.any { caller.canDownload(it) }
    override fun canLaunch(post: Post, caller: PostHandlers) = post.crosspostParentList.any { caller.canLaunch(it) }
    override fun canShare(post: Post, caller: PostHandlers) = post.crosspostParentList.any { caller.canShare(it) }

    override suspend fun download(post: Post, caller: PostHandlers) {
        post.crosspostParentList.firstOrNull { caller.canDownload(it) }?.let { caller.download(it) }
    }

    override suspend fun launch(post: Post, caller: PostHandlers) {
        post.crosspostParentList.firstOrNull { caller.canLaunch(it) }?.let { caller.launch(it) }
    }

    override suspend fun share(post: Post, caller: PostHandlers) {
        post.crosspostParentList.firstOrNull { caller.canShare(it) }?.let { caller.share(it) }
    }
}
