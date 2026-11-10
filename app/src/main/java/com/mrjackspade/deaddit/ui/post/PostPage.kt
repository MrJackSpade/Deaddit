package com.mrjackspade.deaddit.ui.post

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.lazy.rememberLazyListState
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.outlined.ArrowBack
import androidx.compose.material.icons.automirrored.outlined.Reply
import androidx.compose.material.icons.outlined.Bookmark
import androidx.compose.material.icons.outlined.BookmarkBorder
import androidx.compose.material.icons.outlined.ExpandMore
import androidx.compose.material.icons.outlined.Image
import androidx.compose.material.icons.outlined.MoreHoriz
import androidx.compose.material.icons.outlined.Share
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.derivedStateOf
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.unit.dp
import com.mrjackspade.deaddit.ui.Divider
import com.mrjackspade.deaddit.ui.IconAction
import com.mrjackspade.deaddit.ui.PillButton
import com.mrjackspade.deaddit.ui.TopBar
import com.mrjackspade.deaddit.ui.TopBarButton
import com.mrjackspade.deaddit.ui.innerShape
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
    }

    private val postModel = PostModel(post, PostState.None)
    private val root = mutableStateListOf<ThreadEntry>()

    /** Numbers the "more" rows: Reddit names every "continue this thread" stub alike (t1__). */
    private var nextMoreKey = 0

    private var loading by mutableStateOf(false)
    private var selected by mutableStateOf<String?>(null)
    private var autoLoadImages by mutableStateOf(services.settings.hacks.value.autoLoadCommentImages)

    /** Whether the load-images button shows, set once: the C# button kept showing 🖼 after it was tapped. */
    private val showImageButton = !autoLoadImages

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

        Column(Modifier.fillMaxSize().background(styling.secondaryColor.color)) {
            TopBar {
                TopBarButton(Icons.AutoMirrored.Outlined.ArrowBack, "Back", color = styling.textColor.color) { services.navigator.remove(this@PostPage) }
                Text(
                    "r/${post.subreddit}",
                    Modifier.weight(1f).clickable { Pages.openSubredditAbout(services, post.subreddit!!) }.padding(horizontal = 8.dp, vertical = 10.dp),
                    color = styling.subTextColor.color,
                    fontSize = styling.subTextFontSize.px,
                    maxLines = 1,
                )
                TopBarButton(Icons.Outlined.MoreHoriz, "More") { services.launchAction { postController.more(postModel) } }
            }

        LazyColumn(
            Modifier.fillMaxSize().navigationBarsPadding(),
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

            postBody?.let { blocks ->
                item(key = "body") {
                    HtmlBody(
                        blocks,
                        styling.commentFontSize.px,
                        onLink = ::openLink,
                        modifier = Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 4.dp),
                    )
                }
            }

            item(key = "actions") { ActionBar() }

            items(rows, key = { it.entry.key }) { row ->
                ThreadRowFrame(row, selected) {
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
    }

    /** The post's own actions, then the line that heads the comments. */
    @Composable
    private fun ActionBar() {
        val styling = LocalStyling.current

        Column(Modifier.fillMaxWidth()) {
            Row(Modifier.fillMaxWidth().padding(horizontal = 10.dp, vertical = 2.dp), verticalAlignment = Alignment.CenterVertically) {
                IconAction(
                    if (postModel.saved) Icons.Outlined.Bookmark else Icons.Outlined.BookmarkBorder,
                    if (postModel.saved) "Unsave" else "Save",
                    active = postModel.saved,
                    tint = if (postModel.saved) styling.accentColor.color else styling.subTextColor.color,
                ) { services.launchAction { postController.toggleSave(postModel) } }
                IconAction(Icons.Outlined.Share, "Share") {
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
                // Shown until tapped, as the C# 🖼 button, which stayed after it was tapped.
                if (showImageButton) {
                    IconAction(Icons.Outlined.Image, "Load images", active = autoLoadImages) { autoLoadImages = true }
                }
                Spacer(Modifier.weight(1f))
                PillButton("Reply", icon = Icons.AutoMirrored.Outlined.Reply) {
                    Pages.openReply(services, post) { reply -> addTopLevelReply(reply) }
                }
            }
            Divider(Modifier.padding(top = 6.dp))
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

    /** The C# MoreCommentsWebComponent, as a quiet chip with its count, which loads or opens the rest. */
    @Composable
    private fun MoreButton(entry: MoreEntry) {
        val styling = LocalStyling.current

        val collapsed = entry.collapsed?.size
        val label = when (entry.reason) {
            // www gives no count for its later top-level pages.
            null -> if (entry.node!!.more.loadsInPlace) entry.node.more.count?.let { "More [$it]" } ?: "More" else "Continue Thread"
            CollapsedReasonCode.Deleted -> "Deleted [$collapsed]"
            CollapsedReasonCode.LowScore, CollapsedReasonCode.ScoreBelowThreshold -> "Low Score [$collapsed]"
            CollapsedReasonCode.BlockedAuthor -> "Blocked [$collapsed]"
            // The C# app labelled any other reason with its name.
            CollapsedReasonCode.Empty -> "Empty"
        }

        if (entry.loading) {
            Box(Modifier.fillMaxWidth()) { Spinner() }
        } else {
            Row(
                Modifier.padding(vertical = 6.dp).heightIn(min = 40.dp).clip(styling.innerShape).background(styling.tertiaryColor.color)
                    .clickable(role = Role.Button) { onMore(entry) }.padding(start = 8.dp, end = 12.dp),
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(6.dp),
            ) {
                Icon(Icons.Outlined.ExpandMore, contentDescription = null, tint = styling.subTextColor.color, modifier = Modifier.size(20.dp))
                Text(label, color = styling.subTextColor.color, fontSize = styling.subTextFontSize.px)
            }
        }
    }
}
