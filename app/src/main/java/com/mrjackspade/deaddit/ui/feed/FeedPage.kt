package com.mrjackspade.deaddit.ui.feed

import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*

import android.widget.Toast
import androidx.activity.compose.BackHandler
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.itemsIndexed
import androidx.compose.foundation.lazy.rememberLazyListState
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.derivedStateOf
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.runtime.snapshotFlow
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import com.mrjackspade.deaddit.Services
import com.mrjackspade.deaddit.core.blocking.Blocking
import com.mrjackspade.deaddit.core.config.BlockRule
import com.mrjackspade.deaddit.core.config.PostState
import com.mrjackspade.deaddit.core.feed.Feed
import com.mrjackspade.deaddit.core.feed.FeedKind
import com.mrjackspade.deaddit.core.feed.Sort
import com.mrjackspade.deaddit.core.model.Comment
import com.mrjackspade.deaddit.core.model.Message
import com.mrjackspade.deaddit.core.model.Post
import com.mrjackspade.deaddit.core.model.RedditThing
import com.mrjackspade.deaddit.core.model.User
import com.mrjackspade.deaddit.core.model.UserPartialData
import com.mrjackspade.deaddit.core.model.thumbnailUrl
import com.mrjackspade.deaddit.core.model.titleOrEmpty
import com.mrjackspade.deaddit.core.model.urlOrEmpty
import com.mrjackspade.deaddit.ui.FlexRow
import com.mrjackspade.deaddit.ui.Links
import com.mrjackspade.deaddit.ui.LocalStyling
import com.mrjackspade.deaddit.ui.Pages
import com.mrjackspade.deaddit.ui.Screen
import com.mrjackspade.deaddit.ui.Spinner
import com.mrjackspade.deaddit.ui.TopBar
import com.mrjackspade.deaddit.ui.TopBarButton
import com.mrjackspade.deaddit.ui.color
import com.mrjackspade.deaddit.ui.comment.CommentController
import com.mrjackspade.deaddit.ui.comment.CommentEntry
import com.mrjackspade.deaddit.ui.comment.CommentModel
import com.mrjackspade.deaddit.ui.comment.CommentView
import com.mrjackspade.deaddit.ui.comment.ThreadRowFrame
import com.mrjackspade.deaddit.ui.comment.findEntry
import com.mrjackspade.deaddit.ui.comment.flattenThread
import com.mrjackspade.deaddit.ui.comment.removeEntry
import com.mrjackspade.deaddit.ui.message.MessageView
import com.mrjackspade.deaddit.ui.post.PostController
import com.mrjackspade.deaddit.ui.post.PostModel
import com.mrjackspade.deaddit.ui.post.PostRow
import com.mrjackspade.deaddit.ui.px
import com.mrjackspade.deaddit.ui.user.UserHeader
import kotlinx.coroutines.launch
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock

/**
 * A listing: a subreddit, multi, user, the inbox, saved items or the front page (the C# app's
 * ThingCollectionPage). Loads a page at a time as the end comes into view, applying the block
 * rules and duplicate handling.
 */
class FeedPage(private val services: Services, private val feed: Feed, initialSort: Sort?) : Screen() {
    /** One loaded thing, with its row's state. */
    sealed interface Item {
        val thing: RedditThing
        class PostEntry(val model: PostModel) : Item { override val thing get() = model.post }

        /** A comment, with the replies the user writes to it here (shown under it, as in C#). */
        class CommentItem(val entry: CommentEntry) : Item { override val thing get() = entry.model.comment }
        class MessageEntry(val message: Message) : Item { override val thing get() = message }
    }

    private val items = mutableStateListOf<Item>()
    private var sort by mutableStateOf(initialSort)

    /** The sort buttons show only for a feed opened with a sort, as in C#. */
    private val showSorts = initialSort != null && feed.sorts.isNotEmpty()

    private var after: String? = null

    /**
     * The last page asked for came back empty. The next scroll gesture asks Reddit again, with the
     * same cursor, as the C# app did on each scroll to the bottom; there's no permanent end.
     */
    private var reachedEnd by mutableStateOf(false)
    private var loading by mutableStateOf(false)
    private var blockEnabled by mutableStateOf(feed.filteredByDefault && feed.kind != FeedKind.Account)
    private var header by mutableStateOf<User?>(null)

