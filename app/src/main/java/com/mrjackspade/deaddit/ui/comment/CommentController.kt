package com.mrjackspade.deaddit.ui.comment

import com.mrjackspade.deaddit.Services
import com.mrjackspade.deaddit.core.blocking.Blocking
import com.mrjackspade.deaddit.core.model.Comment
import com.mrjackspade.deaddit.core.model.Post
import com.mrjackspade.deaddit.core.model.RedditThing
import com.mrjackspade.deaddit.core.model.VoteState
import com.mrjackspade.deaddit.core.service.CommentFocus
import com.mrjackspade.deaddit.ui.Option
import com.mrjackspade.deaddit.ui.Pages

/**
 * What a comment's buttons do (the C# RedditCommentWebComponent's handlers), for any page that
 * shows comments. [post] is the thread when known; [onDeleted] removes the comment from the page;
 * [onReplied] adds the user's new reply under it; [onLink] opens a tapped link. [contextOf] gives
 * the thread above a comment, root first, for the reply and edit pages (the C# Parent chain).
 * What the buttons start runs in the app's scope ([Services.launchAction]), so leaving the page
 * doesn't drop its result.
 */
class CommentController(
    private val services: Services,
    private val post: Post?,
    private val onDeleted: (CommentModel) -> Unit,
    private val onReplied: (CommentModel, Comment) -> Unit,
    private val onLink: (String) -> Unit,
    private val contextOf: (CommentModel) -> List<RedditThing> = { emptyList() },
) {
    fun actions(model: CommentModel) = CommentActions(
        onVote = { direction -> vote(model, direction) },
        onMore = { run { more(model) } },
        onShare = { services.platform.shareText("https://www.reddit.com${model.comment.permalink}", model.comment.body) },
        onReply = {
            Pages.openReply(services, model.comment, contextOf(model)) { reply ->
                onReplied(model, reply.copy(likes = VoteState.Upvote, parentId = model.comment.name))
            }
        },
        onLink = onLink,
    )

    /** Selecting a comment; an unread inbox reply is marked read. */
    fun onSelected(model: CommentModel) {
        model.everSelected = true
        if (model.isNew) run {
            services.reddit.markRead(model.comment, true)
            model.isNew = false
        }
    }

    private fun vote(model: CommentModel, direction: VoteState) {
        val state = model.toggle(direction)
        run { services.reddit.vote(model.comment, state) }
    }

    private suspend fun more(model: CommentModel) {
        val comment = model.comment
        val author = comment.author

        if (!author.equals(services.reddit.loggedInUser, ignoreCase = true)) {
            services.dialogs.choose(
                "Select:",
                Option("Block /u/$author") { newBlockRule(comment) },
                Option("View /u/$author") { Pages.openUser(services, author!!) },
                Option("Tag /u/$author") { tag(author!!) },
                Option(if (model.saved) "Unsave" else "Save") {
                    val saved = !model.saved
                    services.reddit.setSaved(comment, saved)
                    model.saved = saved
                },
                Option("Copy Raw") { services.platform.copy(comment.body) },
                Option("Copy Permalink") { services.platform.copy("https://www.reddit.com${comment.permalink}") },
                Option("View Context") {
                    resolvePost(comment)?.let { Pages.openPost(services, it, CommentFocus(comment)) }
                },
                Option("Report...") { Reporting.report(services, comment, post?.subreddit, "Comment") },
            )
        } else {
            services.dialogs.choose(
                "Select:",
                Option("${if (model.sendReplies) "Disable" else "Enable"} Replies") {
                    val state = !model.sendReplies
                    services.reddit.setInboxReplies(comment, state)
                    model.sendReplies = state
                },
                Option("Delete") {
                    if (services.dialogs.confirm("Confirm Delete", "Are you sure you want to delete this comment?", "Delete", "Cancel")) {
                        services.dialogs.busy { services.reddit.delete(comment) }
                        onDeleted(model)
                    }
                },
                Option("Edit") {
                    Pages.openEdit(services, comment, contextOf(model)) { edited -> model.edited(edited) }
                },
            )
        }
    }

    /** "Continue this thread": the thread again, starting at [comment]. */
    fun continueThread(comment: Comment) = run {
        resolvePost(comment)?.let { Pages.openPost(services, it, CommentFocus(comment, 0)) }
    }

    /** The comment's post: the page's, or fetched (Reddit no longer serves a comment by ID alone). */
    private suspend fun resolvePost(comment: Comment): Post? {
        post?.let { return it }

        if (comment.linkId.isNullOrEmpty()) {
            services.dialogs.message("Unable to locate the parent post for this comment.")
            return null
        }

        val fetched = services.reddit.getPost(comment.linkId!!)
        if (fetched == null) services.dialogs.message("The parent post could not be fetched.")
        return fetched
    }

    private fun newBlockRule(comment: Comment) {
        Pages.openBlockRuleEditor(services, Blocking.fromAuthor(comment)) { rule ->
            services.settings.blocking.update { it.copy(blackList = it.blackList.copy(rules = it.blackList.rules + rule)) }
        }
    }

    private suspend fun tag(author: String) {
        val existing = services.userTags.get(author)
        val prompt = if (existing != null) "Edit tag for /u/$author" else "Tag /u/$author"
        val result = services.dialogs.prompt(prompt, "", existing ?: "") ?: return

        if (result.isBlank()) services.userTags.remove(author) else services.userTags.set(author, result)
    }

    private fun run(block: suspend () -> Unit) {
        services.launchAction(block)
    }
}

/** "Report...": by Reddit's rules or the subreddit's, as the C# pages did. */
object Reporting {
    /** [noun] is "Post" or "Comment", for the confirmation. */
    suspend fun report(services: Services, thing: RedditThing, subreddit: String?, noun: String) {
        services.dialogs.choose(
            "Report:",
            Option("Breaks Reddit Rules") { byRedditRules(services, thing, subreddit, noun) },
            Option("Breaks Subreddit Rules") { bySubredditRules(services, thing, subreddit, noun) },
        )
    }

    private suspend fun byRedditRules(services: Services, thing: RedditThing, subreddit: String?, noun: String) {
        if (subreddit.isNullOrEmpty()) {
            services.dialogs.message("Unable to determine subreddit")
            return
        }

        val rules = services.reddit.rules(subreddit)
        if (rules?.siteRules.isNullOrEmpty()) {
            services.dialogs.alert("Report", "No Reddit rules available")
            return
        }

        services.dialogs.choose(
            "Select Rule:",
            *rules!!.siteRules.map { rule ->
                Option(rule) {
                    services.reddit.report(thing, siteReason = rule)
                    services.dialogs.alert("Report", "$noun reported successfully")
                }
            }.toTypedArray(),
        )
    }

    private suspend fun bySubredditRules(services: Services, thing: RedditThing, subreddit: String?, noun: String) {
        if (subreddit.isNullOrEmpty()) {
            services.dialogs.message("Unable to determine subreddit")
            return
        }

        val rules = services.reddit.rules(subreddit)
        if (rules?.rules.isNullOrEmpty()) {
            services.dialogs.alert("Report", "r/$subreddit has no specific rules")
            return
        }

        services.dialogs.choose(
            "Select Rule:",
            *rules!!.rules.map { rule ->
                Option(rule.shortName) {
                    services.reddit.report(thing, ruleReason = rule.violationReason ?: rule.shortName)
                    services.dialogs.alert("Report", "$noun reported successfully")
                }
            }.toTypedArray(),
        )
    }
}
