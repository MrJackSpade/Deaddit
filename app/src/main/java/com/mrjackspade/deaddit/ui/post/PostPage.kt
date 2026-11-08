package com.mrjackspade.deaddit.ui.post

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
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
import androidx.compose.ui.Modifier
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
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
import com.mrjackspade.deaddit.ui.FlexRow
import com.mrjackspade.deaddit.ui.Links
import com.mrjackspade.deaddit.ui.LocalStyling
import com.mrjackspade.deaddit.ui.Option
import com.mrjackspade.deaddit.ui.Pages
import com.mrjackspade.deaddit.ui.Screen
import com.mrjackspade.deaddit.ui.Spinner
import com.mrjackspade.deaddit.ui.color
import com.mrjackspade.deaddit.ui.comment.CommentController
import com.mrjackspade.deaddit.ui.comment.CommentEntry
import com.mrjackspade.deaddit.ui.comment.CommentModel
import com.mrjackspade.deaddit.ui.comment.CommentView
import com.mrjackspade.deaddit.ui.comment.ThreadEntry
import com.mrjackspade.deaddit.ui.comment.ThreadRowFrame
import com.mrjackspade.deaddit.ui.comment.findEntry
import com.mrjackspade.deaddit.ui.comment.flattenThread
import com.mrjackspade.deaddit.ui.comment.removeEntry
import com.mrjackspade.deaddit.ui.html.HtmlBody
import com.mrjackspade.deaddit.ui.px
import kotlinx.coroutines.launch

/**
 * A post and its comments (the C# app's PostPage). The thread is a tree of [ThreadEntry]s, drawn
 * as a flat list with level lines so only what's on screen is laid out, however big the thread.
 */