    /** The selected row, by thing name: it shows its actions. One at a time. */
    var selected by mutableStateOf<String?>(null)

    private val loadLock = Mutex()

    /**
     * Whether "back" asks for a second press before leaving: armed by each page loaded after the
     * first, and spent by the press it intercepts.
     */
    private var bufferBack by mutableStateOf<Boolean?>(null)

    /**
     * Everything loaded since the last reload, less what a new block rule took away (the C#
     * _loadedPosts): hidden posts stay. The duplicate checks are rebuilt from it at each load.
     */
    private val loaded = mutableListOf<RedditThing>()

    /** Thumbnail ETags seen, across every page since the last reload. */
    private val loadedEtags = HashSet<String>()

    private var started = false

    // Loads run in the page's scope: one is wanted only while the page is open, and the page stays
    // open (its scope alive) for as long as it's on the back stack, whatever else is shown on top.
    private fun start() {
        if (started) return
        started = true
        scope.launch {
            try {
                if (feed.kind == FeedKind.Account) {
                    header = services.reddit.userData(feed.name)
                }
            } catch (e: Throwable) {
                services.dialogs.error(e)
            }
            tryLoad()
        }
    }

    private suspend fun tryLoad() {
        if (!loadLock.tryLock()) return
        try {
            load()
        } catch (e: Throwable) {
            services.dialogs.error(e)
        } finally {
            loadLock.unlock()
        }
    }

    private suspend fun reload() = loadLock.withLock {
        items.clear()
        loaded.clear()
        loadedEtags.clear()
        after = null
        reachedEnd = false
        selected = null
        try {
            load()
        } catch (e: Throwable) {
            services.dialogs.error(e)
        }
    }

    private suspend fun load() {
        loading = true
        try {
            val hacks = services.settings.hacks.value
            val blocking = services.settings.blocking.value
            val added = mutableListOf<Item>()

            // What's been seen, from what's loaded; blank titles and links are no duplicates.
            val loadedNames = loaded.mapTo(HashSet()) { it.name }
            val loadedTitles = loaded.map { it.titleOrEmpty }.filterTo(HashSet()) { it.isNotBlank() }
            val loadedUrls = loaded.map { it.urlOrEmpty }.filterTo(HashSet()) { it.isNotBlank() }

            do {
                val things = services.reddit.getPosts(feed, sort, hacks.pageSize, after, hacks.defaultRegion).take(hacks.pageSize)

                reachedEnd = things.isEmpty()
                if (things.isEmpty()) break

                // Account-based rules need each author's account data, which only a logged-in
                // session can fetch, and which a user's own page doesn't filter on.
                val userData: Map<String, UserPartialData>? =
                    if (Blocking.requiresUserData(blocking) && feed.kind != FeedKind.Account && services.reddit.isLoggedIn) {
                        services.reddit.partialUserData(things.map { it.authorFullname }.distinct())
                    } else {
                        null
                    }

                if (blocking.duplicateThumbHandling != PostState.None) {
                    services.etags.cache(things.filterIsInstance<Post>().mapNotNull { it.thumbnailUrl() }.filter { it.isNotBlank() })
                }

                for (thing in things) {
                    after = thing.name

                    val state = postState(thing, userData, loadedTitles, loadedUrls)

                    if (state == PostState.Block && blockEnabled) continue
                    if (!loadedNames.add(thing.name)) continue

                    val item = when (thing) {
                        is Post -> {
                            if (thing.hidden) continue

                            thing.titleOrEmpty.takeIf { it.isNotBlank() }?.let { loadedTitles += it }
                            thing.urlOrEmpty.takeIf { it.isNotBlank() }?.let { loadedUrls += it }
                            if (blocking.duplicateThumbHandling != PostState.None) {
                                thing.thumbnailUrl()?.takeIf { it.isNotEmpty() }?.let { loadedEtags += services.etags.get(it) }
                            }

                            Item.PostEntry(PostModel(thing, state).also { it.visited = services.visits.hasVisited(thing) })
                        }
                        is Comment -> Item.CommentItem(CommentEntry(CommentModel(thing), null))
                        is Message -> Item.MessageEntry(thing)
                    }

                    added += item
                    loaded += thing
                }
            } while (added.size < hacks.pageSize)

            items += added

            // Back only asks for confirmation from the second page on.
            bufferBack = bufferBack != null
        } finally {
            loading = false
        }
    }

