package com.mrjackspade.deaddit.ui.html

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.IntrinsicSize
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.mutableStateMapOf
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
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
import androidx.compose.ui.unit.TextUnit
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.em
import com.mrjackspade.deaddit.core.html.Align
import com.mrjackspade.deaddit.core.html.Block
import com.mrjackspade.deaddit.core.html.Span
import com.mrjackspade.deaddit.ui.LocalStyling
import com.mrjackspade.deaddit.ui.color

/**
 * Reddit's rendered markdown, drawn natively. [onLink] gets every link tapped. A paragraph that is
 * just one link is drawn by what [inline] returns for it, when it returns something (inline media).
 */
@Composable
fun HtmlBody(
    blocks: List<Block>,
    fontSize: TextUnit,
    onLink: (String) -> Unit,
    modifier: Modifier = Modifier,
    inline: (url: String) -> (@Composable () -> Unit)? = { null },
) {
    // Spoilers stay hidden until tapped, each on its own.
    val revealed = remember(blocks) { mutableStateMapOf<Int, Boolean>() }

    Column(modifier) {
        blocks.forEachIndexed { index, block ->
            BlockView(block, fontSize, onLink, revealed, inline, Modifier.padding(top = if (index == 0) 0.dp else 10.dp))
        }
    }
}

@Composable
private fun BlockView(
    block: Block,
    fontSize: TextUnit,
    onLink: (String) -> Unit,
    revealed: MutableMap<Int, Boolean>,
    inline: (url: String) -> (@Composable () -> Unit)?,
    modifier: Modifier = Modifier,
) {
    val styling = LocalStyling.current
    val text = styling.textColor.color

    when (block) {
        is Block.Paragraph -> {
            val replacement = block.text.singleOrNull()?.link?.let(inline)
            if (replacement != null) {
                Box(modifier) { replacement() }
            } else {
                Text(rich(block.text, onLink, revealed), modifier, color = text, fontSize = fontSize)
            }
        }

        is Block.Heading -> Text(
            rich(block.text, onLink, revealed),
            modifier,
            color = text,
            fontSize = fontSize * (when (block.level) { 1 -> 2.0f; 2 -> 1.5f; 3 -> 1.17f; else -> 1.0f }),
            fontWeight = FontWeight.Bold,
        )

        is Block.Quote -> Row(modifier.padding(start = 15.dp).height(IntrinsicSize.Min)) {
            // The left rule, in the text color, as in the C# app's stylesheet.
            Box(Modifier.width(1.dp).fillMaxHeight().background(text))
            Column(Modifier.padding(start = 5.dp)) {
                block.blocks.forEachIndexed { i, inner ->
                    BlockView(inner, fontSize, onLink, revealed, inline, Modifier.padding(top = if (i == 0) 0.dp else 10.dp))
                }
            }
        }

        is Block.BulletList -> Column(modifier.padding(start = 20.dp), verticalArrangement = Arrangement.spacedBy(2.dp)) {
            block.items.forEachIndexed { i, item ->
                Row {
                    Text(if (block.ordered) "${block.start + i}." else "•", color = text, fontSize = fontSize, modifier = Modifier.padding(end = 6.dp))
                    Column {
                        item.forEachIndexed { j, inner ->
                            BlockView(inner, fontSize, onLink, revealed, inline, Modifier.padding(top = if (j == 0) 0.dp else 10.dp))
                        }
                    }
                }
            }
        }

        is Block.Code -> Text(
            block.text,
            modifier.fillMaxWidth().horizontalScroll(rememberScrollState()).background(Color(0x22FFFFFF)).padding(6.dp),
            color = text,
            fontSize = fontSize,
            fontFamily = FontFamily.Monospace,
            softWrap = false,
        )

        is Block.Table -> Column(modifier.horizontalScroll(rememberScrollState())) {
            val all = listOfNotNull(block.header.takeIf { it.isNotEmpty() }) + block.rows
            all.forEachIndexed { rowIndex, row ->
                Row(Modifier.height(IntrinsicSize.Min)) {
                    row.forEach { cell ->
                        Text(
                            rich(cell.text, onLink, revealed),
                            Modifier.border(0.5.dp, styling.subTextColor.color).width(IntrinsicSize.Max).padding(4.dp),
                            color = text,
                            fontSize = fontSize,
                            fontWeight = if (rowIndex == 0 && block.header.isNotEmpty()) FontWeight.Bold else null,
                            textAlign = when (cell.align) { Align.Start -> TextAlign.Start; Align.Center -> TextAlign.Center; Align.End -> TextAlign.End },
                        )
                    }
                }
            }
        }

        Block.Rule -> Box(modifier.fillMaxWidth().height(1.dp).background(styling.subTextColor.color))
    }
}

@Composable
private fun rich(spans: List<Span>, onLink: (String) -> Unit, revealed: MutableMap<Int, Boolean>): AnnotatedString {
    val styling = LocalStyling.current
    val link = styling.hyperlinkColor.color
    val text = styling.textColor.color

    return buildAnnotatedString {
        for (span in spans) {
            val hidden = span.spoiler != null && revealed[span.spoiler] != true

            val style = SpanStyle(
                fontWeight = if (span.bold) FontWeight.Bold else null,
                fontStyle = if (span.italic) FontStyle.Italic else null,
                textDecoration = if (span.strike) TextDecoration.LineThrough else null,
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
                hidden -> withLink(LinkAnnotation.Clickable("spoiler-${span.spoiler}") { revealed[span.spoiler!!] = true }) {
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
