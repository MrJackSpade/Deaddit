package com.mrjackspade.deaddit.core.model

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable
import kotlinx.serialization.Transient
import kotlinx.serialization.json.JsonElement
import java.time.Instant

enum class VoteState(val direction: Int) { Upvote(1), None(0), Downvote(-1) }

/** Reddit.Api's DistinguishedKind: null is its Null, [Empty] is "". */
@Serializable(DistinguishedSerializer::class)
enum class Distinguished { Empty, Moderator, Admin }

object DistinguishedSerializer : EmptyStringEnumSerializer<Distinguished>(
    "Distinguished",
    Distinguished.Empty,
    mapOf(Distinguished.Moderator to "moderator", Distinguished.Admin to "admin"),
)

/** Reddit.Api's CollapsedReasonCode: null is its Null (not collapsed), [Empty] is "". */
@Serializable(CollapsedReasonCodeSerializer::class)
enum class CollapsedReasonCode { Empty, ScoreBelowThreshold, LowScore, Deleted, BlockedAuthor }

object CollapsedReasonCodeSerializer : EmptyStringEnumSerializer<CollapsedReasonCode>(
    "CollapsedReasonCode",
    CollapsedReasonCode.Empty,
    mapOf(
        CollapsedReasonCode.ScoreBelowThreshold to "score-below-threshold",
        CollapsedReasonCode.LowScore to "low_score",
        CollapsedReasonCode.Deleted to "deleted",
        CollapsedReasonCode.BlockedAuthor to "BLOCKED_AUTHOR",
    ),
)

/** A part of a richtext flair: text, or an emoji image. */
@Serializable
data class FlairPart(
    @SerialName("e") val type: String,
    @SerialName("t") val text: String? = null,
    @SerialName("a") val alias: String? = null,
    @SerialName("u") val url: String? = null,
)

/** The fields posts, comments and messages share. */
sealed interface RedditThing {
    val id: String
    val name: String
    val author: String?
    val authorFullname: String?
    val body: String
    val subreddit: String?
    val createdUtc: Instant?
}

/** A preview image. Reddit.Api's model decodes its URL. */
@Serializable
data class ImageSource(val url: String? = null, val width: Int = 0, val height: Int = 0)

@Serializable
data class PreviewImage(
    val id: String? = null,
    val source: ImageSource? = null,
    val resolutions: List<ImageSource> = emptyList(),
    val variants: ImageVariants? = null,
)

@Serializable
data class ImageVariants(val gif: PreviewImage? = null, val mp4: PreviewImage? = null, val nsfw: PreviewImage? = null, val obfuscated: PreviewImage? = null)

@Serializable
data class RedditVideo(
    @SerialName("dash_url") @Serializable(HtmlDecodedUrlSerializer::class) val dashUrl: String? = null,
    @SerialName("hls_url") @Serializable(HtmlDecodedUrlSerializer::class) val hlsUrl: String? = null,
    @SerialName("fallback_url") @Serializable(HtmlDecodedUrlSerializer::class) val fallbackUrl: String? = null,
    val width: Int? = null,
    val height: Int? = null,
    val duration: Int? = null,
    @SerialName("is_gif") val isGif: Boolean = false,
)

@Serializable
data class Preview(
    val enabled: Boolean = false,
    val images: List<PreviewImage> = emptyList(),
    @SerialName("reddit_video_preview") val redditVideoPreview: RedditVideo? = null,
)

@Serializable
data class OEmbed(
    val type: String? = null,
    val html: String? = null,
    val title: String? = null,
    @SerialName("thumbnail_url") @Serializable(HtmlDecodedUrlSerializer::class) val thumbnailUrl: String? = null,
    @SerialName("provider_name") val providerName: String? = null,
)

@Serializable
data class Media(
    val type: String? = null,
    @SerialName("reddit_video") val redditVideo: RedditVideo? = null,
    val oembed: OEmbed? = null,
)

@Serializable
data class GalleryItem(
    @SerialName("media_id") val mediaId: String,
    val caption: String? = null,
    @SerialName("outbound_url") @Serializable(HtmlDecodedUrlSerializer::class) val outboundUrl: String? = null,
)

