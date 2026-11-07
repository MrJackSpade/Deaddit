package com.mrjackspade.deaddit.ui.post

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.IntrinsicSize
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.lazy.rememberLazyListState
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import coil3.compose.AsyncImage
import com.mrjackspade.deaddit.Services
import com.mrjackspade.deaddit.core.blocking.Blocking
import com.mrjackspade.deaddit.core.config.PostState
import com.mrjackspade.deaddit.core.html.RedditHtml
import com.mrjackspade.deaddit.core.model.CollapsedReasonCode
import com.mrjackspade.deaddit.core.model.Comment
import com.mrjackspade.deaddit.core.model.Post
import com.mrjackspade.deaddit.core.model.RedditThing
import com.mrjackspade.deaddit.core.model.VoteState
import com.mrjackspade.deaddit.core.model.isDeleted
import com.mrjackspade.deaddit.core.model.isRemoved
import com.mrjackspade.deaddit.core.service.CommentFocus
import com.mrjackspade.deaddit.core.thread.CommentNode
import com.mrjackspade.deaddit.core.thread.MoreNode
import com.mrjackspade.deaddit.core.thread.ThreadNode
import com.mrjackspade.deaddit.ui.Links
import com.mrjackspade.deaddit.ui.LocalStyling
import com.mrjackspade.deaddit.ui.Pages
import com.mrjackspade.deaddit.ui.Screen
import com.mrjackspade.deaddit.ui.Spinner
import com.mrjackspade.deaddit.ui.color
import com.mrjackspade.deaddit.ui.comment.CommentController
import com.mrjackspade.deaddit.ui.comment.CommentModel
import com.mrjackspade.deaddit.ui.comment.CommentView
import com.mrjackspade.deaddit.ui.html.HtmlBody
import com.mrjackspade.deaddit.ui.px
import kotlinx.coroutines.launch

/**
 * A post and its comments (the C# app's PostPage). The thread is a tree of [Entry]s, drawn as a
 * flat list with depth guides so only what's on screen is laid out, however big the thread.
 */
