package com.mrjackspade.deaddit.ui.post

import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import com.mrjackspade.deaddit.core.model.Post
import com.mrjackspade.deaddit.core.model.VoteState
import java.lang.ref.WeakReference

/** What the user can change on a post: its vote, the score that follows it, and whether it's saved. */
class PostStatus(post: Post) {
    var likes by mutableStateOf(post.likes)
    var score by mutableIntStateOf(post.score)
    var saved by mutableStateOf(post.saved)

    /** Applies a tap on ▲ or ▼, as the C# vote container did, and returns the new state to send. */
    fun toggle(direction: VoteState): VoteState {
        val target = if (likes == direction) VoteState.None else direction
        score += target.direction - likes.direction
        likes = target
        return target
    }

    fun update(post: Post) {
        likes = post.likes
        score = post.score
        saved = post.saved
    }
}

/**
 * One [PostStatus] per post, shared by every row that shows it (its feed card, its thread, the
 * reply page), so a vote or save made on one shows on all of them, however the thread was opened.
 * Kept only while a row holds it.
 */
class PostStatuses {
    private val byName = HashMap<String, WeakReference<PostStatus>>()

    /** The post's live status, or one made from [post] when nothing shows it. */
    fun of(post: Post): PostStatus = byName[post.name]?.get() ?: PostStatus(post).also { put(post, it) }

    /** For a post just loaded from Reddit, which reports its current vote, score and save. */
    fun loaded(post: Post): PostStatus = byName[post.name]?.get()?.also { it.update(post) } ?: of(post)

    private fun put(post: Post, status: PostStatus) {
        byName.values.removeAll { it.get() == null }
        byName[post.name] = WeakReference(status)
    }
}
