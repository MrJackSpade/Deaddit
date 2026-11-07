package com.mrjackspade.deaddit.ui.post

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import coil3.compose.AsyncImage
import com.mrjackspade.deaddit.core.config.PostState
import com.mrjackspade.deaddit.core.model.Distinguished
import com.mrjackspade.deaddit.core.model.Post
import com.mrjackspade.deaddit.core.model.VoteState
import com.mrjackspade.deaddit.core.model.link
import com.mrjackspade.deaddit.core.model.thumbnailUrl
import com.mrjackspade.deaddit.core.util.UrlHelper
import com.mrjackspade.deaddit.core.util.elapsed
import com.mrjackspade.deaddit.ui.LocalStyling
import com.mrjackspade.deaddit.ui.color
import com.mrjackspade.deaddit.ui.px
import com.mrjackspade.deaddit.ui.flair.Flair

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
 * One post: thumbnail, title with flair and metadata, and the vote column; when [selected], its
 * author and age and the action buttons. In a feed ([inList]), visited posts are dimmed.
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
    val post = model.post

    val background = when {
        model.state == PostState.Block -> styling.blockedBackgroundColor.color
        selected -> styling.highlightColor.color
        else -> styling.secondaryColor.color
    }

    val dimmed = inList && (model.visited || model.state == PostState.Visited)

    Column(Modifier.fillMaxWidth().background(background).alpha(if (dimmed) styling.visitedOpacity.toFloat() else 1f)) {
        Row(Modifier.fillMaxWidth()) {
            AsyncImage(
                model = model.thumbnail,
                contentDescription = "Open ${post.title}",
                contentScale = ContentScale.Crop,
                modifier = Modifier.size(styling.thumbnailSize.dp).clickable(role = Role.Button, onClick = actions.onThumbnail),
            )

            Column(
                Modifier.weight(1f).clickable(onClick = actions.onSelect).padding(start = 5.dp, end = 5.dp, bottom = 5.dp),
            ) {
                Text(
                    post.title,
                    fontSize = styling.titleFontSize.px,
                    color = when (post.distinguished) {
                        Distinguished.Moderator -> styling.moderatorTitleTextColor.color
                        Distinguished.Admin -> styling.adminTitleTextColor.color
                        else -> styling.textColor.color
                    },
                )

                Flair(post.linkFlairText, post.linkFlairRichtext, post.linkFlairBackgroundColor, post.linkFlairTextColor)

                val host = if (post.isSelf) null else UrlHelper.getDisplayHost(post.link)
                Text(
                    "${post.numComments} 🗨 ${post.subreddit}" + (host?.let { " ($it)" } ?: ""),
                    fontSize = styling.subTextFontSize.px,
                    color = styling.subTextColor.color,
                )

                if (selected || !inList) {
                    Text(
                        "${post.createdUtc.elapsed()} by ${post.author}",
                        fontSize = styling.subTextFontSize.px,
                        color = styling.subTextColor.color,
                    )
                }
            }

            VoteColumn(model, hideScore, actions.onVote)
        }

        if (selected) {
            com.mrjackspade.deaddit.ui.FlexRow(Modifier.fillMaxWidth().background(background)) {
                ActionButton("Share", actions.onShare)
                ActionButton(if (model.saved) "Unsave" else "Save", actions.onSave)
                ActionButton("Hide", actions.onHide)
                ActionButton("...", actions.onMore, "More")
                ActionButton("🗨", actions.onComments, "Comments")
            }
        }
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

@Composable
private fun VoteColumn(model: PostModel, hideScore: Boolean, onVote: (VoteState) -> Unit) {
    val styling = LocalStyling.current
    val up = model.likes == VoteState.Upvote
    val down = model.likes == VoteState.Downvote

    Column(Modifier.padding(10.dp), horizontalAlignment = Alignment.CenterHorizontally) {
        Text(
            "▲",
            Modifier.clickable(role = Role.Button) { onVote(VoteState.Upvote) }.semantics { contentDescription = "Upvote" },
            fontSize = styling.titleFontSize.px,
            color = if (up) styling.upvoteColor.color else styling.textColor.color,
            textAlign = TextAlign.Center,
        )
        if (!hideScore) {
            Text(
                model.score.toString(),
                fontSize = styling.titleFontSize.px,
                color = when {
                    up -> styling.upvoteColor.color
                    down -> styling.downvoteColor.color
                    else -> styling.textColor.color
                },
                textAlign = TextAlign.Center,
            )
        }
        Text(
            "▼",
            Modifier.clickable(role = Role.Button) { onVote(VoteState.Downvote) }.semantics { contentDescription = "Downvote" },
            fontSize = styling.titleFontSize.px,
            color = if (down) styling.downvoteColor.color else styling.textColor.color,
            textAlign = TextAlign.Center,
        )
    }
}

@Composable
private fun ActionButton(text: String, onClick: () -> Unit, description: String? = null) {
    val styling = LocalStyling.current
    Text(
        text,
        Modifier.clickable(role = Role.Button, onClick = onClick).padding(10.dp)
            .let { m -> if (description != null) m.semantics { contentDescription = description } else m },
        fontSize = styling.titleFontSize.px,
        color = styling.textColor.color,
        textAlign = TextAlign.Center,
    )
}