class PostPage(
    private val services: Services,
    private val post: Post,
    private val focus: CommentFocus?,
    private val fromHistory: Boolean,
) : Screen() {

    /** A row of the thread: a comment (with its replies), or a "more" button. */
    private sealed class Entry(val key: String, val parent: CommentEntry?) {
        val depth: Int = if (parent == null) 0 else parent.depth + 1
    }

    private class CommentEntry(val model: CommentModel, parent: CommentEntry?) : Entry(model.comment.name, parent) {
        val children = mutableListOf<Entry>()
    }

    /**
     * Reddit's "load more" ([node] set), or a group of comments Reddit collapsed for one reason
     * ([collapsed] set), already loaded and shown on tap.
     */
    private class MoreEntry(
        key: String,
        parent: CommentEntry?,
        val node: MoreNode? = null,
        val collapsed: List<CommentNode>? = null,
        val reason: CollapsedReasonCode? = null,
    ) : Entry(key, parent) {
        var clicked = false
        val count: Int get() = node?.more?.count ?: collapsed!!.size
    }

    private val postModel = PostModel(post, PostState.None)
    private val root = mutableListOf<Entry>()

    /** Bumped on every change to the tree, to redraw the flattened list. */
    private var version by mutableIntStateOf(0)
    private var loading by mutableStateOf(false)
    private var loadingMore by mutableStateOf<MoreEntry?>(null)
    private var selected by mutableStateOf<String?>(null)
    private var autoLoadImages by mutableStateOf(services.settings.hacks.value.autoLoadCommentImages)

    private val postBody = run {
        val visible = post.body.trim().filter { it != '‌' }
        if (post.body.isNotBlank() && visible.length > services.settings.hacks.value.minimumPostBodyLength) {
            post.bodyHtml?.let(RedditHtml::parse)
        } else {
            null
        }
    }

    private val postController = PostController(
        services, scope, inList = false, fromHistory = fromHistory,
        onSelect = { _, _ -> },
    )

    private val commentController = CommentController(
        services,
        scope,
        post = post,
        onDeleted = { model -> removeComment(model) },
        onReplied = { parent, reply -> addReply(parent, reply) },
        onLink = { url -> scope.launch { Links.open(services, url) } },
        contextOf = ::contextOf,
    )

    private var started = false

    private fun start() {
        if (started) return
        started = true
        scope.launch {
            loading = true
            try {
                addChildren(root, null, services.reddit.comments(post, focus), loadMore = false)
            } catch (e: Exception) {
                services.dialogs.error(e)
            } finally {
                loading = false
            }
        }
    }

    /**
     * Adds a level of the thread under [parent]. Comments Reddit collapsed are grouped by reason
     * behind one button each, unless this is the result of opening such a button ([loadMore]).
     */
    private fun addChildren(container: MutableList<Entry>, parent: CommentEntry?, nodes: List<ThreadNode>, loadMore: Boolean) {
        val collapsed = LinkedHashMap<CollapsedReasonCode, MutableList<CommentNode>>()

        for (node in nodes) {
            val reason = (node as? CommentNode)?.comment?.collapsedReasonCode
            if (!loadMore && reason != null) {
                collapsed.getOrPut(reason) { mutableListOf() } += node
                continue
            }

            if (!shouldRender(node)) continue

            when (node) {
                is CommentNode -> {
                    val entry = CommentEntry(CommentModel(node.comment), parent)
                    addChildren(entry.children, entry, node.replies, loadMore = false)
                    container += entry
                }
                is MoreNode -> container += MoreEntry("more-${node.more.name}", parent, node = node)
            }
        }

        for ((reason, comments) in collapsed) {
            container += MoreEntry("collapsed-${parent?.key}-$reason-${comments.first().comment.name}", parent, collapsed = comments, reason = reason)
        }

        version++
    }

    private fun shouldRender(node: ThreadNode): Boolean {
        val comment = (node as? CommentNode)?.comment ?: return true
        if (!Blocking.isAllowed(services.settings.blocking.value, comment)) return false
        if (comment.id == post.id) return false
        if ((comment.isRemoved || comment.isDeleted) && node.replies.isEmpty()) return false
        return true
    }

    private fun containerOf(entry: Entry): MutableList<Entry> = entry.parent?.children ?: root

    private fun onMore(entry: MoreEntry) {
        val node = entry.node

        // A collapsed group already holds its comments.
        if (entry.collapsed != null) {
            val container = containerOf(entry)
            val index = container.indexOf(entry)
            container.removeAt(index)
            val added = mutableListOf<Entry>()
            addChildren(added, entry.parent, entry.collapsed, loadMore = true)
            container.addAll(index, added)
            version++
            return
        }

        // No children to load means "continue this thread" (only possible under a comment).
        if (node!!.more.children.isEmpty()) {
            val parent = node.parent ?: throw IllegalStateException("For some reason the more comment parent wasn't a proper comment")
            commentController.continueThread(parent)
            return
        }

        if (entry.clicked) return
        entry.clicked = true

        scope.launch {
            loadingMore = entry
            try {
                val loaded = services.reddit.moreComments(post, node)
                val container = containerOf(entry)
                val index = container.indexOf(entry)
                container.removeAt(index)
                val added = mutableListOf<Entry>()
                addChildren(added, entry.parent, loaded, loadMore = true)
                container.addAll(index, added)
                version++
            } catch (e: Exception) {
                services.dialogs.error(e)
            } finally {
                loadingMore = null
            }
        }
    }

    private fun removeComment(model: CommentModel) {
        fun remove(list: MutableList<Entry>): Boolean {
            if (list.removeAll { it is CommentEntry && it.model === model }) return true
            return list.any { it is CommentEntry && remove(it.children) }
        }
        remove(root)
        version++
    }

    /** The entry showing [model], wherever it is in the tree. */
    private fun entryOf(model: CommentModel): CommentEntry? {
        fun find(list: List<Entry>): CommentEntry? {
            for (entry in list) {
                if (entry is CommentEntry) {
                    if (entry.model === model) return entry
                    find(entry.children)?.let { return it }
                }
            }
            return null
        }
        return find(root)
    }

    private fun addReply(parent: CommentModel, reply: Comment) {
        val parentEntry = entryOf(parent) ?: throw IllegalStateException("Replied to a comment that isn't on the page.")
        parentEntry.children.add(0, CommentEntry(CommentModel(reply), parentEntry))
        version++
    }

    /** The thread above a comment, root first: the post, then each comment it's under. */
    private fun contextOf(model: CommentModel): List<RedditThing> {
        val entry = entryOf(model) ?: throw IllegalStateException("A comment that isn't on the page.")
        val chain = ArrayList<RedditThing>()
        var ancestor = entry.parent
        while (ancestor != null) {
            chain.add(0, ancestor.model.comment)
            ancestor = ancestor.parent
        }
        chain.add(0, post)
        return chain
    }

    private fun addTopLevelReply(reply: Comment) {
        root.add(0, CommentEntry(CommentModel(reply.copy(likes = VoteState.Upvote)), null))
        version++
    }

    /** The tree in reading order. */
    private fun flatten(): List<Entry> {
        val rows = ArrayList<Entry>()
        fun walk(list: List<Entry>) {
            for (entry in list) {
                rows += entry
                if (entry is CommentEntry) walk(entry.children)
            }
        }
        walk(root)
        return rows
    }

    @Composable
    override fun Content() {
        val styling = LocalStyling.current

        LaunchedEffect(Unit) { start() }

        val rows = remember(version) { flatten() }

        LazyColumn(
            Modifier.fillMaxSize().background(styling.secondaryColor.color).statusBarsPadding().navigationBarsPadding(),
            rememberLazyListState(),
        ) {
            item(key = "post") {
                PostRow(
                    postModel,
                    selected = false,
                    inList = false,
                    hideScore = services.settings.hacks.value.hideSelfKarma && post.author.equals(services.reddit.loggedInUser, ignoreCase = true),
                    actions = postController.actions(postModel),
                )
            }

            item(key = "actions") { ActionBar() }

            postBody?.let { blocks ->
                item(key = "body") {
                    HtmlBody(
                        blocks,
                        styling.titleFontSize.px,
                        onLink = { url -> scope.launch { Links.open(services, url) } },
                        modifier = Modifier.fillMaxWidth().padding(2.dp)
                            .border(1.dp, styling.textColor.color)
                            .background(styling.primaryColor.color)
                            .padding(5.dp),
                    )
                }
            }

            items(rows, key = { it.key }) { entry ->
                ThreadRow(entry)
            }

            if (loading) item(key = "spinner") { Spinner() }
        }
    }

    @Composable
    private fun ActionBar() {
        val styling = LocalStyling.current

        @Composable
        fun Button(text: String, description: String, onClick: () -> Unit) {
            Text(
                text,
                Modifier.clickable(role = Role.Button, onClick = onClick).padding(10.dp)
                    .semantics { contentDescription = description },
                color = styling.textColor.color,
                fontSize = styling.titleFontSize.px,
                textAlign = TextAlign.Center,
            )
        }

        fun run(block: suspend () -> Unit) {
            scope.launch {
                try {
                    block()
                } catch (e: Exception) {
                    services.dialogs.error(e)
                }
            }
        }

        com.mrjackspade.deaddit.ui.FlexRow(Modifier.fillMaxWidth().background(styling.secondaryColor.color)) {
            Button("Share", "Share") {
                run {
                    services.dialogs.choose(
                        "Share:",
                        // As in the C# app, "File" shares the post's link.
                        com.mrjackspade.deaddit.ui.Option("File") { postController.shareLink(post) },
                        com.mrjackspade.deaddit.ui.Option("Link") { postController.shareLink(post) },
                        com.mrjackspade.deaddit.ui.Option("Comments") { postController.shareComments(post) },
                    )
                }
            }
            Button(if (postModel.saved) "Unsave" else "Save", "Save") { run { postController.toggleSave(postModel) } }
            Button("...", "More") { run { postController.more(postModel) } }
            Button(if (autoLoadImages) "" else "🖼", "Load images") { autoLoadImages = true }
            Button("Reply", "Reply") { Pages.openReply(services, post) { reply -> addTopLevelReply(reply) } }
        }
    }

    @Composable
    private fun ThreadRow(entry: Entry) {
        val styling = LocalStyling.current

        Row(Modifier.fillMaxWidth().height(IntrinsicSize.Min).padding(end = 5.dp)) {
            // One rule per level, in the highlight color when the comment it hangs from is selected.
            var ancestor = entry.parent
            val rules = ArrayList<Boolean>()
            while (ancestor != null) {
                rules.add(0, selected == ancestor.key)
                ancestor = ancestor.parent
            }

            Spacer(Modifier.width(5.dp))
            for (highlighted in rules) {
                Box(Modifier.width(1.dp).fillMaxHeight().background(if (highlighted) styling.highlightColor.color else styling.subTextColor.color))
                Spacer(Modifier.width(13.dp))
            }

            when (entry) {
                is CommentEntry -> CommentView(
                    services,
                    entry.model,
                    post,
                    isSelected = selected == entry.key,
                    onSelect = {
                        selected = entry.key
                        commentController.onSelected(entry.model)
                    },
                    actions = commentController.actions(entry.model),
                    inline = ::inlineMedia,
                    modifier = Modifier.weight(1f).padding(vertical = 5.dp),
                )

                is MoreEntry -> MoreButton(entry, Modifier.weight(1f))
            }
        }
    }

    /** With images on, a link the URL handlers can show inline is drawn as its image. */
    private fun inlineMedia(url: String): (@Composable () -> Unit)? {
        if (!autoLoadImages || !services.posts.urls.canInline(url)) return null
        val image = services.posts.urls.inlineUrl(url) ?: return null
        return {
            com.mrjackspade.deaddit.ui.media.InlineImage(image, url, onClick = { scope.launch { Links.open(services, url) } })
        }
    }

    @Composable
    private fun MoreButton(entry: MoreEntry, modifier: Modifier) {
        val styling = LocalStyling.current

        val label = when (entry.reason) {
            null -> if (entry.node!!.more.children.isNotEmpty()) "More [${entry.count}]" else "Continue Thread"
            CollapsedReasonCode.Deleted -> "Deleted [${entry.count}]"
            CollapsedReasonCode.LowScore, CollapsedReasonCode.ScoreBelowThreshold -> "Low Score [${entry.count}]"
            CollapsedReasonCode.BlockedAuthor -> "Blocked [${entry.count}]"
        }

        if (loadingMore === entry) {
            Box(modifier) { Spinner() }
        } else {
            Text(
                label,
                modifier.padding(vertical = 5.dp).background(styling.primaryColor.color)
                    .clickable(role = Role.Button) { onMore(entry) }.padding(5.dp),
                color = styling.textColor.color,
                fontSize = (styling.titleFontSize * 0.9).toInt().toDouble().px,
            )
        }
    }
}
