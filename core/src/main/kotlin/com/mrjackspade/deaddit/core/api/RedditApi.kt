package com.mrjackspade.deaddit.core.api

import com.mrjackspade.deaddit.core.markdown.HtmlMarkdownConverter
import com.mrjackspade.deaddit.core.model.Comment
import com.mrjackspade.deaddit.core.model.Message
import com.mrjackspade.deaddit.core.model.More
import com.mrjackspade.deaddit.core.model.Multi
import com.mrjackspade.deaddit.core.model.Post
import com.mrjackspade.deaddit.core.model.RedditJson
import com.mrjackspade.deaddit.core.model.Subreddit
import com.mrjackspade.deaddit.core.model.SubredditRules
import com.mrjackspade.deaddit.core.model.User
import com.mrjackspade.deaddit.core.model.UserPartialData
import com.mrjackspade.deaddit.core.session.Session
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import kotlinx.serialization.json.JsonArray
import kotlinx.serialization.json.JsonElement
import kotlinx.serialization.json.JsonNull
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonObjectBuilder
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.boolean
import kotlinx.serialization.json.booleanOrNull
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.contentOrNull
import kotlinx.serialization.json.decodeFromJsonElement
import kotlinx.serialization.json.jsonArray
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive
import kotlinx.serialization.json.put

/** Runs one Reddit.Api command and returns its JSON result; how depends on the platform. */
fun interface CommandRunner {
    fun call(method: String, args: JsonObject): JsonElement
}

/** A thing from a listing: what kind it is, and its data. */
sealed interface ListingItem {
    data class PostItem(val post: Post) : ListingItem
    data class CommentItem(val comment: Comment) : ListingItem
    data class MessageItem(val message: Message) : ListingItem
    data class MoreItem(val more: More) : ListingItem

    companion object {
        /** One `{"kind": ..., "data": ...}` thing. */
        fun parse(thing: JsonElement): ListingItem {
            val kind = thing.jsonObject["kind"]!!.jsonPrimitive.content
            val data = thing.jsonObject["data"]!!
            return when (kind) {
                "t3" -> PostItem(decodePost(data))
                "t1" -> CommentItem(RedditJson.decodeFromJsonElement(data))
                "t4" -> MessageItem(RedditJson.decodeFromJsonElement(data))
                "more" -> MoreItem(RedditJson.decodeFromJsonElement(data))
                else -> throw IllegalStateException("Unexpected thing kind '$kind' in a listing.")
            }
        }

        /**
         * A post. One read from www's HTML comes without selftext when it has a body, since www
         * doesn't send the markdown: it's rebuilt from the body's HTML, as comments' is (Reddit's
         * JSON always has selftext, "" when there's no body).
         */
        fun decodePost(data: JsonElement): Post {
            val post = RedditJson.decodeFromJsonElement<Post>(data)
            val html = post.bodyHtml
            return if ("selftext" !in data.jsonObject && !html.isNullOrEmpty()) post.copy(body = HtmlMarkdownConverter.convert(html)) else post
        }

        /** The things in a listing (`{"kind": "Listing", "data": {"children": [...]}}`). */
        fun parseListing(listing: JsonElement): List<ListingItem> =
            listing.jsonObject["data"]!!.jsonObject["children"]!!.jsonArray.map(::parse)
    }
}

data class Page(val items: List<ListingItem>, val after: String?)

/** One page of a listing request. */
data class ListingParameters(val after: String? = null, val limit: Int? = null, val geo: String? = null)

/**
 * Typed access to Reddit.Api (the C# library, run natively). Each function is one Reddit.Api call;
 * all routing, blocking workarounds and Reddit quirks live on the C# side.
 */
class RedditApi(private val runner: CommandRunner) {

    private suspend fun call(method: String, args: JsonObjectBuilder.() -> Unit = {}): JsonElement =
        withContext(Dispatchers.IO) { runner.call(method, buildJsonObject(args)) }

    // ----- Session -----

    /**
     * The session Reddit.Api holds, mirrored here so it can be read without a call into .NET. Reddit.Api only ever
     * gets a session from the host: through [setSession], or from the host's session refresher, which reports it with
     * [sessionRefreshed]. (It never logs in by itself: the app gives it no credentials.)
     */
    @Volatile
    var session: Session? = null
        private set

    suspend fun setSession(session: Session) {
        call("SetBearerToken") { put("token", RedditJson.encodeToJsonElement(Session.serializer(), session)) }
        this.session = session
    }

    /** Records the session the host's refresher handed Reddit.Api, which sets it itself (RefreshSessionAsync). */
    fun sessionRefreshed(session: Session) {
        this.session = session
    }

    suspend fun getTokenOwner(accessToken: String, sessionCookies: String?): String? =
        call("GetTokenOwner") { put("accessToken", accessToken); put("sessionCookies", sessionCookies) }.stringOrNull()

    suspend fun validateSession(session: Session): Boolean =
        call("ValidateBearerToken") { put("token", RedditJson.encodeToJsonElement(Session.serializer(), session)) }.jsonPrimitive.booleanOrNull == true

