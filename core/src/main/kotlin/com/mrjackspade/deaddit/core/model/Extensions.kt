package com.mrjackspade.deaddit.core.model

/** The post's link. Reddit sends a crosspost's as a path on its own site (/r/sub/comments/...). */
val Post.link: String?
    get() = url?.let { if (it.startsWith('/')) "https://www.reddit.com$it" else it }

/** The thumbnail to show: Reddit's own, or the first preview image. */
fun Post.thumbnailUrl(): String? {
    if (thumbnail?.contains("://") == true) return thumbnail
    return preview?.images?.firstOrNull()?.source?.url
}

val RedditThing.isDeleted: Boolean get() = author == "[deleted]"

val RedditThing.isRemoved: Boolean get() = author == "[removed]" || body == "[ Removed by Reddit ]"

/** The title for duplicate checks: posts' titles, nothing for other things. */
val RedditThing.titleOrEmpty: String get() = (this as? Post)?.title?.trim() ?: ""

/** The post's link when it's an absolute URL, for duplicate checks. */
val RedditThing.urlOrEmpty: String
    get() = (this as? Post)?.link?.takeIf(::isAbsoluteUrl) ?: ""

/** Whether [value] parses as an absolute URI (the C# app's Uri.TryCreate(..., Absolute)). */
fun isAbsoluteUrl(value: String): Boolean = try {
    java.net.URI(value).isAbsolute
} catch (_: java.net.URISyntaxException) {
    false
}
