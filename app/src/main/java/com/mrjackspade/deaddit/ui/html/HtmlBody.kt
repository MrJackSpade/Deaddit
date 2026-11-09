package com.mrjackspade.deaddit.ui.html

import androidx.compose.foundation.background
import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.layout.wrapContentWidth
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.selection.SelectionContainer
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.mutableStateMapOf
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.Layout
import androidx.compose.ui.text.AnnotatedString
import androidx.compose.ui.text.LinkAnnotation
import androidx.compose.ui.text.SpanStyle
import androidx.compose.ui.text.TextLinkStyles
import androidx.compose.ui.text.buildAnnotatedString
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontStyle
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.BaselineShift
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextDecoration
import androidx.compose.ui.text.withLink
import androidx.compose.ui.text.withStyle
import androidx.compose.ui.unit.Constraints
import androidx.compose.ui.unit.TextUnit
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.em
import com.mrjackspade.deaddit.core.html.Align
import com.mrjackspade.deaddit.core.html.Block
import com.mrjackspade.deaddit.core.html.Span
import com.mrjackspade.deaddit.ui.LocalStyling
import com.mrjackspade.deaddit.ui.color

/**
 * Reddit's rendered markdown, drawn natively as the C# app's WebView drew it: the browser's default
 * styles with the app's stylesheet (site.css) over them. [onLink] gets every link tapped. A paragraph
 * that is just one link is drawn by what [inline] returns for it, when it returns something (inline
 * media). [onSpoiler] is told when a hidden spoiler is tapped (and revealed): in the C# app that tap
 * also reached the comment around it, selecting it. Its text can be selected and copied with a
 * long press, as in the WebView.
 */
@Composable
fun HtmlBody(
    blocks: List<Block>,
    fontSize: TextUnit,
    onLink: (String) -> Unit,
    modifier: Modifier = Modifier,
    inline: (url: String) -> (@Composable () -> Unit)? = { null },
    onSpoiler: () -> Unit = {},
) {
    // Spoilers stay hidden until tapped, each on its own.
    val revealed = remember(blocks) { mutableStateMapOf<Int, Boolean>() }
    val body = Body(fontSize, onLink, revealed, inline, onSpoiler)

    SelectionContainer(modifier) {
        Column {
            if (blocks.isNotEmpty()) {
                // The first block's top margin and the last's bottom one collapse through the body
                // (no padding or border of its own) and show around it.
                val edges = Css.edges(blocks, body.em, listDepth = 0)
                Spacer(Modifier.height(edges.top.dp))
                body.Blocks(blocks, listDepth = 0)
                Spacer(Modifier.height(edges.bottom.dp))
            }
        }
    }
}

/** A block's vertical margins, in CSS px. */
private class Margins(val top: Float, val bottom: Float)

/**
 * The vertical margins of each block, from Chrome's default stylesheet and the C# app's site.css:
 * p and h1-h3 have none but 10px above when not first; blockquote 10px above when not first and
 * 1em below; lists and pre 1em around (none for a list inside a list); h4-h6 their own em margins;
 * hr 0.5em. Margins of adjacent blocks collapse to the larger, and a quote's or list item's first
 * and last children's collapse through it (they have no padding or border above or below).
 */
private object Css {
    fun own(block: Block, first: Boolean, em: Float, listDepth: Int): Margins = when (block) {
        is Block.Paragraph -> Margins(if (first) 0f else 10f, 0f)
        is Block.Heading -> when (block.level) {
            1, 2, 3 -> Margins(if (first) 0f else 10f, 0f)
            else -> headingMargin(block.level, em).let { Margins(it, it) }
        }
        is Block.Quote -> Margins(if (first) 0f else 10f, em)
        is Block.BulletList -> if (listDepth > 0) Margins(0f, 0f) else Margins(em, em)
        is Block.Code -> Margins(em, em)
        is Block.Table -> Margins(0f, 0f)
        Block.Rule -> Margins(em * 0.5f, em * 0.5f)
    }

    /** [own], collapsed with the margins of the block's first and last children. */
    fun collapsed(block: Block, first: Boolean, em: Float, listDepth: Int): Margins {
        val own = own(block, first, em, listDepth)
        val inner = when (block) {
            is Block.Quote -> edges(block.blocks, em, listDepth)
            is Block.BulletList -> if (block.items.isEmpty()) null else Margins(
                edges(block.items.first(), em, listDepth + 1).top,
                edges(block.items.last(), em, listDepth + 1).bottom,
            )
            else -> null
        } ?: return own
        return Margins(maxOf(own.top, inner.top), maxOf(own.bottom, inner.bottom))
    }

    /** The margins at the top of the first of [blocks] and the bottom of the last. */
    fun edges(blocks: List<Block>, em: Float, listDepth: Int): Margins =
        if (blocks.isEmpty()) Margins(0f, 0f) else Margins(
            collapsed(blocks.first(), true, em, listDepth).top,
            collapsed(blocks.last(), blocks.size == 1, em, listDepth).bottom,
        )

