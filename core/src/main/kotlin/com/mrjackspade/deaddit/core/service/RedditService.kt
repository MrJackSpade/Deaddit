package com.mrjackspade.deaddit.core.service

import com.mrjackspade.deaddit.core.api.ListingItem
import com.mrjackspade.deaddit.core.api.ListingParameters
import com.mrjackspade.deaddit.core.api.RedditApi
import com.mrjackspade.deaddit.core.config.ApplicationHacks
import com.mrjackspade.deaddit.core.config.Region
import com.mrjackspade.deaddit.core.feed.Feed
import com.mrjackspade.deaddit.core.feed.Sort
import com.mrjackspade.deaddit.core.markdown.HtmlMarkdownConverter
import com.mrjackspade.deaddit.core.markdown.MarkdownAnalyzer
import com.mrjackspade.deaddit.core.markdown.MarkdownRichTextConverter
import com.mrjackspade.deaddit.core.model.Comment
import com.mrjackspade.deaddit.core.model.CommentText
import com.mrjackspade.deaddit.core.model.Multi
import com.mrjackspade.deaddit.core.model.Post
import com.mrjackspade.deaddit.core.model.RedditThing
import com.mrjackspade.deaddit.core.model.Subreddit
import com.mrjackspade.deaddit.core.model.SubredditRules
import com.mrjackspade.deaddit.core.model.User
import com.mrjackspade.deaddit.core.model.UserPartialData
import com.mrjackspade.deaddit.core.model.VoteState
import com.mrjackspade.deaddit.core.thread.CommentTree
import com.mrjackspade.deaddit.core.thread.MoreNode
import com.mrjackspade.deaddit.core.thread.ThreadNode

/** A comment to open a thread at, with how many of its parents to show above it. */
data class CommentFocus(val comment: Comment, val context: Int = MAX_CONTEXT) {
    companion object {
        /** The most context Reddit's API gives. */
        const val MAX_CONTEXT = 8
    }
}

class NotLoggedInException(message: String) : IllegalStateException(message)

/** The language www translates comments in other languages into. */
private const val TRANSLATE_INTO = "en"

/**
 * The app's Reddit operations (the C# app's RedditService): logs in as each call needs, applies
 * the user's outgoing-text settings, and shapes results for the screens.
 */
class RedditService(private val api: RedditApi, private val hacks: () -> ApplicationHacks) {

    /** The session's user, expired or not; null without a session (the C# LoggedInUser: Reddit.Api's AuthenticatedUser). */
    val loggedInUser: String? get() = api.session?.authenticatedUser

    /** Whether there's a session that hasn't expired, checked at each read (the C# IsLoggedIn: Reddit.Api's IsAuthenticated). */
    val isLoggedIn: Boolean get() = api.session?.isExpired == false

    /** Checks that the session mirrored in [RedditApi.session] is the one Reddit.Api holds; a mismatch is a bug. */
    suspend fun refreshUser() {
        val native = api.authenticatedUser()
        check(native == loggedInUser) { "Reddit.Api's session is for '$native', but the app's mirror of it is for '$loggedInUser'." }
    }

    /** One page of a feed. Things are listed in Reddit's order; [Feed.User] falls back to search when empty. */
    suspend fun getPosts(feed: Feed, sort: Sort?, pageSize: Int, after: String?, region: Region): List<RedditThing> {
        api.tryAuthenticate()

        val parameters = ListingParameters(
            after = after,
            limit = minOf(100, pageSize),
            geo = if (region != Region.GLOBAL) region.name else null,
        )

        var endpoint = feed.endpoint.trimStart('/')

        if (endpoint.contains("%USER%")) {
            endpoint = endpoint.replace("%USER%", loggedInUser ?: throw NotLoggedInException("You must be logged in to view this content."))
        }

        val fullEndpoint = if (sort != null) {
            val sortName = sort.label.lowercase()
            if (endpoint.isEmpty()) "/$sortName" else "/$endpoint/$sortName"
        } else {
            if (endpoint.isEmpty()) "/" else "/$endpoint"
        }

        // Reddit blocks these sorts as JSON listings, so the client has its own path for them.
        var things: List<RedditThing> = if (sort?.label == "New" || sort?.label == "Rising") {
            api.getSortedPosts(fullEndpoint, parameters).items.map(::thingOf)
        } else {
            api.getListing(fullEndpoint, parameters).items.map(::thingOf)
        }

        // An empty profile can still have posts: search finds them.
        if (things.isEmpty() && after == null && endpoint.startsWith("user/") && !fullEndpoint.endsWith("/comments")) {
            val username = endpoint.split('/')[1]
            things = api.search("author:$username", sort = "new", time = "all", limit = minOf(100, pageSize)).items.map(::thingOf)
        }

        return things.take(pageSize)
    }