    suspend fun authenticatedUser(): String? =
        call("GetSession").jsonObject["authenticatedUser"]?.stringOrNull()

    /** Logs in if there's a way to; carries on logged out if not. */
    suspend fun tryAuthenticate() { call("TryAuthenticate") }

    /** Logs in, asking the user if needed; fails if there's no session. */
    suspend fun ensureAuthenticated() { call("EnsureAuthenticated") }

    // ----- Reads -----

    suspend fun getListing(endpoint: String, parameters: ListingParameters): Page =
        call("GetListing") { put("endpoint", endpoint); put("parameters", parameters.toJson()) }.toPage()

    suspend fun getSortedPosts(path: String, parameters: ListingParameters): Page =
        call("GetSortedPosts") { put("path", path); put("parameters", parameters.toJson()) }.toPage()

    /**
     * A thread's comments from www's HTML (or one comment's with its replies, and [context] of its
     * parents above it), as a tree, with "load more" placeholders for the rest. [translate] is a
     * language to have www translate into.
     */
    suspend fun getThreadComments(subreddit: String, linkFullname: String, commentFullname: String?, context: Int, translate: String?): List<ListingItem> =
        call("GetThreadComments") {
            put("subreddit", subreddit)
            put("linkFullname", linkFullname)
            put("commentFullname", commentFullname)
            put("context", context)
            put("translate", translate)
        }.jsonArray.map(ListingItem::parse)

    /** The comments behind one of www's "load more" placeholders, as a tree in its place. */
    suspend fun getMoreComments(more: More, linkFullname: String, subreddit: String, parentFullname: String, translate: String?): List<ListingItem> =
        call("GetMoreComments") {
            put("moreUrl", requireNotNull(more.moreUrl) { "${more.name} isn't one of www's \"load more\" placeholders." })
            put("cursor", requireNotNull(more.moreCursor) { "${more.name} has no cursor." })
            put("linkFullname", linkFullname)
            put("subreddit", subreddit)
            put("parentFullname", parentFullname)
            put("translate", translate)
        }.jsonArray.map(ListingItem::parse)

    /** The post on a www thread page (as a browser has it, past Reddit's JavaScript challenge). */
    suspend fun parseThreadPagePost(html: String): Post =
        ListingItem.decodePost(call("ParseThreadPagePost") { put("html", html) })

    suspend fun getPostsById(fullnames: List<String>): List<Post> =
        call("GetById") { put("fullnames", JsonArray(fullnames.map(::JsonPrimitive))) }.toPage().items
            .map { (it as ListingItem.PostItem).post }

    suspend fun search(query: String, sort: String? = null, time: String? = null, limit: Int? = null, after: String? = null): Page =
        call("Search") {
            put("parameters", buildJsonObject {
                put("query", query); put("sort", sort); put("time", time); put("limit", limit); put("after", after)
            })
        }.toPage()

    suspend fun getSubredditAbout(subreddit: String): Subreddit? =
        call("GetSubredditAbout") { put("subreddit", subreddit) }.takeIf { it !is JsonNull }
            ?.let { RedditJson.decodeFromJsonElement<Subreddit>(it.jsonObject["data"]!!) }

    suspend fun getSubredditRules(subreddit: String): SubredditRules? =
        call("GetSubredditRules") { put("subreddit", subreddit) }.takeIf { it !is JsonNull }
            ?.let { RedditJson.decodeFromJsonElement<SubredditRules>(it) }

    suspend fun getUserAbout(username: String): User? =
        call("GetUserAbout") { put("username", username) }.takeIf { it !is JsonNull }
            ?.let { RedditJson.decodeFromJsonElement<User>(it.jsonObject["data"]!!) }

    suspend fun getUserDataByIds(accountIds: List<String>): Map<String, UserPartialData> =
        call("GetUserDataByIds") { put("accountIds", JsonArray(accountIds.map(::JsonPrimitive))) }
            .takeIf { it !is JsonNull }?.let { RedditJson.decodeFromJsonElement<Map<String, UserPartialData>>(it) } ?: emptyMap()

    suspend fun getMyMultis(): List<Multi> =
        call("GetMyMultis").takeIf { it !is JsonNull }?.jsonArray
            ?.mapNotNull { it.jsonObject["data"]?.takeIf { d -> d !is JsonNull }?.let { d -> RedditJson.decodeFromJsonElement<Multi>(d) } } ?: emptyList()

    // ----- Writes -----

    /** [richtextJson] is a RichTextDocument (see MarkdownRichTextConverter); null sends [markdown] as markdown instead. */
    suspend fun comment(parentFullname: String, markdown: String, richtextJson: JsonObject?): Comment? =
        (if (richtextJson != null) call("CommentRichText") { put("parentFullname", parentFullname); put("richtext", richtextJson) }
        else call("CommentMarkdown") { put("parentFullname", parentFullname); put("markdown", markdown) }).toCommentOrNull()