    /** The gap between two adjacent blocks: their margins collapse to the larger. */
    fun gap(previous: Block, previousFirst: Boolean, next: Block, em: Float, listDepth: Int): Float =
        maxOf(collapsed(previous, previousFirst, em, listDepth).bottom, collapsed(next, false, em, listDepth).top)

    /** h1-h6 font sizes, in em of the body text. */
    fun headingSize(level: Int): Float = when (level) {
        1 -> 2.0f
        2 -> 1.5f
        3 -> 1.17f
        4 -> 1.0f
        5 -> 0.83f
        else -> 0.67f
    }

    /** h4-h6 margins: 1.33em, 1.67em and 2.33em of their own size. */
    private fun headingMargin(level: Int, em: Float): Float = em * headingSize(level) * when (level) {
        4 -> 1.33f
        5 -> 1.67f
        else -> 2.33f
    }
}

private class Body(
    val fontSize: TextUnit,
    val onLink: (String) -> Unit,
    val revealed: MutableMap<Int, Boolean>,
    val inline: (url: String) -> (@Composable () -> Unit)?,
    val onSpoiler: () -> Unit,
) {
    /** 1em in CSS px (and dp, which they map to). */
    val em: Float = fontSize.value

    /** [blocks] with the gaps between them; the first's top and last's bottom margins are the caller's. */
    @Composable
    fun Blocks(blocks: List<Block>, listDepth: Int) {
        blocks.forEachIndexed { i, block ->
            if (i > 0) Spacer(Modifier.height(Css.gap(blocks[i - 1], i == 1, block, em, listDepth).dp))
            BlockView(block, listDepth)
        }
    }

    @Composable
    private fun BlockView(block: Block, listDepth: Int) {
        val styling = LocalStyling.current
        val text = styling.textColor.color

        when (block) {
            is Block.Paragraph -> {
                val replacement = block.text.singleOrNull()?.link?.let(inline)
                if (replacement != null) {
                    Box { replacement() }
                } else {
                    Text(rich(block.text), color = text, fontSize = fontSize)
                }
            }

            is Block.Heading -> Text(
                rich(block.text),
                color = text,
                fontSize = fontSize * Css.headingSize(block.level),
                fontWeight = FontWeight.Bold,
            )

            // margin-left 15px, a 1px left border in the text color, padding-left 5px; the
            // browser's 40px right margin.
            is Block.Quote -> Column(
                Modifier.padding(start = 15.dp, end = 40.dp)
                    .drawBehind { drawRect(text, size = Size(1.dp.toPx(), size.height)) }
                    .padding(start = 6.dp),
            ) {
                Blocks(block.blocks, listDepth)
            }

            is Block.BulletList -> ListView(block, listDepth)

            // The browser's pre: no background or padding; it scrolls sideways (site.css).
            is Block.Code -> Text(
                block.text,
                Modifier.fillMaxWidth().horizontalScroll(rememberScrollState()),
                color = text,
                fontSize = fontSize,
                fontFamily = FontFamily.Monospace,
                softWrap = false,
            )

            is Block.Table -> TableView(block)

            // The browser's hr: a 1px inset border all round an empty box, its top in a darker
            // gray than its bottom.
            Block.Rule -> Box(
                Modifier.fillMaxWidth().height(2.dp).drawBehind {
                    val line = 1.dp.toPx()
                    drawRect(RULE_DARK, size = Size(size.width, line))
                    drawRect(RULE_LIGHT, topLeft = Offset(0f, size.height - line), size = Size(size.width, line))
                },
            )
        }
    }

    /**
     * A list, its markers hanging in its left padding (20px for ul, from site.css; the browser's
     * 40px for ol), each ending a space short of the item's text. No gaps between items but their
     * own blocks' margins. Nested lists mark with circles, then squares.
     */
    @Composable
    private fun ListView(block: Block.BulletList, listDepth: Int) {
        val text = LocalStyling.current.textColor.color
        val indent = if (block.ordered) 40.dp else 20.dp
        val bullet = when (listDepth) {
            0 -> "•"
            1 -> "◦"
            else -> "▪"
        }

        Column {
            block.items.forEachIndexed { i, item ->
                if (i > 0) {
                    val gap = maxOf(Css.edges(block.items[i - 1], em, listDepth + 1).bottom, Css.edges(item, em, listDepth + 1).top)
                    Spacer(Modifier.height(gap.dp))
                }
                Row {
                    Box(Modifier.width(indent).alignByBaseline()) {
                        Text(
                            if (block.ordered) "${block.start + i}." else bullet,
                            Modifier.align(Alignment.CenterEnd).wrapContentWidth(Alignment.End, unbounded = true).padding(end = (em * SPACE_EM).dp),
                            color = text,
                            fontSize = fontSize,
                            softWrap = false,
                        )
                    }
                    Column(Modifier.weight(1f).alignByBaseline()) {
                        Blocks(item, listDepth + 1)
                    }
                }
            }
        }
    }

    /**
     * A table as the browser lays out one with no styles: columns as wide as their widest cell,
     * 2px between cells and around them, 1px of padding in each, header cells bold and centered,
     * cells centered vertically in their row. Too wide for the body, it scrolls sideways.
     */
    @Composable
    private fun TableView(block: Block.Table) {
        val text = LocalStyling.current.textColor.color
        val rows = listOfNotNull(block.header.takeIf { it.isNotEmpty() }) + block.rows
        val headerRows = if (block.header.isNotEmpty()) 1 else 0
        val columns = rows.maxOfOrNull { it.size } ?: 0

        Box(Modifier.horizontalScroll(rememberScrollState())) {
            Layout(
                content = {
                    rows.forEachIndexed { r, row ->
                        row.forEach { cell ->
                            val header = r < headerRows
                            Text(
                                rich(cell.text),
                                Modifier.padding(1.dp),
                                color = text,
                                fontSize = fontSize,
                                fontWeight = if (header) FontWeight.Bold else null,
                                textAlign = when (cell.align) {
                                    Align.Start -> TextAlign.Start
                                    Align.Center -> TextAlign.Center
                                    Align.End -> TextAlign.End
                                    Align.Unspecified -> if (header) TextAlign.Center else TextAlign.Start
                                },
                                softWrap = false,
                            )
                        }
                    }
                },
            ) { measurables, _ ->
                val spacing = 2.dp.roundToPx()
                var index = 0
                val cells = rows.map { row -> row.map { measurables[index++] } }

                val widths = IntArray(columns)
                for (row in cells) row.forEachIndexed { c, m -> widths[c] = maxOf(widths[c], m.maxIntrinsicWidth(Constraints.Infinity)) }

                val placed = cells.map { row -> row.mapIndexed { c, m -> m.measure(Constraints.fixedWidth(widths[c])) } }
                val heights = placed.map { row -> row.maxOfOrNull { it.height } ?: 0 }

                val width = widths.sum() + spacing * (columns + 1)
                val height = heights.sum() + spacing * (rows.size + 1)

                layout(width, height) {
                    var y = spacing
                    placed.forEachIndexed { r, row ->
                        var x = spacing
                        row.forEachIndexed { c, p ->
                            p.placeRelative(x, y + (heights[r] - p.height) / 2)
                            x += widths[c] + spacing
                        }
                        y += heights[r] + spacing
                    }
                }
            }
        }
    }

    @Composable
    private fun rich(spans: List<Span>): AnnotatedString {
        val styling = LocalStyling.current
        val link = styling.hyperlinkColor.color
        val text = styling.textColor.color

        return buildAnnotatedString {
            for (span in spans) {
                val hidden = span.spoiler != null && revealed[span.spoiler] != true

                val decorations = listOfNotNull(
                    if (span.strike) TextDecoration.LineThrough else null,
                    // Links are underlined, as the browser draws them.
                    if (span.link != null && !hidden) TextDecoration.Underline else null,
                )

                val style = SpanStyle(
                    fontWeight = if (span.bold) FontWeight.Bold else null,
                    fontStyle = if (span.italic) FontStyle.Italic else null,
                    textDecoration = if (decorations.isEmpty()) null else TextDecoration.combine(decorations),
                    fontFamily = if (span.code) FontFamily.Monospace else null,
                    baselineShift = if (span.superscript > 0) BaselineShift.Superscript else null,
                    fontSize = if (span.superscript > 0) 0.8.em else TextUnit.Unspecified,
                    color = when {
                        hidden -> text
                        span.link != null -> link
                        else -> Color.Unspecified
                    },
                    background = if (hidden) text else Color.Unspecified,
                )

                when {
                    hidden -> withLink(
                        LinkAnnotation.Clickable("spoiler-${span.spoiler}") {
                            revealed[span.spoiler!!] = true
                            onSpoiler()
                        },
                    ) {
                        withStyle(style) { append(span.text) }
                    }

                    span.link != null -> {
                        val target: String = span.link!!
                        withLink(LinkAnnotation.Clickable(target, TextLinkStyles(style)) { onLink(target) }) { append(span.text) }
                    }

                    else -> withStyle(style) { append(span.text) }
                }
            }
        }
    }

    private companion object {
        /** A space's width in em: the gap a list marker leaves before its item ("• "). */
        const val SPACE_EM = 0.278f

        /** Chrome's inset border on the hr's gray: darkened above, as is below. */
        val RULE_DARK = Color(0xFF2C2C2C)
        val RULE_LIGHT = Color(0xFF808080)
    }
}