    /**
     * A post's comment tree, from www's HTML; focused on [focus] and its parents when given (www
     * sends all of a focused comment's parents, so [CommentFocus.context] doesn't apply).
     */
    suspend fun comments(post: Post, focus: CommentFocus? = null): List<ThreadNode> {
        api.ensureAuthenticated()
        val subreddit = requireNotNull(post.subreddit) { "${post.name} has no subreddit." }
        val load = { translate: String? -> suspend { api.getThreadComments(subreddit, post.name, focus?.comment?.name, translate) } }
        val items = load(null)()
        return CommentTree.fromComments(items, post.id, focus?.comment, prepare(translations(items, load(TRANSLATE_INTO))))
    }

    /** The comments behind a "load more", nested, to put in its place. */
    suspend fun moreComments(post: Post, more: MoreNode): List<ThreadNode> {
        api.ensureAuthenticated()
        if (!more.more.loadsInPlace) return emptyList()

        val subreddit = requireNotNull(post.subreddit) { "${post.name} has no subreddit." }
        val parent = more.parent?.name ?: post.name
        val load = { translate: String? -> suspend { api.getMoreComments(more.more, post.name, subreddit, parent, translate) } }
        val items = load(null)()
        return CommentTree.fromComments(items, post.id, more.parent, prepare(translations(items, load(TRANSLATE_INTO))))
    }

    /**
     * www's translations of the comments in [items] that are in another language, by name: loaded
     * again translated, only when there are any.
     */
    private suspend fun translations(items: List<ListingItem>, loadTranslated: suspend () -> List<ListingItem>): Map<String, Comment> {
        val translatable = CommentTree.allComments(items).filter { it.translatable }.map { it.name }.toSet()
        if (translatable.isEmpty()) return emptyMap()

        return CommentTree.allComments(loadTranslated())
            .filter { it.translated && it.name in translatable }
            .associateBy { it.name }
    }

    /**
     * Makes a comment from www's HTML what the app shows: its markdown rebuilt from its HTML (www
     * doesn't send it), and the translation in its place when there is one, keeping the original.
     */
    private fun prepare(translations: Map<String, Comment>): (Comment) -> Comment = { comment ->
        val written = comment.copy(body = HtmlMarkdownConverter.convert(comment.bodyHtml))
        val translation = translations[comment.name]

        if (translation == null) {
            written
        } else {
            written.copy(
                body = HtmlMarkdownConverter.convert(translation.bodyHtml),
                bodyHtml = translation.bodyHtml,
                translated = true,
                original = CommentText(written.body, written.bodyHtml),
            )
        }
    }

    suspend fun getPost(idOrFullname: String): Post? {
        api.ensureAuthenticated()
        return api.getPost(idOrFullname)
    }

    suspend fun getPosts(ids: List<String>): List<Post> {
        api.ensureAuthenticated()
        return api.getPostsById(ids.map { if (it.startsWith("t3_")) it else "t3_$it" })
    }

    suspend fun about(subreddit: String): Subreddit? {
        api.tryAuthenticate()
        return api.getSubredditAbout(subreddit)
    }

    suspend fun rules(subreddit: String): SubredditRules? {
        api.tryAuthenticate()
        return api.getSubredditRules(subreddit)
    }

    suspend fun userData(username: String): User? {
        api.tryAuthenticate()
        return api.getUserAbout(username)
    }

    /** Account data for the given authors (by fullname), for the account-based block rules. */
    suspend fun partialUserData(fullnames: Collection<String?>): Map<String, UserPartialData> {
        val ids = fullnames.filterNotNull().filter { it.isNotBlank() }
        if (ids.isEmpty()) return emptyMap()
        api.ensureAuthenticated()
        return api.getUserDataByIds(ids)
    }

    suspend fun vote(thing: RedditThing, state: VoteState) {
        api.ensureAuthenticated()
        api.vote(thing.name, state.direction)
    }

    suspend fun setSaved(thing: RedditThing, saved: Boolean) {
        api.ensureAuthenticated()
        api.setSaved(thing.name, saved)
    }

    suspend fun setVisible(thing: RedditThing, visible: Boolean) {
        api.ensureAuthenticated()
        api.setHidden(thing.name, !visible)
    }

    suspend fun setLocked(thing: RedditThing, locked: Boolean) {
        api.ensureAuthenticated()
        api.setLocked(thing.name, locked)
    }

    suspend fun setInboxReplies(thing: RedditThing, enabled: Boolean) {
        api.ensureAuthenticated()
        api.setSendReplies(thing.name, enabled)
    }

    suspend fun distinguish(thing: RedditThing, distinguish: Boolean, sticky: Boolean) {
        api.ensureAuthenticated()
        api.distinguish(thing.name, distinguish, sticky)
    }

