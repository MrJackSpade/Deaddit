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
     * The tree for a post's comments, from GetComments. [parent] is the comment the response is
     * focused on (top-level nodes reply to it), or null for the whole thread. The post itself can
     * come back among the comments; [postId] drops it.
     */
    fun fromComments(items: List<ListingItem>, postId: String, parent: Comment?): List<ThreadNode> =
        items.mapNotNull { item ->
            when (item) {
                is ListingItem.MoreItem -> MoreNode(item.more, parent)
                is ListingItem.CommentItem -> if (item.comment.id == postId) null else commentWithReplies(item.comment, parent)
                else -> throw IllegalStateException("Unexpected $item among a post's comments.")
            }
        }

    /**
     * Nests the flat result of a "load more" (GetMoreChildren) by parent ID. What doesn't reply to
     * another loaded comment takes the place of the "load more", replying to [parent].
     */
    fun fromMoreChildren(items: List<ListingItem>, parent: Comment?): List<ThreadNode> {
        val comments = LinkedHashMap<String, CommentNode>()
        val all = items.map { item ->
            when (item) {
                is ListingItem.CommentItem -> CommentNode(item.comment, null).also { comments[item.comment.name] = it }
                is ListingItem.MoreItem -> MoreNode(item.more, null)
                else -> throw IllegalStateException("Unexpected $item among loaded comments.")
            }
        }

        val topLevel = mutableListOf<ThreadNode>()

        for (node in all) {
            val parentId = when (node) {
                is CommentNode -> node.comment.parentId
                is MoreNode -> node.more.parentId
            }

            val parentNode = parentId?.let(comments::get)

            if (parentNode != null) {
                node.parent = parentNode.comment
                parentNode.replies += node
            } else {
                topLevel += node
            }
        }

        topLevel.forEach { it.parent = parent }
        return topLevel
    }

    private fun commentWithReplies(comment: Comment, parent: Comment?): CommentNode {
        val node = CommentNode(comment, parent)

        // Reddit sends "replies" as an empty string when there are none, otherwise a listing.
        val replies = comment.replies
        if (replies is JsonObject) {
            for (item in ListingItem.parseListing(replies)) {
                node.replies += when (item) {
                    is ListingItem.MoreItem -> MoreNode(item.more, comment)
                    is ListingItem.CommentItem -> commentWithReplies(item.comment, comment)
                    else -> throw IllegalStateException("Unexpected $item among replies to ${comment.name}.")
                }
            }
        } else if (replies != null && !(replies is JsonPrimitive && replies.isString && replies.content.isEmpty())) {
            throw IllegalStateException("Unexpected replies on ${comment.name}: $replies")
        }

        return node
    }
}
