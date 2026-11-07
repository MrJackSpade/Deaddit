package com.mrjackspade.deaddit.core.feed

import com.mrjackspade.deaddit.core.config.InboxSort
import com.mrjackspade.deaddit.core.config.PostSort
import com.mrjackspade.deaddit.core.config.UserProfileSort

/** A sort a feed can use: its name on the sort buttons, and its path segment (null for none). */
data class Sort(val label: String, val path: String?, val ordinal: Int)

fun PostSort.toSort() = Sort(name, path, ordinal)
fun InboxSort.toSort() = Sort(name, path, ordinal)
fun UserProfileSort.toSort() = Sort(name, path, ordinal + 1)

enum class FeedKind { Subreddit, Multi, Account, Message, Listing }

/**
 * Something with a listing to open: a subreddit, multi, user, the inbox, saved items or home.
 * [endpoint] may contain %USER%, the logged-in user.
 */
sealed class Feed(val name: String, open val displayName: String) {
    abstract val kind: FeedKind
    abstract val endpoint: String

    /** The sorts the feed's buttons offer; empty when it has none. */
    abstract val sorts: List<Sort>
    abstract val defaultSort: Sort?

    /** Whether block rules apply when the feed opens. */
    open val filteredByDefault: Boolean = true

    override fun equals(other: Any?) = other is Feed && other.name == name && other.kind == kind
    override fun hashCode() = name.hashCode()

    open class Subreddit(name: String) : Feed(cleanName(name, 'r'), "/r/${cleanName(name, 'r')}") {
        override val kind = FeedKind.Subreddit
        override val endpoint get() = "/r/$name"
        override val sorts = PostSort.entries.filter { it != PostSort.Undefined }.map { it.toSort() }
        override val defaultSort: Sort? = PostSort.Hot.toSort()
    }

    /** The front page. */
    object Home : Subreddit("") {
        override val displayName = "Home"
        override val endpoint get() = ""
        override fun toString() = "Home"
    }

    class Multi(name: String, val user: String = "%USER%") : Feed(cleanName(name, 'm'), "/m/${cleanName(name, 'm')}") {
        override val kind = FeedKind.Multi
        override val endpoint get() = "/user/$user/m/$name"
        override val sorts = PostSort.entries.filter { it != PostSort.Undefined }.map { it.toSort() }
        override val defaultSort: Sort? = PostSort.Hot.toSort()
    }

    class User(name: String) : Feed(cleanName(name, 'u'), "/u/${cleanName(name, 'u')}") {
        override val kind = FeedKind.Account
        override val endpoint get() = "/user/$name"
        override val sorts = UserProfileSort.entries.map { it.toSort() }
        override val defaultSort: Sort? = UserProfileSort.Overview.toSort()
        override val filteredByDefault = false
    }

    object Messages : Feed("Messages", "Messages") {
        override val kind = FeedKind.Message
        override val endpoint = "/message"
        override val sorts = InboxSort.entries.filter { it != InboxSort.Undefined }.map { it.toSort() }
        override val defaultSort: Sort? = InboxSort.Undefined.toSort()
        override val filteredByDefault = false
    }

    object Saved : Feed("Saved", "Saved") {
        override val kind = FeedKind.Listing
        override val endpoint = "/user/%USER%/saved"
        override val sorts = emptyList<Sort>()
        override val defaultSort: Sort? = null
        override val filteredByDefault = false
    }

    companion object {
        /** "pics", "r/pics", "/r/pics", "m/name" or "u/name". */
        fun fromName(name: String): Feed {
            val n = name.removePrefix("/")
            return when {
                !n.contains('/') || n.startsWith("r/", ignoreCase = true) -> Subreddit(n)
                n.startsWith("m/") -> Multi(n)
                n.startsWith("u/") -> User(n)
                else -> throw IllegalArgumentException("'$name' isn't a subreddit, multi or user.")
            }
        }

        private fun cleanName(name: String, prefix: Char): String = when {
            name.startsWith("/$prefix/", ignoreCase = true) -> name.substring(3)
            name.startsWith("$prefix/", ignoreCase = true) -> name.substring(2)
            !name.contains('/') -> name
            else -> throw IllegalArgumentException("Invalid name")
        }
    }
}
