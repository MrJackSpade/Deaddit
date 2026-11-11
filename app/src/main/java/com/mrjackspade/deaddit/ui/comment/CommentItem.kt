package com.mrjackspade.deaddit.ui.comment

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.outlined.Reply
import androidx.compose.material.icons.outlined.ArrowDownward
import androidx.compose.material.icons.outlined.ArrowUpward
import androidx.compose.material.icons.outlined.MoreHoriz
import androidx.compose.material.icons.outlined.Share
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import com.mrjackspade.deaddit.Services
import com.mrjackspade.deaddit.core.html.Block
import com.mrjackspade.deaddit.core.html.ProcessingImages
import com.mrjackspade.deaddit.core.html.RedditHtml
import com.mrjackspade.deaddit.core.model.Comment
import com.mrjackspade.deaddit.core.model.Distinguished
import com.mrjackspade.deaddit.core.model.Post
import com.mrjackspade.deaddit.core.model.VoteState
import com.mrjackspade.deaddit.core.util.CompactCount
import com.mrjackspade.deaddit.core.util.elapsed
import com.mrjackspade.deaddit.ui.Chip
import com.mrjackspade.deaddit.ui.IconAction
import com.mrjackspade.deaddit.ui.LocalHacks
import com.mrjackspade.deaddit.ui.LocalStyling
import com.mrjackspade.deaddit.ui.accentEdge
import com.mrjackspade.deaddit.ui.accentFill
import com.mrjackspade.deaddit.ui.color
import com.mrjackspade.deaddit.ui.flair.Flair
import com.mrjackspade.deaddit.ui.html.HtmlBody
import com.mrjackspade.deaddit.ui.innerShape
import com.mrjackspade.deaddit.ui.px

/** A comment as shown, with what the user can change on it. */
class CommentModel(comment: Comment) {
    var comment by mutableStateOf(comment)
    var likes by mutableStateOf(comment.likes)
    var score by mutableIntStateOf(comment.score)
    var saved by mutableStateOf(comment.saved)
    var sendReplies by mutableStateOf(comment.sendReplies)
    var isNew by mutableStateOf(comment.new)

    /** Whether it has been selected: the line beside its replies stays the text color once deselected. */
    var everSelected by mutableStateOf(false)

    /** For a comment www translated: whether it's showing as written rather than translated. */
    var showingOriginal by mutableStateOf(false)
        private set

    /** The body, parsed once per edit (or switch between translation and original). */
    var blocks: List<Block> by mutableStateOf(parse(comment))
        private set

    /** The markdown of the body as shown. */
    val body: String get() = comment.original?.takeIf { showingOriginal }?.body ?: comment.body

    /** Replaces the body with an edited version. */
    fun edited(updated: Comment) {
        comment = comment.copy(body = updated.body, bodyHtml = updated.bodyHtml, translated = false, original = null)
        showingOriginal = false
        blocks = parse(comment)
    }

    /** Switches a translated comment between www's translation and the comment as written. */
    fun toggleTranslation() {
        val original = checkNotNull(comment.original) { "${comment.name} isn't translated." }
        showingOriginal = !showingOriginal
        blocks = parse(if (showingOriginal) comment.copy(body = original.body, bodyHtml = original.bodyHtml) else comment)
    }

    /** Applies a tap on ▲ or ▼, as the C# comment did, and returns the state to send. */
    fun toggle(direction: VoteState): VoteState {
        val target = if (likes == direction) VoteState.None else direction
        score += target.direction - likes.direction
        likes = target
        return target
    }

    private companion object {
        /** The body, with stale "Processing img" placeholders shown as their images (see ProcessingImages). */
        fun parse(comment: Comment): List<Block> =
            comment.bodyHtml?.let { RedditHtml.parse(ProcessingImages.resolve(it, comment.mediaMetadata)) } ?: emptyList()
    }
}

/**
 * One comment: header (author and their marks, age, score), tag or flair, body, and — when
 * [isSelected] — its actions, on the highlight with an accent edge. [post] is the thread it's in,
 * when known (for the OP mark). Tapping it anywhere but a link selects it, a hidden spoiler
 * included (the C# tap reached the comment too).
 */
