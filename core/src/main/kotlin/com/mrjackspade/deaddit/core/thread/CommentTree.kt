package com.mrjackspade.deaddit.core.thread

import com.mrjackspade.deaddit.core.api.ListingItem
import com.mrjackspade.deaddit.core.model.Comment
import com.mrjackspade.deaddit.core.model.More
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonPrimitive

/** A comment thread, as a tree. [parent] is the comment a node replies to; null for the post. */
sealed interface ThreadNode {
    var parent: Comment?
}

class CommentNode(val comment: Comment, override var parent: Comment?) : ThreadNode {
    val replies: MutableList<ThreadNode> = mutableListOf()
}

/** "Load more": [More.children] are the comments to load; none means "continue this thread". */
class MoreNode(val more: More, override var parent: Comment?) : ThreadNode

object CommentTree {
    /**
     * The tree for a post's comments, from GetComments (or www's comments, which come nested the
     * same way). [parent] is the comment the response is focused on (top-level nodes reply to it),
     * or null for the whole thread. The post itself can come back among the comments; [postId]
     * drops it. [prepare] makes each comment what the app shows, before its node is made.
     */
    fun fromComments(items: List<ListingItem>, postId: String, parent: Comment?, prepare: (Comment) -> Comment = { it }): List<ThreadNode> =
        items.mapNotNull { item ->
            when (item) {
                is ListingItem.MoreItem -> MoreNode(item.more, parent)
                is ListingItem.CommentItem -> if (item.comment.id == postId) null else commentWithReplies(item.comment, parent, prepare)
                else -> throw IllegalStateException("Unexpected $item among a post's comments.")
            }
        }

    /** Every comment in [items] and their replies, all the way down, as received. */
    fun allComments(items: List<ListingItem>): Sequence<Comment> = sequence {
        for (item in items) {
            if (item !is ListingItem.CommentItem) continue
            yield(item.comment)
            val replies = item.comment.replies
            if (replies is JsonObject) yieldAll(allComments(ListingItem.parseListing(replies)))
        }
    }

    private fun commentWithReplies(received: Comment, parent: Comment?, prepare: (Comment) -> Comment = { it }): CommentNode {
        val comment = prepare(received)
        val node = CommentNode(comment, parent)

        // Reddit sends "replies" as an empty string when there are none, otherwise a listing.
        val replies = received.replies
        if (replies is JsonObject) {
            for (item in ListingItem.parseListing(replies)) {
                node.replies += when (item) {
                    is ListingItem.MoreItem -> MoreNode(item.more, comment)
                    is ListingItem.CommentItem -> commentWithReplies(item.comment, comment, prepare)
                    else -> throw IllegalStateException("Unexpected $item among replies to ${comment.name}.")
                }
            }
        } else if (replies != null && !(replies is JsonPrimitive && replies.isString && replies.content.isEmpty())) {
            throw IllegalStateException("Unexpected replies on ${comment.name}: $replies")
        }

        return node
    }
}
