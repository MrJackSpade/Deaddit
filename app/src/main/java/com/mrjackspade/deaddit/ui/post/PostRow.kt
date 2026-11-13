package com.mrjackspade.deaddit.ui.post

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.layout.widthIn
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.ArrowDownward
import androidx.compose.material.icons.outlined.ArrowUpward
import androidx.compose.material.icons.outlined.Bookmark
import androidx.compose.material.icons.outlined.BookmarkBorder
import androidx.compose.material.icons.outlined.ChatBubbleOutline
import androidx.compose.material.icons.outlined.Check
import androidx.compose.material.icons.outlined.Link
import androidx.compose.material.icons.outlined.MoreHoriz
import androidx.compose.material.icons.outlined.Share
import androidx.compose.material.icons.outlined.VisibilityOff
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import coil3.compose.AsyncImage
import com.mrjackspade.deaddit.core.config.PostState
import com.mrjackspade.deaddit.core.html.HtmlText
import com.mrjackspade.deaddit.core.model.Distinguished
import com.mrjackspade.deaddit.core.model.Post
import com.mrjackspade.deaddit.core.model.VoteState
import com.mrjackspade.deaddit.core.model.link
import com.mrjackspade.deaddit.core.model.thumbnailUrl
import com.mrjackspade.deaddit.core.util.CompactCount
import com.mrjackspade.deaddit.core.util.UrlHelper
import com.mrjackspade.deaddit.core.util.elapsed
import com.mrjackspade.deaddit.ui.Card
import com.mrjackspade.deaddit.ui.Divider
import com.mrjackspade.deaddit.ui.IconAction
import com.mrjackspade.deaddit.ui.LocalStyling
import com.mrjackspade.deaddit.ui.color
import com.mrjackspade.deaddit.ui.flair.Flair
import com.mrjackspade.deaddit.ui.innerShape
import com.mrjackspade.deaddit.ui.px

/**
 * A post as shown, with what the user can change on it. Made where the post is loaded, so a post
 * that can't be shown (a malformed link) fails that load, whose error is shown, as the C# app's
 * row did when it was built; drawing it can't throw.
 */
class PostModel(val post: Post, val state: PostState) {
    /** The thumbnail: Reddit's, or the post's own link when that's an image. */
    val thumbnail: String? = thumbnailFor(post)

    var likes by mutableStateOf(post.likes)
    var score by mutableIntStateOf(post.score)
    var saved by mutableStateOf(post.saved)
    var visited by mutableStateOf(false)

    /** Applies a tap on ▲ or ▼, as the C# vote container did, and returns the new state to send. */
    fun toggle(direction: VoteState): VoteState {
        val target = if (likes == direction) VoteState.None else direction
        score += target.direction - likes.direction
        likes = target
        return target
    }
}

/** What a post row does when tapped; the page decides. */
class PostActions(
    val onSelect: () -> Unit,
    val onThumbnail: () -> Unit,
    val onVote: (VoteState) -> Unit,
    val onShare: () -> Unit,
    val onSave: () -> Unit,
    val onHide: () -> Unit,
    val onMore: () -> Unit,
    val onComments: () -> Unit,
)

/**
 * One post. In a feed ([inList]) it's a card: thumbnail on the left, where it's from, title and
 * flair, then votes; when [selected], its actions, ending with the comment count. Read posts fade
 * back (the visited opacity) and lose the title's weight. On its thread it's the page's heading.
 */
