package com.mrjackspade.deaddit.core.html

import com.mrjackspade.deaddit.core.model.MediaMetadata
import org.jsoup.nodes.Entities

/**
 * Comments posted with an image are rendered by Reddit the moment they're posted, while the image
 * is still processing: their body and body_html keep "*Processing img <id>...*" for good, even
 * once the image is ready. www.reddit.com draws images from media_metadata instead, which is why
 * they show there. This does the same: each placeholder whose image is now "valid" becomes a link
 * to it, rendered like any other image link in a comment.
 */
object ProcessingImages {
    private val PLACEHOLDER = Regex("""(?:<em>)?Processing img ([A-Za-z0-9]+)\.\.\.(?:</em>)?""")

    /** [html] is body_html (plain or HTML-escaped, as RedditHtml takes it). */
    fun resolve(html: String, media: Map<String, MediaMetadata>?): String {
        if (media.isNullOrEmpty()) return html

        // Placeholders sit in the markup itself, so work on it unescaped.
        val plain = if (html.trimStart().startsWith("&lt;")) Entities.unescape(html) else html

        return PLACEHOLDER.replace(plain) { match ->
            val url = media[match.groupValues[1]]?.let(::urlOf) ?: return@replace match.value
            val escaped = Entities.escape(url)
            "<a href=\"$escaped\">$escaped</a>"
        }
    }

    /** The image, once Reddit has it: the GIF itself for animations, otherwise the full-size image. */
    private fun urlOf(item: MediaMetadata): String? {
        if (item.status != "valid") return null
        val source = item.source ?: return null
        return if (item.kind == "AnimatedImage") source.gif ?: source.url else source.url ?: source.gif
    }
}