class PostPage(
    private val services: Services,
    private val post: Post,
    private val focus: CommentFocus?,
    private val fromHistory: Boolean,
) : Screen() {

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
    ) : ThreadEntry(key, parent) {
        var clicked = false
        var loading by mutableStateOf(false)
        val count: Int get() = node?.more?.count ?: collapsed!!.size
    }

    private val postModel = PostModel(post, PostState.None)
    private val root = mutableStateListOf<ThreadEntry>()

    /** Numbers the "more" rows: Reddit names every "continue this thread" stub alike (t1__). */
    private var nextMoreKey = 0

    private var loading by mutableStateOf(false)
    private var selected by mutableStateOf<String?>(null)
    private var autoLoadImages by mutableStateOf(services.settings.hacks.value.autoLoadCommentImages)

    /** The image button's label, set once: the C# button kept showing 🖼 after it was tapped. */
    private val imageButtonLabel = if (autoLoadImages) "" else "🖼"

    private val postBody = run {
        val visible = post.body.trim().filter { it != '‌' }
        if (post.body.isNotBlank() && visible.length > services.settings.hacks.value.minimumPostBodyLength) {
            post.bodyHtml?.let(RedditHtml::parse)
        } else {
            null
        }
    }

    private val postController = PostController(
        services, inList = false, fromHistory = fromHistory,
        onSelect = { _, _ -> },
    )

    private val commentController = CommentController(
        services,
        post = post,
        onDeleted = { model -> removeEntry(root, model) },
        onReplied = { parent, reply -> addReply(parent, reply) },
        onLink = ::openLink,
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
            } catch (e: Throwable) {
                services.dialogs.error(e)
            } finally {
                loading = false
            }
        }
    }

    private fun openLink(url: String) {
        services.appScope.launch { Links.open(services, url) }
    }

    /**
     * Adds a level of the thread under [parent]. Comments Reddit collapsed are grouped by reason
     * behind one button each, unless this is the result of opening such a button ([loadMore]).
     */
    private fun addChildren(container: MutableList<ThreadEntry>, parent: CommentEntry?, nodes: List<ThreadNode>, loadMore: Boolean) {
        val collapsed = LinkedHashMap<CollapsedReasonCode, MutableList<CommentNode>>()
        val added = ArrayList<ThreadEntry>()

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
                    added += entry
                }
                is MoreNode -> added += MoreEntry("more-${nextMoreKey++}", parent, node = node)
            }
        }

        for ((reason, comments) in collapsed) {
            added += MoreEntry("more-${nextMoreKey++}", parent, collapsed = comments, reason = reason)
        }

        container += added
    }

    private fun shouldRender(node: ThreadNode): Boolean {
        val comment = (node as? CommentNode)?.comment ?: return true
        if (!Blocking.isAllowed(services.settings.blocking.value, comment)) return false
        if (comment.id == post.id) return false
        if ((comment.isRemoved || comment.isDeleted) && node.replies.isEmpty()) return false
        return true
    }

    private fun containerOf(entry: ThreadEntry): MutableList<ThreadEntry> = entry.parent?.children ?: root

    /** Puts [nodes] where [entry] is, in its place. */
    private fun replace(entry: MoreEntry, nodes: List<ThreadNode>) {
        val container = containerOf(entry)
        val added = ArrayList<ThreadEntry>()
        addChildren(added, entry.parent, nodes, loadMore = true)
        val index = container.indexOf(entry)
        container.removeAt(index)
        container.addAll(index, added)
    }

    private fun onMore(entry: MoreEntry) {
        // A collapsed group already holds its comments.
        if (entry.collapsed != null) {
            replace(entry, entry.collapsed)
            return
        }

        val node = entry.node!!

        // Under a comment, no children to load means "continue this thread", from that comment.
        if (entry.parent != null && !node.more.loadsInPlace) {
            services.launchAction {
                val parent = node.parent ?: throw IllegalStateException("For some reason the more comment parent wasn't a proper comment")
                commentController.continueThread(parent)
            }
            return
        }

        if (entry.clicked) return
        entry.clicked = true

        // At the top of the thread the C# page took the button away before loading, so one that
        // fails is gone rather than left dead; a "continue this thread" there loads nothing.
        val topLevel = entry.parent == null

        services.appScope.launch {
            entry.loading = true
            try {
                replace(entry, services.reddit.moreComments(post, node))
            } catch (e: Throwable) {
                if (topLevel) containerOf(entry).remove(entry)
                services.dialogs.error(e)
            } finally {
                entry.loading = false
            }
        }
    }

    private fun addReply(parent: CommentModel, reply: Comment) {
        val parentEntry = findEntry(root, parent) ?: throw IllegalStateException("Replied to a comment that isn't on the page.")
        parentEntry.children.add(0, CommentEntry(CommentModel(reply), parentEntry))
    }

    /** The thread above a comment, root first: the post, then each comment it's under. */
    private fun contextOf(model: CommentModel): List<RedditThing> {
        val entry = findEntry(root, model) ?: throw IllegalStateException("A comment that isn't on the page.")
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
    }

    @Composable
    override fun Content() {
        val styling = LocalStyling.current

        LaunchedEffect(Unit) { start() }

        val rows by remember { derivedStateOf { flattenThread(root) } }

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
                        onLink = ::openLink,
                        modifier = Modifier.fillMaxWidth().padding(2.dp)
                            .border(1.dp, styling.textColor.color)
                            .background(styling.primaryColor.color)
                            .padding(5.dp),
                    )
                }
            }

            // The comment container's 5px right padding.
            items(rows, key = { it.entry.key }) { row ->
                ThreadRowFrame(row, selected, endPadding = 5.dp) {
                    when (val entry = row.entry) {
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
                        )

                        is MoreEntry -> MoreButton(entry)
                    }
                }
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

        FlexRow(Modifier.fillMaxWidth().background(styling.secondaryColor.color)) {
            Button("Share", "Share") {
                services.launchAction {
                    services.dialogs.choose(
                        "Share:",
                        // As in the C# app, "File" shares the post's link.
                        Option("File") { postController.shareLink(post) },
                        Option("Link") { postController.shareLink(post) },
                        Option("Comments") { postController.shareComments(post) },
                    )
                }
            }
            Button(if (postModel.saved) "Unsave" else "Save", "Save") { services.launchAction { postController.toggleSave(postModel) } }
            Button("...", "More") { services.launchAction { postController.more(postModel) } }
            Button(imageButtonLabel, "Load images") { autoLoadImages = true }
            Button("Reply", "Reply") { Pages.openReply(services, post) { reply -> addTopLevelReply(reply) } }
        }
    }

    /** With images on, a link the URL handlers can show inline is drawn as its image. */
    private fun inlineMedia(url: String): (@Composable () -> Unit)? {
        if (!autoLoadImages || !services.posts.urls.canInline(url)) return null
        val image = services.posts.urls.inlineUrl(url) ?: return null
        return {
            com.mrjackspade.deaddit.ui.media.InlineImage(image, url, onClick = { openLink(url) })
        }
    }

    /** The C# MoreCommentsWebComponent: a full-width block, padded 5px left, top and bottom. */
    @Composable
    private fun MoreButton(entry: MoreEntry) {
        val styling = LocalStyling.current

        val label = when (entry.reason) {
            null -> if (entry.node!!.more.loadsInPlace) "More [${entry.count}]" else "Continue Thread"
            CollapsedReasonCode.Deleted -> "Deleted [${entry.count}]"
            CollapsedReasonCode.LowScore, CollapsedReasonCode.ScoreBelowThreshold -> "Low Score [${entry.count}]"
            CollapsedReasonCode.BlockedAuthor -> "Blocked [${entry.count}]"
            // The C# app labelled any other reason with its name.
            CollapsedReasonCode.Empty -> "Empty"
        }

        if (entry.loading) {
            Box(Modifier.fillMaxWidth()) { Spinner() }
        } else {
            Text(
                label,
                Modifier.fillMaxWidth().background(styling.primaryColor.color)
                    .clickable(role = Role.Button) { onMore(entry) }.padding(start = 5.dp, top = 5.dp, bottom = 5.dp),
                color = styling.textColor.color,
                fontSize = (styling.titleFontSize * 0.9).toInt().toDouble().px,
            )
        }
    }
}