@Composable
fun PostRow(
    model: PostModel,
    selected: Boolean,
    inList: Boolean,
    hideScore: Boolean,
    actions: PostActions,
) {
    val styling = LocalStyling.current
    val blocked = model.state == PostState.Block
    val read = inList && (model.visited || model.state == PostState.Visited)

    if (!inList) {
        Column(Modifier.fillMaxWidth().background(if (blocked) styling.blockedBackgroundColor.color else Color.Transparent)) {
            Summary(model, inList = false, full = true, read = false, actions = actions)
            VoteRow(model, hideScore, actions, Modifier.padding(end = 10.dp))
        }
        return
    }

    Card(
        Modifier.padding(horizontal = 8.dp, vertical = 3.dp).alpha(if (read) styling.visitedOpacity.toFloat() else 1f),
        selected = selected,
        background = if (blocked) styling.blockedBackgroundColor.color else null,
    ) {
        Summary(model, inList = true, full = selected, read = read, actions = actions)
        VoteRow(model, hideScore, actions, Modifier.padding(end = 6.dp, bottom = 4.dp))

        if (selected) {
            Divider(Modifier.padding(horizontal = 8.dp))
            Row(Modifier.fillMaxWidth().padding(start = 4.dp, end = 6.dp, top = 4.dp, bottom = 4.dp), verticalAlignment = Alignment.CenterVertically) {
                IconAction(
                    if (model.saved) Icons.Outlined.Bookmark else Icons.Outlined.BookmarkBorder,
                    if (model.saved) "Unsave" else "Save",
                    active = model.saved,
                    tint = if (model.saved) styling.accentColor.color else styling.subTextColor.color,
                    onClick = actions.onSave,
                )
                IconAction(Icons.Outlined.Share, "Share", onClick = actions.onShare)
                IconAction(Icons.Outlined.VisibilityOff, "Hide", onClick = actions.onHide)
                IconAction(Icons.Outlined.MoreHoriz, "More", onClick = actions.onMore)
                Spacer(Modifier.weight(1f))
                CommentsButton(model.post.numComments, actions.onComments)
            }
        }
    }
}

/** The comment count, which opens the thread. */
@Composable
private fun CommentsButton(comments: Int, onClick: () -> Unit) {
    val styling = LocalStyling.current
    Row(
        Modifier.heightIn(min = 40.dp).clip(styling.innerShape)
            .clickable(role = Role.Button, onClick = onClick)
            .semantics { contentDescription = "$comments comments" }
            .padding(horizontal = 8.dp),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(6.dp),
    ) {
        Icon(Icons.Outlined.ChatBubbleOutline, contentDescription = null, tint = styling.subTextColor.color, modifier = Modifier.size(20.dp))
        Text(CompactCount.format(comments), color = styling.subTextColor.color, fontSize = styling.subTextFontSize.px)
    }
}

/**
 * The thumbnail (or, for a link without one, a link mark that opens it) beside where the post is
 * from and its title. [full] adds the author and lets that line wrap; otherwise it's one line,
 * ending in a feed with the comment count.
 */
@Composable
private fun Summary(model: PostModel, inList: Boolean, full: Boolean, read: Boolean, actions: PostActions) {
    val styling = LocalStyling.current
    val post = model.post
    val title = HtmlText.display(post.title)

    Row(
        Modifier.fillMaxWidth().clickable(onClick = actions.onSelect)
            .padding(start = if (inList) 12.dp else 16.dp, end = if (inList) 12.dp else 16.dp, top = 10.dp),
    ) {
        // A thread's own post is opened from its thumbnail only when it links somewhere.
        if (model.thumbnail != null || !post.isSelf) {
            Thumbnail(model.thumbnail, "Open $title", actions.onThumbnail)
            Spacer(Modifier.width(10.dp))
        }

        Column(Modifier.weight(1f), verticalArrangement = Arrangement.spacedBy(4.dp)) {
            val host = if (post.isSelf) null else UrlHelper.getDisplayHost(post.link)
            // A thread page has the subreddit in its top bar.
            val parts = listOfNotNull(
                "r/${post.subreddit}".takeIf { inList },
                post.author?.takeIf { full }?.let { "u/$it" },
                post.createdUtc.elapsed(),
                host,
            )
            Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(6.dp)) {
                Text(
                    parts.joinToString(" · "),
                    Modifier.weight(1f, fill = false),
                    color = styling.subTextColor.color,
                    fontSize = styling.subTextFontSize.px,
                    maxLines = if (full) Int.MAX_VALUE else 1,
                    overflow = TextOverflow.Ellipsis,
                )
                // A selected card has the count in its action bar instead.
                if (inList && !full) {
                    Row(
                        Modifier.semantics(mergeDescendants = true) { contentDescription = "${post.numComments} comments" },
                        verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.spacedBy(4.dp),
                    ) {
                        Text("·", color = styling.subTextColor.color, fontSize = styling.subTextFontSize.px)
                        Icon(Icons.Outlined.ChatBubbleOutline, contentDescription = null, tint = styling.subTextColor.color, modifier = Modifier.size(15.dp))
                        Text(CompactCount.format(post.numComments), color = styling.subTextColor.color, fontSize = styling.subTextFontSize.px, maxLines = 1)
                    }
                }
                if (read) {
                    Icon(Icons.Outlined.Check, contentDescription = "Read", tint = styling.subTextColor.color, modifier = Modifier.size(16.dp))
                }
            }

            Text(
                title,
                fontSize = (if (inList) styling.titleFontSize else styling.titleFontSize * 1.2).px,
                fontWeight = if (read) FontWeight.Normal else FontWeight.SemiBold,
                lineHeight = (if (inList) styling.titleFontSize else styling.titleFontSize * 1.2).px * 1.3f,
                color = when (post.distinguished) {
                    Distinguished.Moderator -> styling.moderatorTitleTextColor.color
                    Distinguished.Admin -> styling.adminTitleTextColor.color
                    else -> styling.textColor.color
                },
            )

            Flair(post.linkFlairText, post.linkFlairRichtext, post.linkFlairBackgroundColor, post.linkFlairTextColor)
        }
    }
}

