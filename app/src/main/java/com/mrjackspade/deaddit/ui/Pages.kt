package com.mrjackspade.deaddit.ui

import com.mrjackspade.deaddit.Services
import com.mrjackspade.deaddit.core.config.InboxSort
import com.mrjackspade.deaddit.core.config.PostSort
import com.mrjackspade.deaddit.core.config.UserProfileSort
import com.mrjackspade.deaddit.core.feed.Feed
import com.mrjackspade.deaddit.core.feed.Sort
import com.mrjackspade.deaddit.core.feed.toSort
import com.mrjackspade.deaddit.core.config.BlockRule
import com.mrjackspade.deaddit.core.model.Comment
import com.mrjackspade.deaddit.core.model.Post
import com.mrjackspade.deaddit.core.model.Message
import com.mrjackspade.deaddit.core.model.RedditThing
import com.mrjackspade.deaddit.core.model.User
import com.mrjackspade.deaddit.core.service.CommentFocus
import com.mrjackspade.deaddit.ui.feed.FeedPage
import com.mrjackspade.deaddit.ui.post.PostPage

/** Opens pages (the C# app's AppNavigator). */
object Pages {
    fun openFeed(services: Services, feed: Feed, sort: Sort? = feed.defaultSort) {
        services.navigator.push(FeedPage(services, feed, sort))
    }

    fun openSubreddit(services: Services, name: String, sort: PostSort = PostSort.Hot) =
        openFeed(services, Feed.fromName(name), sort.toSort())

    fun openUser(services: Services, username: String, sort: UserProfileSort = UserProfileSort.Overview) =
        openFeed(services, Feed.User(username), sort.toSort())

    fun openMessages(services: Services, sort: InboxSort = InboxSort.Unread) =
        openFeed(services, Feed.Messages, sort.toSort())

    /** Opens a thread, recording it in History unless it was opened from there. */
    fun openPost(services: Services, post: Post, focus: CommentFocus? = null, fromHistory: Boolean = false) {
        if (!fromHistory) services.history.add(post.name)
        services.navigator.push(PostPage(services, post, focus, fromHistory))
    }

    /**
     * The reply page for [parent] (a post or comment); [onSubmitted] gets the new comment.
     * [context] is the thread above [parent], root first, shown with it (the C# Parent chain).
     */
    fun openReply(services: Services, parent: RedditThing, context: List<RedditThing> = emptyList(), onSubmitted: (Comment) -> Unit) =
        services.navigator.push(com.mrjackspade.deaddit.ui.reply.ReplyPage(services, context + parent, null, onSubmitted))

    /**
     * The edit page for the user's own comment; [onSubmitted] gets it as edited. [context] is the
     * thread above it, root first: what's shown, and what AI Assist is given.
     */
    fun openEdit(services: Services, comment: Comment, context: List<RedditThing> = emptyList(), onSubmitted: (Comment) -> Unit) =
        services.navigator.push(com.mrjackspade.deaddit.ui.reply.ReplyPage(services, context, comment, onSubmitted))

    /** The editor for a new block rule; [onSaved] gets the rule as the user saved it. */
    fun openBlockRuleEditor(services: Services, rule: BlockRule, onSaved: (BlockRule) -> Unit) =
        com.mrjackspade.deaddit.ui.editor.EditorPage.open(services, com.mrjackspade.deaddit.ui.editor.Schemas.blockRule, rule, onSaved)

    /** The page for writing a private message to [user], optionally replying to [replyTo]. */
    fun openMessage(services: Services, user: User, replyTo: Message? = null) =
        services.navigator.push(com.mrjackspade.deaddit.ui.messages.MessagePage(services, user, replyTo))

    fun openSettings(services: Services) =
        services.navigator.push(com.mrjackspade.deaddit.ui.settings.SettingsPage(services))

    fun openHistory(services: Services) =
        services.navigator.push(com.mrjackspade.deaddit.ui.history.HistoryPage(services))

    fun openSubredditAbout(services: Services, subreddit: String) =
        services.navigator.push(com.mrjackspade.deaddit.ui.about.SubredditAboutPage(services, subreddit))

    /** The image viewer for one or more images. */
    fun openImages(services: Services, items: List<com.mrjackspade.deaddit.handlers.FileDownload>) =
        services.navigator.push(com.mrjackspade.deaddit.ui.media.ImageViewerPage(services, items))

    /** The video player. */
    fun openVideo(services: Services, item: com.mrjackspade.deaddit.handlers.FileDownload) =
        services.navigator.push(com.mrjackspade.deaddit.ui.media.VideoPlayerPage(services, item))

    /** The in-app browser. */
    fun openBrowser(services: Services, url: String) =
        services.navigator.push(com.mrjackspade.deaddit.ui.media.BrowserPage(services, url))

    /** Asks which of [items] to use; null when the user backs out. */
    suspend fun selectImages(services: Services, items: List<com.mrjackspade.deaddit.handlers.FileDownload>) =
        com.mrjackspade.deaddit.ui.media.ImageSelectPage.open(services, items)
}