@Serializable
data class GalleryData(val items: List<GalleryItem> = emptyList())

/** An image or video in a gallery, or inline in a comment. "m" is its MIME type, "e" its kind. */
@Serializable
data class MediaMetadata(
    val id: String? = null,
    val status: String? = null,
    @SerialName("e") val kind: String? = null,
    @SerialName("m") val mimeType: String? = null,
    @SerialName("s") val source: MediaMetadataSource? = null,
    @SerialName("p") val previews: List<MediaMetadataSource> = emptyList(),
)

/** "u" is an image, "gif"/"mp4" an animation, each with its size. Reddit.Api's model decodes the URLs. */
@Serializable
data class MediaMetadataSource(
    @SerialName("u") val url: String? = null,
    val gif: String? = null,
    val mp4: String? = null,
    @SerialName("x") val width: Int = 0,
    @SerialName("y") val height: Int = 0,
)

@Serializable
data class Post(
    override val id: String,
    override val name: String,
    val title: String = "",
    override val author: String? = null,
    @SerialName("author_fullname") override val authorFullname: String? = null,
    @SerialName("author_flair_text") @Serializable(BlankAsNullSerializer::class) val authorFlairText: String? = null,
    @SerialName("author_flair_background_color") @Serializable(JsonColorSerializer::class) val authorFlairBackgroundColor: String? = null,
    @SerialName("author_flair_text_color") @Serializable(BlankAsNullSerializer::class) val authorFlairTextColor: String? = null,
    @SerialName("author_flair_richtext") val authorFlairRichtext: List<FlairPart> = emptyList(),
    @SerialName("selftext") override val body: String = "",
    @SerialName("selftext_html") val bodyHtml: String? = null,
    val domain: String? = null,
    override val subreddit: String? = null,
    @SerialName("subreddit_id") val subredditId: String? = null,
    val score: Int = 0,
    @SerialName("num_comments") val numComments: Int = 0,
    @SerialName("created_utc") @Serializable(RedditInstantSerializer::class) override val createdUtc: Instant? = null,
    val permalink: String? = null,
    @Serializable(HtmlDecodedUrlSerializer::class) val url: String? = null,
    @SerialName("is_self") val isSelf: Boolean = false,
    @SerialName("is_video") val isVideo: Boolean = false,
    @SerialName("is_gallery") @Serializable(JsonBoolSerializer::class) val isGallery: Boolean = false,
    @SerialName("over_18") val isNsfw: Boolean = false,
    val spoiler: Boolean = false,
    val hidden: Boolean = false,
    val saved: Boolean = false,
    val clicked: Boolean = false,
    val visited: Boolean = false,
    val locked: Boolean = false,
    val archived: Boolean = false,
    val stickied: Boolean = false,
    @SerialName("send_replies") val sendReplies: Boolean = false,
    @Serializable(VoteStateSerializer::class) val likes: VoteState = VoteState.None,
    val distinguished: Distinguished? = null,
    @SerialName("link_flair_text") @Serializable(BlankAsNullSerializer::class) val linkFlairText: String? = null,
    @SerialName("link_flair_background_color") @Serializable(JsonColorSerializer::class) val linkFlairBackgroundColor: String? = null,
    @SerialName("link_flair_text_color") @Serializable(BlankAsNullSerializer::class) val linkFlairTextColor: String? = null,
    @SerialName("link_flair_richtext") val linkFlairRichtext: List<FlairPart> = emptyList(),
    @Serializable(HtmlDecodedUrlSerializer::class) val thumbnail: String? = null,
    val media: Media? = null,
    @SerialName("secure_media") val secureMedia: Media? = null,
    val preview: Preview? = null,
    @SerialName("gallery_data") val galleryData: GalleryData? = null,
    @SerialName("media_metadata") val mediaMetadata: Map<String, MediaMetadata>? = null,
    @SerialName("crosspost_parent_list") val crosspostParentList: List<Post> = emptyList(),
    val category: String? = null,
    @SerialName("content_categories") val contentCategories: List<String> = emptyList(),
) : RedditThing

