package com.mrjackspade.deaddit.core.thread

import com.mrjackspade.deaddit.core.api.ListingItem
import com.mrjackspade.deaddit.core.model.RedditJson
import kotlinx.serialization.json.JsonElement
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Assert.assertSame
import org.junit.Assert.assertTrue
import org.junit.Test

class CommentTreeTest {
    private fun json(text: String): JsonElement = RedditJson.parseToJsonElement(text)

    private fun comment(id: String, parent: String, replies: String = "\"\"") =
        """{"kind":"t1","data":{"id":"$id","name":"t1_$id","parent_id":"$parent","body":"$id","replies":$replies}}"""

    private fun more(id: String, parent: String, children: List<String>) =
        """{"kind":"more","data":{"id":"$id","name":"t1_$id","parent_id":"$parent","count":${children.size},"children":[${children.joinToString { "\"$it\"" }}]}}"""

    private fun listing(vararg things: String) = """{"kind":"Listing","data":{"children":[${things.joinToString(",")}]}}"""

    @Test
    fun buildsNestedRepliesFromTheThread() {
        val items = ListingItem.parseListing(
            json(
                listing(
                    comment("a", "t3_p", listing(comment("b", "t1_a"), more("m", "t1_a", listOf("c", "d")))),
                    comment("e", "t3_p"),
                    more("top", "t3_p", listOf("f")),
                ),
            ),
        )

        val tree = CommentTree.fromComments(items, postId = "p", parent = null)

        assertEquals(3, tree.size)
        val a = tree[0] as CommentNode
        assertNull(a.parent)
        assertEquals(listOf("b", "m"), a.replies.map { (it as? CommentNode)?.comment?.id ?: (it as MoreNode).more.id })
        assertSame(a.comment, a.replies[0].parent)
        assertSame(a.comment, a.replies[1].parent)
        assertEquals(listOf("c", "d"), (a.replies[1] as MoreNode).more.children)
        assertTrue((tree[1] as CommentNode).replies.isEmpty())
        assertEquals("top", (tree[2] as MoreNode).more.id)
    }

    @Test
    fun dropsThePostFromItsComments() {
        val items = ListingItem.parseListing(json(listing(comment("p", "t3_p"), comment("a", "t3_p"))))
        assertEquals(listOf("a"), CommentTree.fromComments(items, "p", null).map { (it as CommentNode).comment.id })
    }
}
