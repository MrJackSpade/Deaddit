package com.mrjackspade.deaddit.ui.comment

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.runtime.Composable
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import com.mrjackspade.deaddit.ui.LocalStyling
import com.mrjackspade.deaddit.ui.color

/**
 * A node of a comment tree: a comment with its replies ([CommentEntry]), or a row of the page's own
 * under one (the thread's "more" buttons). [key] is unique on the page.
 */
abstract class ThreadEntry(val key: String, val parent: CommentEntry?) {
    val depth: Int = if (parent == null) 0 else parent.depth + 1

    /**
     * The padding around the row's content on the left, top and bottom: a comment's box has 5px
     * (RedditCommentWebComponent); a page's own rows draw their own.
     */
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
 * A row of a comment tree, laid out as the C# app's nested boxes: each comment a box padded 5px
 * left, top and bottom, its replies in a container 8px further in behind a 1px line. So a reply's
 * text is 14px further in than its parent's; a parent and its first reply are 5px apart, siblings
 * 10px; and each level's line runs down the replies only, stopping above the parent's bottom
 * padding. The line is the subtext color, the highlight color while its comment is selected, and
 * the text color once it has been deselected. [selected] is the selected row's key; [endPadding]
 * the container's right padding.
 */
@Composable
fun ThreadRowFrame(row: ThreadRow, selected: String?, endPadding: Dp, content: @Composable () -> Unit) {
    val styling = LocalStyling.current
    val entry = row.entry
    val depth = entry.depth

    // The comments above this row, outermost first, and the color of each one's line.
    val ancestors = generateSequence(entry.parent) { it.parent }.toList().asReversed()
    val colors = ancestors.map { ancestor ->
        when {
            selected == ancestor.key -> styling.highlightColor.color
            ancestor.model.everSelected -> styling.textColor.color
            else -> styling.subTextColor.color
        }
    }

    // The boxes ending at this row are the innermost [closing] ancestors; each one's line stops
    // above its own bottom padding and those of the boxes around it.
    val firstClosing = depth - row.closing
    val ownBottom = if (entry is CommentEntry && entry.children.isNotEmpty()) 0.dp else entry.padding

    Box(
        Modifier.fillMaxWidth().padding(end = endPadding).drawBehind {
            val step = LEVEL_STEP.toPx()
            val padding = COMMENT_PADDING.toPx()
            val line = 1.dp.toPx()
            colors.forEachIndexed { level, color ->
                val short = if (level >= firstClosing) padding * (level - firstClosing + 1) else 0f
                drawRect(color, topLeft = Offset(padding + step * level, 0f), size = Size(line, size.height - short))
            }
        },
    ) {
        Column(
            Modifier.fillMaxWidth().padding(
                start = LEVEL_STEP * depth + entry.padding,
                top = entry.padding,
                bottom = ownBottom + COMMENT_PADDING * row.closing,
            ),
        ) {
            content()
        }
    }
}

private val COMMENT_PADDING = 5.dp

/** How much further in each level of replies is: the line (1px) and the replies' padding (8px), and the comment's own padding (5px). */
private val LEVEL_STEP = 14.dp