@Serializable
data class Comment(
    override val id: String,
    override val name: String,
    override val author: String? = null,
    @SerialName("author_fullname") override val authorFullname: String? = null,
    @SerialName("author_flair_text") @Serializable(BlankAsNullSerializer::class) val authorFlairText: String? = null,
    @SerialName("author_flair_background_color") @Serializable(JsonColorSerializer::class) val authorFlairBackgroundColor: String? = null,
    @SerialName("author_flair_text_color") @Serializable(BlankAsNullSerializer::class) val authorFlairTextColor: String? = null,
    @SerialName("author_flair_richtext") val authorFlairRichtext: List<FlairPart> = emptyList(),
    override val body: String = "",
    @SerialName("body_html") val bodyHtml: String? = null,
    val score: Int = 0,
    /** Null when Reddit leaves it out; the C# app hid the score then too. */
    @SerialName("score_hidden") val scoreHidden: Boolean? = null,
    @SerialName("created_utc") @Serializable(RedditInstantSerializer::class) override val createdUtc: Instant? = null,
    @Serializable(RedditInstantSerializer::class) val edited: Instant? = null,
    val permalink: String? = null,
    @SerialName("parent_id") val parentId: String? = null,
    @SerialName("link_id") val linkId: String? = null,
    @SerialName("link_title") val linkTitle: String? = null,
    override val subreddit: String? = null,
    @SerialName("subreddit_id") val subredditId: String? = null,
    @Serializable(VoteStateSerializer::class) val likes: VoteState = VoteState.None,
    val saved: Boolean = false,
    val locked: Boolean = false,
    val archived: Boolean = false,
    val stickied: Boolean = false,
    @SerialName("send_replies") val sendReplies: Boolean = false,
    @SerialName("is_submitter") val isSubmitter: Boolean = false,
    val distinguished: Distinguished? = null,
    @SerialName("collapsed_reason_code") val collapsedReasonCode: CollapsedReasonCode? = null,
    @SerialName("media_metadata") val mediaMetadata: Map<String, MediaMetadata>? = null,
    /** Inbox only: whether it's unread, and the link to it in context. */
    val new: Boolean = false,
    val context: String? = null,
    /** Read from www's HTML: whether www can translate it, and whether this is its translation. */
    val translatable: Boolean = false,
    val translated: Boolean = false,
    /** When this is www's translation, the comment as written, to switch back to. */
    @Transient val original: CommentText? = null,
    @Serializable(RawJsonSerializer::class) val replies: JsonElement? = null,
) : RedditThing

@Serializable
data class Message(
    override val id: String,
    override val name: String,
    override val author: String? = null,
    @SerialName("author_fullname") override val authorFullname: String? = null,
    val subject: String = "",
    override val body: String = "",
    @SerialName("body_html") val bodyHtml: String? = null,
    val dest: String? = null,
    override val subreddit: String? = null,
    @SerialName("created_utc") @Serializable(RedditInstantSerializer::class) override val createdUtc: Instant? = null,
    val new: Boolean = false,
    val context: String? = null,
    @SerialName("was_comment") val wasComment: Boolean = false,
    val distinguished: Distinguished? = null,
) : RedditThing

/** A comment's text: its markdown and its HTML. */
data class CommentText(val body: String, val bodyHtml: String?)

/**
 * A "load more" placeholder. From www's HTML, [moreUrl] and [moreCursor] load it; from Reddit's
 * JSON, [children]. Neither means "continue this thread". [count] is null when Reddit doesn't give
 * one (www's later top-level pages).
 */
@Serializable
data class More(
    val id: String,
    val name: String,
    @SerialName("parent_id") val parentId: String? = null,
    val count: Int? = null,
    val depth: Int = 0,
    val children: List<String> = emptyList(),
    @SerialName("more_url") val moreUrl: String? = null,
    @SerialName("more_cursor") val moreCursor: String? = null,
) {
    /** Whether it loads comments in place; otherwise it's "continue this thread". */
    val loadsInPlace: Boolean get() = moreUrl != null || children.isNotEmpty()
}