@Composable
private fun Thumbnail(url: String?, description: String, onClick: () -> Unit) {
    val styling = LocalStyling.current
    val shape = styling.innerShape
    val size = styling.thumbnailSize.dp
    Box(
        Modifier.size(size).clip(shape).background(styling.tertiaryColor.color).border(1.dp, styling.borderColor.color, shape)
            .clickable(role = Role.Button, onClickLabel = description, onClick = onClick)
            .semantics { contentDescription = description },
        contentAlignment = Alignment.Center,
    ) {
        if (url != null) {
            AsyncImage(model = url, contentDescription = null, contentScale = ContentScale.Crop, modifier = Modifier.size(size))
        } else {
            Icon(Icons.Outlined.Link, contentDescription = null, tint = styling.subTextColor.color, modifier = Modifier.size(24.dp))
        }
    }
}

/**
 * At the right edge, mirroring the thumbnail on the left: up, the score and down. The vote's
 * color marks a vote cast.
 */
@Composable
private fun VoteRow(model: PostModel, hideScore: Boolean, actions: PostActions, modifier: Modifier = Modifier) {
    val styling = LocalStyling.current
    val up = model.likes == VoteState.Upvote
    val down = model.likes == VoteState.Downvote

    Row(modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
        Spacer(Modifier.weight(1f))

        IconAction(
            Icons.Outlined.ArrowUpward, "Upvote",
            tint = if (up) styling.upvoteColor.color else styling.subTextColor.color,
            active = up, activeColor = styling.upvoteColor.color,
        ) { actions.onVote(VoteState.Upvote) }

        if (!hideScore) {
            Text(
                CompactCount.format(model.score),
                Modifier.widthIn(min = 36.dp),
                color = when {
                    up -> styling.upvoteColor.color
                    down -> styling.downvoteColor.color
                    else -> styling.subTextColor.color
                },
                fontSize = styling.subTextFontSize.px,
                fontWeight = if (up || down) FontWeight.SemiBold else FontWeight.Normal,
                textAlign = TextAlign.Center,
            )
        }

        IconAction(
            Icons.Outlined.ArrowDownward, "Downvote",
            tint = if (down) styling.downvoteColor.color else styling.subTextColor.color,
            active = down, activeColor = styling.downvoteColor.color,
        ) { actions.onVote(VoteState.Downvote) }
    }
}

private fun thumbnailFor(post: Post): String? {
    val thumbnail = post.thumbnailUrl()
    if (!thumbnail.isNullOrBlank()) return thumbnail
    val link = post.link
    if (link.isNullOrBlank()) return null
    // Uri.TryCreate(..., UriKind.Absolute), as the C# RedditPostWebComponent did.
    if (!com.mrjackspade.deaddit.core.model.isAbsoluteUrl(link)) throw IllegalStateException("Post ${post.name} has a URL that isn't absolute: '$link'.")
    return if (UrlHelper.getMimeTypeFromUri(link).startsWith("image/")) link else null
}