    suspend fun delete(thing: RedditThing) {
        api.ensureAuthenticated()
        api.delete(thing.name)
    }

    suspend fun report(thing: RedditThing, reason: String? = null, siteReason: String? = null, ruleReason: String? = null) {
        api.ensureAuthenticated()
        api.report(thing.name, reason, siteReason, ruleReason)
    }

    suspend fun markRead(thing: RedditThing, read: Boolean) {
        api.ensureAuthenticated()
        api.setRead(thing.name, read)
    }

    suspend fun setSubscribed(subreddit: Subreddit, subscribed: Boolean) {
        api.ensureAuthenticated()
        api.setSubscribed(subreddit.displayName, subscribed)
    }

    suspend fun message(username: String, subject: String, body: String) {
        api.ensureAuthenticated()
        api.composeMessage(username, prepareOutgoingText(subject), prepareOutgoingText(body))
    }

    suspend fun multis(): List<Multi> {
        api.ensureAuthenticated()
        return api.getMyMultis()
    }

    suspend fun createMulti(name: String): Multi? {
        api.ensureAuthenticated()
        val user = loggedInUser ?: throw NotLoggedInException("You must be logged in to create a multireddit.")
        return api.createMulti("user/$user/m/$name", name)
    }

    /** Whether Reddit deleted it (the C# DeleteMulti's bool). */
    suspend fun deleteMulti(multi: Multi): Boolean {
        api.ensureAuthenticated()
        return api.deleteMulti(multi.path.trimStart('/'))
    }

    suspend fun addSubredditToMulti(multi: Multi, subreddit: String) {
        api.ensureAuthenticated()
        api.addSubredditToMulti(multi.path.trimStart('/'), subreddit)
    }

    suspend fun removeSubredditFromMulti(multi: Multi, subreddit: String) {
        api.ensureAuthenticated()
        api.removeSubredditFromMulti(multi.path.trimStart('/'), subreddit)
    }

    /**
     * Replies to a post or comment. Sent as richtext, except text with nested quotes, which
     * Reddit's richtext endpoint mishandles: that goes as markdown.
     */
    suspend fun comment(parent: RedditThing, markdown: String): Comment? {
        api.ensureAuthenticated()
        val text = prepareOutgoingText(markdown)
        ensureNotNestedQuotesWithImage(text)
        return if (MarkdownAnalyzer.hasNestedBlockquote(text)) {
            api.comment(parent.name, text, null)
        } else {
            api.comment(parent.name, text, MarkdownRichTextConverter.convert(text))
        }
    }

    /** Edits the user's own comment, sent the same way as [comment]. */
    suspend fun update(comment: Comment, markdown: String): Comment? {
        api.ensureAuthenticated()
        val text = prepareOutgoingText(markdown)
        ensureNotNestedQuotesWithImage(text)
        return if (MarkdownAnalyzer.hasNestedBlockquote(text)) {
            api.edit(comment.name, text, null)
        } else {
            api.edit(comment.name, text, MarkdownRichTextConverter.convert(text))
        }
    }

    private fun ensureNotNestedQuotesWithImage(markdown: String) {
        if (MarkdownAnalyzer.hasNestedBlockquote(markdown) && MarkdownAnalyzer.containsImagePlaceholder(markdown)) {
            throw IllegalStateException(
                "Reddit's API can't handle nested blockquotes and image attachments in the same " +
                    "comment: the richtext endpoint mishandles nested quotes, and the markdown " +
                    "endpoint can't attach images. Post the quotes and the image as separate comments.",
            )
        }
    }

    /** Submits a post; returns it as Reddit now has it. [kind] is "self", "link" or "image". */
    suspend fun createPost(subreddit: String, title: String, content: String, kind: String): Post? {
        api.ensureAuthenticated()
        val name = api.submit(
            subreddit = subreddit,
            title = prepareOutgoingText(title),
            kind = kind,
            text = if (kind == "self") prepareOutgoingText(content) else null,
            url = if (kind == "link") content else null,
        ) ?: return null
        return getPost(name)
    }

    suspend fun uploadMedia(bytes: ByteArray, filename: String, mimeType: String): String {
        api.ensureAuthenticated()
        return api.uploadMedia(bytes, filename, mimeType)
    }

    /** The user's outgoing-text settings, applied to anything they're about to submit. */
    fun prepareOutgoingText(text: String): String =
        if (hacks().swearObfuscation) com.mrjackspade.deaddit.core.text.SwearObfuscator.obfuscate(text) else text

    private fun thingOf(item: ListingItem): RedditThing = when (item) {
        is ListingItem.PostItem -> item.post
        is ListingItem.CommentItem -> item.comment
        is ListingItem.MessageItem -> item.message
        is ListingItem.MoreItem -> throw IllegalStateException("A feed listing contained a 'more' placeholder.")
    }
}