    /** Blocked, dimmed as already seen, or shown normally; blocking wins over dimming. */
    private suspend fun postState(thing: RedditThing, userData: Map<String, UserPartialData>?, loadedTitles: Set<String>, loadedUrls: Set<String>): PostState {
        val blocking = services.settings.blocking.value

        if (!Blocking.isAllowed(blocking, thing, userData)) return PostState.Block

        val titleSeen = loadedTitles.any { it.equals(thing.titleOrEmpty.trim(), ignoreCase = true) }
        val etagSeen = suspend {
            val thumb = (thing as? Post)?.thumbnailUrl()
            !thumb.isNullOrBlank() && loadedEtags.contains(services.etags.get(thumb))
        }
        val urlSeen = loadedUrls.contains(thing.urlOrEmpty)

        if (blocking.duplicateTitleHandling == PostState.Block && titleSeen) return PostState.Block
        if (thing is Post && blocking.duplicateThumbHandling == PostState.Block && etagSeen()) return PostState.Block
        if (blocking.duplicateLinkHandling == PostState.Block && urlSeen) return PostState.Block

        if (blocking.duplicateTitleHandling == PostState.Visited && titleSeen) return PostState.Visited
        if (thing is Post && blocking.duplicateThumbHandling == PostState.Visited && etagSeen()) return PostState.Visited
        if (blocking.duplicateLinkHandling == PostState.Visited && urlSeen) return PostState.Visited

        return PostState.None
    }

    private val postController = PostController(
        services,
        inList = true,
        fromHistory = false,
        onSelect = { model, toggle ->
            val name = model.post.name
            selected = if (toggle && selected == name) null else name
        },
        onBlockAdded = ::onBlockAdded,
        onHidden = { model -> items.removeAll { it.thing.name == model.post.name } },
    )

    private val commentController = CommentController(
        services,
        post = null,
        onDeleted = ::removeComment,
        onReplied = ::addReply,
        onLink = { url -> services.appScope.launch { Links.open(services, url) } },
    )

    /** A comment deleted here: one the feed listed goes from the feed, a reply from under its comment. */
    private fun removeComment(model: CommentModel) {
        if (items.removeAll { it is Item.CommentItem && it.entry.model === model }) return
        for (item in items) {
            if (item is Item.CommentItem && removeEntry(item.entry.children, model)) return
        }
    }

    /** The user's reply goes first under the comment replied to (RedditCommentWebComponent). */
    private fun addReply(parent: CommentModel, reply: Comment) {
        val parentEntry = items.firstNotNullOfOrNull { item -> (item as? Item.CommentItem)?.let { findEntry(listOf(it.entry), parent) } }
            ?: throw IllegalStateException("Replied to a comment that isn't on the page.")
        parentEntry.children.add(0, CommentEntry(CommentModel(reply), parentEntry))
    }

    /** "Hide Self Karma": no score on the user's own posts and comments. */
    private fun hidesScore(author: String?) =
        services.settings.hacks.value.hideSelfKarma && author.equals(services.reddit.loggedInUser, ignoreCase = true)

    /** A new block rule removes what it matches from the page, and from what counts as seen. */
    fun onBlockAdded(rule: BlockRule) {
        items.removeAll { Blocking.isMatch(rule, it.thing) }
        loaded.removeAll { Blocking.isMatch(rule, it) }
    }

    fun remove(item: Item) {
        items.remove(item)
    }

