package com.mrjackspade.deaddit.ui.comment

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.RowScope
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import com.mrjackspade.deaddit.Services
import com.mrjackspade.deaddit.core.html.Block
import com.mrjackspade.deaddit.core.html.RedditHtml
import com.mrjackspade.deaddit.core.model.Comment
import com.mrjackspade.deaddit.core.model.Distinguished
import com.mrjackspade.deaddit.core.model.Post
import com.mrjackspade.deaddit.core.model.VoteState
import com.mrjackspade.deaddit.core.util.elapsed
import com.mrjackspade.deaddit.ui.LocalHacks
import com.mrjackspade.deaddit.ui.LocalStyling
import com.mrjackspade.deaddit.ui.color
import com.mrjackspade.deaddit.ui.flair.Flair
import com.mrjackspade.deaddit.ui.html.HtmlBody
import com.mrjackspade.deaddit.ui.px

/** A comment as shown, with what the user can change on it. */
class CommentModel(comment: Comment) {
    var comment by mutableStateOf(comment)
    var likes by mutableStateOf(comment.likes)
    var score by mutableIntStateOf(comment.score)
    var saved by mutableStateOf(comment.saved)
    var sendReplies by mutableStateOf(comment.sendReplies)
    var isNew by mutableStateOf(comment.new)

    /** The body, parsed once per edit. */
    var blocks: List<Block> by mutableStateOf(parse(comment))
        private set

    /** Replaces the body with an edited version. */
    fun edited(updated: Comment) {
        comment = comment.copy(body = updated.body, bodyHtml = updated.bodyHtml)
        blocks = parse(comment)
    }

    /** Applies a tap on ▲ or ▼, as the C# comment did, and returns the state to send. */
    fun toggle(direction: VoteState): VoteState {
        val target = if (likes == direction) VoteState.None else direction
        score += target.direction - likes.direction
        likes = target
        return target
    }

    private companion object {
        fun parse(comment: Comment): List<Block> = comment.bodyHtml?.let(RedditHtml::parse) ?: emptyList()
    }
}

/**
 * One comment: header (vote mark, author, score, age, tag or flair), body, and — when [isSelected] —
 * the action bar. [post] is the thread it's in, when known (for the OP mark).
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
    val comment = model.comment

    Column(
        modifier.fillMaxWidth()
            .background(if (isSelected) styling.highlightColor.color else Color.Transparent)
            .clickable(onClick = onSelect),
    ) {
        Header(services, model, post)

        HtmlBody(
            model.blocks,
            fontSize = styling.commentFontSize.px,
            onLink = actions.onLink,
            inline = inline,
        )

        if (isSelected) {
            Row(Modifier.fillMaxWidth().background(styling.highlightColor.color)) {
                BarButton("▲", "Upvote", if (model.likes == VoteState.Upvote) styling.upvoteColor.color else styling.textColor.color) { actions.onVote(VoteState.Upvote) }
                BarButton("▼", "Downvote", if (model.likes == VoteState.Downvote) styling.downvoteColor.color else styling.textColor.color) { actions.onVote(VoteState.Downvote) }
                BarButton("...", "More", styling.textColor.color, actions.onMore)
                BarButton("⢔", "Share", styling.textColor.color, actions.onShare)
                BarButton("↩", "Reply", styling.textColor.color, actions.onReply)
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

@Composable
private fun Header(services: Services, model: CommentModel, post: Post?) {
    val styling = LocalStyling.current
    val hacks = LocalHacks.current
    val comment = model.comment
    val sub = styling.subTextColor.color
    val subSize = styling.subTextFontSize.px

    val hideScore = comment.scoreHidden ||
        (hacks.hideSelfKarma && comment.author.equals(services.reddit.loggedInUser, ignoreCase = true))

    Column {
        Row(verticalAlignment = Alignment.CenterVertically) {
            when (model.likes) {
                VoteState.Upvote -> Text("▲", color = styling.upvoteColor.color, fontSize = subSize)
                VoteState.Downvote -> Text("▼", color = styling.downvoteColor.color, fontSize = subSize)
                VoteState.None -> Unit
            }

            AuthorName(comment.author ?: "", comment.distinguished, isOp = post != null && comment.author == post.author)

            if (!hideScore) {
                Text(model.score.toString(), color = sub, fontSize = subSize)
                val arrow = if (model.score > 0) "▲" else if (model.score < 0) "▼" else ""
                if (arrow.isNotEmpty()) Text(arrow, Modifier.padding(start = 1.dp), color = sub, fontSize = subSize * 0.5f)
            }

            Text(" ${comment.createdUtc.elapsed()}", color = sub, fontSize = subSize)
        }

        val tag = comment.author?.let(services.userTags::get)
        if (tag != null) {
            Text(
                tag,
                Modifier.padding(2.dp).background(styling.primaryColor.color, RoundedCornerShape(4.dp)).padding(4.dp),
                color = Color(0xFFFF4444),
                fontSize = subSize,
            )
        } else {
            Flair(comment.authorFlairText, comment.authorFlairRichtext, comment.authorFlairBackgroundColor, comment.authorFlairTextColor)
        }
    }
}

/** The author's name, marked as OP, moderator or admin. */
@Composable
fun AuthorName(author: String, distinguished: Distinguished?, isOp: Boolean) {
    val styling = LocalStyling.current

    val (background, text) = when (distinguished) {
        Distinguished.Moderator -> styling.moderatorAuthorBackgroundColor.color to styling.moderatorAuthorTextColor.color
        Distinguished.Admin -> styling.adminAuthorBackgroundColor.color to styling.adminAuthorTextColor.color
        else -> if (isOp) styling.opBackgroundColor.color to styling.opTextColor.color else Color.Transparent to styling.subTextColor.color
    }

    Text(
        author,
        Modifier.padding(end = 5.dp).background(background, RoundedCornerShape(3.dp)).padding(2.dp),
        color = text,
        fontSize = styling.subTextFontSize.px,
    )
}

@Composable
private fun RowScope.BarButton(text: String, description: String, color: Color, onClick: () -> Unit) {
    Text(
        text,
        Modifier.weight(1f).clickable(role = Role.Button, onClick = onClick).padding(10.dp)
            .semantics { contentDescription = description },
        color = color,
        fontSize = LocalStyling.current.titleFontSize.px,
        textAlign = TextAlign.Center,
    )
}