    suspend fun edit(fullname: String, markdown: String, richtextJson: JsonObject?): Comment? =
        (if (richtextJson != null) call("EditRichText") { put("fullname", fullname); put("richtext", richtextJson) }
        else call("EditMarkdown") { put("fullname", fullname); put("markdown", markdown) }).toCommentOrNull()

    /** [kind] is "self", "link" or "image"; returns the new post's fullname. */
    suspend fun submit(subreddit: String, title: String, kind: String, text: String? = null, url: String? = null): String? =
        call("Submit") {
            put("request", buildJsonObject {
                put("subreddit", subreddit); put("title", title); put("kind", kind); put("text", text); put("url", url); put("send_replies", true)
            })
        }.takeIf { it !is JsonNull }?.jsonObject?.get("name")?.stringOrNull()

    suspend fun delete(fullname: String) { call("DeleteThing") { put("fullname", fullname) } }
    suspend fun vote(fullname: String, direction: Int) { call("Vote") { put("fullname", fullname); put("direction", direction) } }
    suspend fun setSaved(fullname: String, saved: Boolean) { call(if (saved) "Save" else "Unsave") { put("fullname", fullname) } }
    suspend fun setHidden(fullname: String, hidden: Boolean) { call(if (hidden) "Hide" else "Unhide") { put("fullname", fullname) } }
    suspend fun setLocked(fullname: String, locked: Boolean) { call(if (locked) "Lock" else "Unlock") { put("fullname", fullname) } }
    suspend fun setSendReplies(fullname: String, state: Boolean) { call("SetSendReplies") { put("fullname", fullname); put("state", state) } }
    suspend fun distinguish(fullname: String, distinguish: Boolean, sticky: Boolean) {
        call("Distinguish") { put("fullname", fullname); put("how", if (distinguish) "Yes" else "No"); put("sticky", sticky) }
    }
    suspend fun report(fullname: String, reason: String?, siteReason: String?, ruleReason: String?) {
        call("Report") { put("fullname", fullname); put("reason", reason); put("siteReason", siteReason); put("ruleReason", ruleReason) }
    }
    suspend fun setSubscribed(subreddit: String, subscribed: Boolean) { call("Subscribe") { put("subreddit", subreddit); put("subscribe", subscribed) } }
    suspend fun setRead(fullname: String, read: Boolean) { call(if (read) "ReadMessage" else "UnreadMessage") { put("fullname", fullname) } }
    suspend fun composeMessage(to: String, subject: String, body: String) { call("ComposeMessage") { put("to", to); put("subject", subject); put("body", body) } }

    suspend fun createMulti(multipath: String, displayName: String): Multi? =
        call("CreateOrUpdateMulti") {
            put("multipath", multipath)
            put("request", buildJsonObject { put("display_name", displayName); put("visibility", "private") })
        }.takeIf { it !is JsonNull }?.jsonObject?.get("data")?.let { RedditJson.decodeFromJsonElement<Multi>(it) }

    /** Whether Reddit deleted it. */
    suspend fun deleteMulti(multipath: String): Boolean = call("DeleteMulti") { put("multipath", multipath) }.jsonPrimitive.boolean
    suspend fun addSubredditToMulti(multipath: String, subreddit: String) { call("AddSubredditToMulti") { put("multipath", multipath); put("subreddit", subreddit) } }
    suspend fun removeSubredditFromMulti(multipath: String, subreddit: String) { call("RemoveSubredditFromMulti") { put("multipath", multipath); put("subreddit", subreddit) } }

    /** Uploads an image for a comment; returns the media key to reference it by. */
    suspend fun uploadMedia(bytes: ByteArray, filename: String, mimeType: String): String {
        val lease = call("GetMediaAssetUploadLease") { put("filepath", filename); put("mimetype", mimeType) }
        check(lease !is JsonNull) { "Failed to get media upload lease" }
        val key = lease.jsonObject["args"]!!.jsonObject["fields"]!!.jsonArray
            .first { it.jsonObject["name"]?.jsonPrimitive?.content == "key" }.jsonObject["value"]!!.jsonPrimitive.content
        val uploaded = call("UploadMediaToS3") {
            put("lease", lease); put("filename", filename); put("mimetype", mimeType)
            put("fileBase64", java.util.Base64.getEncoder().encodeToString(bytes))
        }.jsonPrimitive.booleanOrNull == true
        check(uploaded) { "Failed to upload media to S3" }
        return key
    }

    // ----- Parsing -----

    private fun ListingParameters.toJson(): JsonObject = buildJsonObject {
        put("after", after); put("limit", limit); put("geo", geo)
    }

    private fun JsonElement.toPage(): Page {
        val data = jsonObject["data"]!!.jsonObject
        return Page(data["children"]!!.jsonArray.map(ListingItem::parse), data["after"]?.stringOrNull())
    }

    private fun JsonElement.toCommentOrNull(): Comment? =
        takeIf { it !is JsonNull }?.jsonObject?.get("data")?.let { RedditJson.decodeFromJsonElement<Comment>(it) }

    private fun JsonElement.stringOrNull(): String? = (this as? JsonPrimitive)?.takeIf { it !is JsonNull }?.contentOrNull
}