    @Composable
    override fun Content() {
        val styling = LocalStyling.current
        val context = LocalContext.current
        val listState = rememberLazyListState()

        LaunchedEffect(Unit) { start() }

        // Load the next page as the end comes into view.
        val nearEnd by remember {
            derivedStateOf {
                val last = listState.layoutInfo.visibleItemsInfo.lastOrNull()?.index ?: 0
                last >= listState.layoutInfo.totalItemsCount - 1 - services.settings.hacks.value.pageBuffer
            }
        }
        // The load runs in the page's scope, not this effect's: the effect restarts whenever the
        // list moves in or out of range of the end, and a restart cancelled the load in flight,
        // silently, with no new page (and the page it had fetched skipped, "after" having moved on).
        LaunchedEffect(nearEnd, items.size, reachedEnd) {
            if (nearEnd && started && !reachedEnd && !loading) scope.launch { tryLoad() }
        }
        // Past the end, each scroll asks again.
        LaunchedEffect(listState) {
            snapshotFlow { listState.isScrollInProgress }.collect { scrolling -> if (scrolling) reachedEnd = false }
        }

        BackHandler(enabled = bufferBack == true) {
            bufferBack = false
            Toast.makeText(context, "Press back again to exit", Toast.LENGTH_SHORT).show()
        }

        Column(Modifier.fillMaxSize().background(styling.secondaryColor.color)) {
            TopBar {
                TopBarButton(Icons.Filled.Refresh, "Reload") { scope.launch { reload() } }
                TopBarButton(
                    Icons.Filled.Block,
                    if (blockEnabled) "Blocking on" else "Blocking off",
                    color = if (blockEnabled) Color.Red else styling.textColor.color,
                ) {
                    blockEnabled = !blockEnabled
                    scope.launch { reload() }
                }
                Text(
                    feed.name,
                    Modifier.weight(1f),
                    color = styling.textColor.color,
                    textAlign = TextAlign.Center,
                )
                TopBarButton(Icons.Filled.Info, "About") {
                    if (feed is Feed.Subreddit) Pages.openSubredditAbout(services, feed.name)
                }
                TopBarButton(Icons.Filled.Settings, "Settings") { Pages.openSettings(services) }
            }

            LazyColumn(Modifier.fillMaxSize().navigationBarsPadding(), listState) {
                header?.let { user -> item(key = "header") { UserHeader(services, user) } }

                if (showSorts) {
                    item(key = "sorts") { SortButtons() }
                }

                itemsIndexed(items, key = { _, item -> item.thing.name }) { _, item ->
                    val isSelected = selected == item.thing.name
                    when (item) {
                        is Item.PostEntry -> PostRow(
                            item.model,
                            selected = isSelected,
                            inList = true,
                            hideScore = hidesScore(item.model.post.author),
                            actions = postController.actions(item.model),
                        )

                        // Padded left, top and bottom, as the C# comment box; the user's replies under it.
                        is Item.CommentItem -> Column {
                            for (row in flattenThread(listOf(item.entry))) {
                                val entry = row.entry as CommentEntry
                                ThreadRowFrame(row, selected, endPadding = 0.dp) {
                                    CommentView(
                                        services,
                                        entry.model,
                                        post = null,
                                        isSelected = selected == entry.key,
                                        onSelect = {
                                            selected = entry.key
                                            commentController.onSelected(entry.model)
                                        },
                                        actions = commentController.actions(entry.model),
                                    )
                                }
                            }
                        }

                        is Item.MessageEntry -> MessageView(services, item.message, isSelected) {
                            selected = item.thing.name
                        }
                    }
                }

                if (loading) item(key = "spinner") { Spinner() }
            }
        }
    }

    /** One button per sort, each as wide as its label with the spare room shared (CSS flex-grow). */
    @Composable
    private fun SortButtons() {
        val styling = LocalStyling.current
        FlexRow(Modifier.fillMaxWidth()) {
            for (option in feed.sorts) {
                Text(
                    option.label,
                    Modifier
                        .background(if (option == sort) styling.tertiaryColor.color else styling.secondaryColor.color)
                        .clickable(role = Role.Button) {
                            sort = option
                            scope.launch { reload() }
                        }
                        .padding(15.dp),
                    color = styling.textColor.color,
                    fontSize = styling.titleFontSize.px,
                    textAlign = TextAlign.Center,
                )
            }
        }
    }
}
