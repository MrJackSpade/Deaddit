package com.mrjackspade.deaddit.ui.post

import com.mrjackspade.deaddit.Services
import com.mrjackspade.deaddit.core.blocking.Blocking
import com.mrjackspade.deaddit.core.config.BlockRule
import com.mrjackspade.deaddit.core.model.Post
import com.mrjackspade.deaddit.core.model.VoteState
import com.mrjackspade.deaddit.core.model.link
import com.mrjackspade.deaddit.ui.Option
import com.mrjackspade.deaddit.ui.Pages
import com.mrjackspade.deaddit.ui.comment.Reporting

/**
 * What a post's buttons do (the C# RedditPostWebComponent and PostPage handlers).
 * [inList] is true in a feed; [onBlockAdded] lets the feed drop what a new block rule matches;
 * [onHidden] removes the post from the page. What the buttons start runs in the app's scope
 * ([Services.launchAction]), so leaving the page doesn't drop its result.
 */
class PostController(
    private val services: Services,
    private val inList: Boolean,
    private val fromHistory: Boolean,
    /** Selects the post; toggles when the second argument is true. */
    private val onSelect: (PostModel, Boolean) -> Unit,
    private val onBlockAdded: (BlockRule) -> Unit = {},
    private val onHidden: (PostModel) -> Unit = {},
) {
    fun actions(model: PostModel) = PostActions(
        onSelect = { onSelect(model, true) },
        onThumbnail = { run { thumbnail(model) } },
        onVote = { direction ->
            val state = model.toggle(direction)
            run { services.reddit.vote(model.post, state) }
        },
        onShare = { run { share(model) } },
        onSave = { run { toggleSave(model) } },
        // The C# app called Reddit's hide only when IRedditClient.CanLogIn: when it had API
        // credentials (a username, password, app key and secret), which it was never given
        // (MauiProgram made its RedditService without them). So Hide only took the post off the page.
        onHide = { onHidden(model) },
        onMore = { run { more(model) } },
        onComments = { run { comments(model) } },
    )

    private fun comments(model: PostModel) {
        if (model.post.isSelf) {
            model.visited = true
            services.visits.visit(model.post)
        }
        Pages.openPost(services, model.post, fromHistory = fromHistory)
    }

    private suspend fun thumbnail(model: PostModel) {
        val post = model.post
        if (post.isSelf && !inList) return

        if (!services.posts.canLaunch(post)) {
            services.dialogs.message("Can not handle post")
            return
        }

        if (inList) {
            model.visited = true
            onSelect(model, false)
            services.visits.visit(post)
        }

        if (!fromHistory) services.history.add(post.name)

        services.posts.launch(post)
    }

    suspend fun toggleSave(model: PostModel) {
        val saved = !model.saved
        services.reddit.setSaved(model.post, saved)
        model.saved = saved
    }

    suspend fun share(model: PostModel) {
        val post = model.post
        if (services.posts.canDownload(post)) {
            services.dialogs.choose(
                "Share:",
                Option("File") { shareFile(post) },
                Option("Link") { shareLink(post) },
                Option("Comments") { shareComments(post) },
            )
        } else {
            services.dialogs.choose("Share:", Option("Link") { shareLink(post) }, Option("Comments") { shareComments(post) })
        }
    }

    private suspend fun shareFile(post: Post) {
        // Galleries and Reddit videos first, then whatever the link points at.
        if (services.posts.canShare(post)) {
            services.posts.share(post)
            return
        }

        if (services.posts.urls.canDownload(post.link, services.posts)) {
            services.media.shareDownloads(listOf(services.posts.urls.download(post.link, services.posts)))
        }
    }

    fun shareLink(post: Post) = services.platform.shareText(post.link, post.title)

    fun shareComments(post: Post) = services.platform.shareText("https://www.reddit.com${post.permalink}", post.title)

    suspend fun more(model: PostModel) {
        val post = model.post
        val options = mutableListOf(
            Option("View...") { view(post) },
            Option("Block...") { blockOrWhitelist(post, whitelist = false) },
            Option("Whitelist...") { blockOrWhitelist(post, whitelist = true) },
        )
        // The thread page also offers sharing here.
        if (!inList) {
            options += Option("Share...") {
                services.dialogs.choose("Share:", Option("Link") { shareLink(post) }, Option("Comments") { shareComments(post) })
            }
        }
        options += Option("Report...") { Reporting.report(services, post, post.subreddit, "Post") }

        services.dialogs.choose("Select:", *options.toTypedArray())
    }

    private suspend fun view(post: Post) {
        services.dialogs.choose(
            "View:",
            Option("/u/${post.author}") { Pages.openUser(services, post.author!!) },
            Option("/r/${post.subreddit}") { Pages.openSubreddit(services, post.subreddit!!) },
        )
    }

    private suspend fun blockOrWhitelist(post: Post, whitelist: Boolean) {
        services.dialogs.choose(
            if (whitelist) "Whitelist:" else "Block:",
            Option("/u/${post.author}") { newRule(Blocking.fromAuthor(post), whitelist) },
            Option("/r/${post.subreddit}") { newRule(Blocking.fromSubReddit(post), whitelist) },
            Option(post.domain) { newRule(Blocking.fromDomain(post), whitelist) },
            Option(post.linkFlairText) { newRule(Blocking.fromFlair(post), whitelist) },
        )
    }

    private fun newRule(rule: BlockRule, whitelist: Boolean) {
        Pages.openBlockRuleEditor(services, rule) { saved ->
            services.settings.blocking.update {
                if (whitelist) {
                    it.copy(whiteList = it.whiteList.copy(rules = it.whiteList.rules + saved))
                } else {
                    it.copy(blackList = it.blackList.copy(rules = it.blackList.rules + saved))
                }
            }
            if (!whitelist) onBlockAdded(saved)
        }
    }

    private fun run(block: suspend () -> Unit) {
        services.launchAction(block)
    }
}
