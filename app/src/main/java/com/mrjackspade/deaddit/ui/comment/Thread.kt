package com.mrjackspade.deaddit.ui.comment

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.runtime.Composable
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.CornerRadius
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Rect
import androidx.compose.ui.geometry.RoundRect
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.drawscope.clipRect
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import com.mrjackspade.deaddit.ui.LocalStyling
import com.mrjackspade.deaddit.ui.accentEdge
import com.mrjackspade.deaddit.ui.color

/**
 * A node of a comment tree: a comment with its replies ([CommentEntry]), or a row of the page's own
 * under one (the thread's "more" buttons). [key] is unique on the page.
 */
abstract class ThreadEntry(val key: String, val parent: CommentEntry?) {
    val depth: Int = if (parent == null) 0 else parent.depth + 1

    /** The padding above and below the row's content: a comment has its own; a page's own rows draw theirs. */
    open val padding: Dp get() = 0.dp
}

class CommentEntry(val model: CommentModel, parent: CommentEntry?) : ThreadEntry(model.comment.name, parent) {
    /** Its replies, in order. Changes redraw the tree. */
    val children = mutableStateListOf<ThreadEntry>()

    override val padding: Dp get() = COMMENT_PADDING
}

/**
 * One row of a tree drawn flat. [closing] is how many comments' boxes end with this row: this is
 * the last row under each, so each one's bottom padding comes after it.
 */
class ThreadRow(val entry: ThreadEntry, val closing: Int)

/** The tree in reading order. Reads the entries' children, so a composable reading it redraws as they change. */
fun flattenThread(roots: List<ThreadEntry>): List<ThreadRow> {
    val entries = ArrayList<ThreadEntry>()
    val closing = ArrayList<Int>()

    fun walk(list: List<ThreadEntry>) {
        for (entry in list) {
            entries += entry
            closing += 0
            if (entry is CommentEntry && entry.children.isNotEmpty()) {
                walk(entry.children)
                closing[closing.size - 1]++
            }
        }
    }

    walk(roots)
    return entries.indices.map { ThreadRow(entries[it], closing[it]) }
}

/** The comment entry showing [model] in the tree, or null. */
fun findEntry(roots: List<ThreadEntry>, model: CommentModel): CommentEntry? {
    for (entry in roots) {
        if (entry is CommentEntry) {
            if (entry.model === model) return entry
            findEntry(entry.children, model)?.let { return it }
        }
    }
    return null
}

/** Removes the comment entry showing [model] from the tree; false when it isn't there. */
fun removeEntry(roots: MutableList<ThreadEntry>, model: CommentModel): Boolean {
    if (roots.removeAll { it is CommentEntry && it.model === model }) return true
    return roots.any { it is CommentEntry && removeEntry(it.children, model) }
}

/**
 * A row of a comment tree. Each top-level comment and everything under it is one card, drawn a
 * row at a time: its first row has the card's top, its last row the card's bottom (and the gap to
 * the next card). Inside, each comment is padded above and below, its replies [LEVEL_STEP] further
 * in beside a 2dp line that runs down the replies only, stopping above the parent's bottom
 * padding. The line is the border color, the accent edge while its comment is selected, and the
 * subtext color once it has been deselected. [selected] is the selected row's key.
 */
@Composable
fun ThreadRowFrame(row: ThreadRow, selected: String?, content: @Composable () -> Unit) {
    val styling = LocalStyling.current
    val entry = row.entry
    val depth = entry.depth

    // The comments above this row, outermost first, and the color of each one's line.
    val ancestors = generateSequence(entry.parent) { it.parent }.toList().asReversed()
    val colors = ancestors.map { ancestor ->
        when {
            selected == ancestor.key -> styling.accentEdge
            ancestor.model.everSelected -> styling.subTextColor.color
            else -> styling.borderColor.color
        }
    }

    // The boxes ending at this row are the innermost [closing] ancestors; each one's line stops
    // above its own bottom padding and those of the boxes around it.
    val firstClosing = depth - row.closing
    val hasReplies = entry is CommentEntry && entry.children.isNotEmpty()
    val ownBottom = if (hasReplies) 0.dp else entry.padding
    val first = depth == 0
    val last = firstClosing == 0 && !hasReplies

    val card = styling.cardColor.color
    val edge = styling.borderColor.color
    val radius = styling.cornerRadius.dp

    Box(Modifier.fillMaxWidth().padding(start = CARD_MARGIN, end = CARD_MARGIN, top = if (first) CARD_GAP else 0.dp)) {
        Box(
            Modifier.fillMaxWidth().drawBehind {
                val r = radius.toPx()
                val stroke = 1.dp.toPx()
                // The card's outline, run past the row's top and bottom where the card goes on
                // above or below, and cut to the row: so only the first and last rows show its ends.
                val top = if (first) stroke / 2 else -r * 2
                val bottom = if (last) size.height - stroke / 2 else size.height + r * 2
                clipRect {
                    val outline = Path().apply {
                        addRoundRect(RoundRect(Rect(stroke / 2, top, size.width - stroke / 2, bottom), CornerRadius(r)))
                    }
                    drawPath(outline, card)
                    drawPath(outline, edge, style = Stroke(stroke))
                }

                val step = LEVEL_STEP.toPx()
                val padding = COMMENT_PADDING.toPx()
                val line = 2.dp.toPx()
                val inset = (CARD_INNER + LINE_OFFSET).toPx()
                colors.forEachIndexed { level, color ->
                    val short = if (level >= firstClosing) padding * (level - firstClosing + 1) else 0f
                    drawRect(color, topLeft = Offset(inset + step * level, 0f), size = Size(line, size.height - short))
                }
            },
        ) {
            Column(
                Modifier.fillMaxWidth().padding(
                    start = CARD_INNER + LEVEL_STEP * depth,
                    end = CARD_INNER,
                    top = entry.padding + if (first) CARD_EDGE else 0.dp,
                    bottom = ownBottom + COMMENT_PADDING * row.closing + if (last) CARD_EDGE else 0.dp,
                ),
            ) {
                content()
            }
        }
    }
}

/** Above and below each comment. */
private val COMMENT_PADDING = 12.dp

/** How much further in each level of replies is. */
private val LEVEL_STEP = 14.dp

/** Between the screen's edges and the cards, and between cards. */
private val CARD_MARGIN = 8.dp
private val CARD_GAP = 10.dp

/** Inside a card: from its sides to the comments, and the extra room under its top and over its bottom. */
private val CARD_INNER = 12.dp
private val CARD_EDGE = 0.dp

/** Where a level's line sits in the step before its replies. */
private val LINE_OFFSET = 3.dp