@Composable
fun CommentView(
    services: Services,
    model: CommentModel,
    post: Post?,
    isSelected: Boolean,
    onSelect: () -> Unit,
    actions: CommentActions,
    inline: (String) -> (@Composable () -> Unit)? = { null },
    modifier: Modifier = Modifier,
) {
    val styling = LocalStyling.current
    val shape = styling.innerShape

    Column(
        modifier.fillMaxWidth()
            .let { m ->
                if (isSelected) m.clip(shape).background(styling.highlightColor.color).border(1.dp, styling.accentEdge, shape)
                else m
            }
            .clickable(onClick = onSelect)
            .padding(if (isSelected) 8.dp else 0.dp),
        verticalArrangement = Arrangement.spacedBy(4.dp),
    ) {
        Header(services, model, post)

        HtmlBody(
            model.blocks,
            fontSize = styling.commentFontSize.px,
            onLink = actions.onLink,
            inline = inline,
            onSpoiler = onSelect,
        )

        if (isSelected) {
            val up = model.likes == VoteState.Upvote
            val down = model.likes == VoteState.Downvote
            Row(Modifier.fillMaxWidth().padding(top = 2.dp), verticalAlignment = Alignment.CenterVertically) {
                IconAction(
                    Icons.Outlined.ArrowUpward, "Upvote",
                    tint = if (up) styling.upvoteColor.color else styling.subTextColor.color,
                    active = up, activeColor = styling.upvoteColor.color,
                ) { actions.onVote(VoteState.Upvote) }
                IconAction(
                    Icons.Outlined.ArrowDownward, "Downvote",
                    tint = if (down) styling.downvoteColor.color else styling.subTextColor.color,
                    active = down, activeColor = styling.downvoteColor.color,
                ) { actions.onVote(VoteState.Downvote) }
                Spacer(Modifier.weight(1f))
                IconAction(Icons.AutoMirrored.Outlined.Reply, "Reply", onClick = actions.onReply)
                IconAction(Icons.Outlined.Share, "Share", onClick = actions.onShare)
                IconAction(Icons.Outlined.MoreHoriz, "More", onClick = actions.onMore)
            }
        }
    }
}

/** What a comment's buttons and links do; the page decides. */
class CommentActions(
    val onVote: (VoteState) -> Unit,
    val onMore: () -> Unit,
    val onShare: () -> Unit,
    val onReply: () -> Unit,
    val onLink: (String) -> Unit,
)

/**
 * The author with their marks, the age, the score in the vote's color when one is cast (and, for a
 * comment www translated, the switch between translation and original); under it, the user's tag
 * for the author or their flair.
 */
@Composable
private fun Header(services: Services, model: CommentModel, post: Post?) {
    val styling = LocalStyling.current
    val hacks = LocalHacks.current
    val comment = model.comment
    val sub = styling.subTextColor.color
    val subSize = styling.subTextFontSize.px

    // Shown only when Reddit says the score isn't hidden (C#: !ScoreHidden == true).
    val hideScore = comment.scoreHidden != false ||
        (hacks.hideSelfKarma && comment.author.equals(services.reddit.loggedInUser, ignoreCase = true))

    Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
        // The score follows the age, so it stays with the comment at any depth.
        Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(6.dp)) {
            AuthorName(comment.author ?: "", comment.distinguished, isOp = post != null && comment.author == post.author, modifier = Modifier.weight(1f, fill = false))
            Text("·", color = sub, fontSize = subSize)
            Text(comment.createdUtc.elapsed() ?: "", color = sub, fontSize = subSize, maxLines = 1)
            if (!hideScore) {
                val color = when (model.likes) {
                    VoteState.Upvote -> styling.upvoteColor.color
                    VoteState.Downvote -> styling.downvoteColor.color
                    VoteState.None -> sub
                }
                Text("·", color = sub, fontSize = subSize)
                Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(3.dp)) {
                    Icon(
                        if (model.likes == VoteState.Downvote) Icons.Outlined.ArrowDownward else Icons.Outlined.ArrowUpward,
                        contentDescription = when (model.likes) {
                            VoteState.Upvote -> "Score, upvoted"
                            VoteState.Downvote -> "Score, downvoted"
                            VoteState.None -> "Score"
                        },
                        tint = color,
                        modifier = Modifier.size(15.dp),
                    )
                    Text(CompactCount.format(model.score), color = color, fontSize = subSize, maxLines = 1)
                }
            }
            if (comment.original != null) {
                Text(
                    if (model.showingOriginal) "original" else "translated",
                    Modifier.clickable(role = Role.Button) { model.toggleTranslation() }
                        .semantics { contentDescription = if (model.showingOriginal) "Show translation" else "Show original" },
                    color = sub,
                    fontSize = subSize,
                    maxLines = 1,
                )
            }
        }

        val tag = comment.author?.let(services.userTags::get)
        if (tag != null) {
            Chip(tag, color = styling.accentColor.color, background = styling.accentFill)
        } else {
            Flair(comment.authorFlairText, comment.authorFlairRichtext, comment.authorFlairBackgroundColor, comment.authorFlairTextColor)
        }
    }
}

/** The author's name, followed by a small OP, MOD or ADMIN chip in that mark's colors. */
@Composable
fun AuthorName(author: String, distinguished: Distinguished?, isOp: Boolean, modifier: Modifier = Modifier) {
    val styling = LocalStyling.current

    val mark = when (distinguished) {
        Distinguished.Moderator -> Triple("MOD", styling.moderatorAuthorTextColor.color, styling.moderatorAuthorBackgroundColor.color)
        Distinguished.Admin -> Triple("ADMIN", styling.adminAuthorTextColor.color, styling.adminAuthorBackgroundColor.color)
        else -> if (isOp) Triple("OP", styling.opTextColor.color, styling.opBackgroundColor.color) else null
    }

    Row(modifier, verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(6.dp)) {
        Text(
            author,
            Modifier.weight(1f, fill = false),
            color = styling.textColor.color,
            fontSize = styling.subTextFontSize.px,
            fontWeight = FontWeight.SemiBold,
            maxLines = 1,
            overflow = TextOverflow.Ellipsis,
        )
        if (mark != null) Chip(mark.first, color = mark.second, background = mark.third)
    }
}
