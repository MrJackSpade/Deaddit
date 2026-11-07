package com.mrjackspade.deaddit.ui.html

import androidx.compose.foundation.layout.LayoutScopeMarker
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.Immutable
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.CornerRadius
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.FirstBaseline
import androidx.compose.ui.layout.Layout
import androidx.compose.ui.layout.ParentDataModifier
import androidx.compose.ui.layout.Placeable
import androidx.compose.ui.unit.Constraints
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.TextUnit
import androidx.compose.ui.unit.dp
import com.mrjackspade.deaddit.ui.px

/** The size of text the C# app gave no size: the WebView's default, CSS "medium". */
val CssMediumFontSize: TextUnit = 16.0.px

/** Roboto's x-height, in em: where CSS "vertical-align: middle" centers a box. */
private const val X_HEIGHT_EM = 0.528f

@LayoutScopeMarker
@Immutable
interface InlineFlowScope {
    /** CSS "vertical-align: middle": centered half an x-height above the line's baseline. */
    fun Modifier.alignMiddle(): Modifier = this.then(MiddleAligned)
}

private object InlineFlowScopeInstance : InlineFlowScope

private object MiddleAligned : ParentDataModifier {
    override fun Density.modifyParentData(parentData: Any?): Any = MiddleAligned
}

/**
 * A line of inline boxes as a browser lays them out in a block: left to right, wrapping when a
 * box doesn't fit, each sitting on the line's baseline (or its middle, [InlineFlowScope.alignMiddle]).
 * Every line has the "strut" of the block's own font ([strutSize]), so no line is shorter than a
 * line of that text, however small the boxes on it.
 */
@Composable
fun InlineFlow(modifier: Modifier = Modifier, strutSize: TextUnit = CssMediumFontSize, content: @Composable InlineFlowScope.() -> Unit) {
    Layout(
        contents = listOf({ Text("", fontSize = strutSize) }, { InlineFlowScopeInstance.content() }),
        modifier = modifier,
    ) { (strutMeasurables, measurables), constraints ->
        val strut = strutMeasurables.single().measure(Constraints())
        val strutAscent = strut.baseline()
        val strutDescent = strut.height - strutAscent
        val halfXHeight = strutSize.toPx() * X_HEIGHT_EM / 2

        val itemConstraints = Constraints(maxWidth = constraints.maxWidth)
        val items = measurables.map { it.measure(itemConstraints) to (it.parentData === MiddleAligned) }

        // Above and below the baseline, for each box.
        fun ascent(item: Pair<Placeable, Boolean>): Float =
            if (item.second) halfXHeight + item.first.height / 2f else item.first.baseline().toFloat()

        fun descent(item: Pair<Placeable, Boolean>): Float = item.first.height - ascent(item)

        val lines = mutableListOf<List<Pair<Placeable, Boolean>>>()
        var line = mutableListOf<Pair<Placeable, Boolean>>()
        var x = 0
        for (item in items) {
            if (line.isNotEmpty() && x + item.first.width > constraints.maxWidth) {
                lines += line
                line = mutableListOf()
                x = 0
            }
            line += item
            x += item.first.width
        }
        lines += line

        val metrics = lines.map { boxes ->
            val above = maxOf(strutAscent.toFloat(), boxes.maxOfOrNull(::ascent) ?: 0f)
            val below = maxOf(strutDescent.toFloat(), boxes.maxOfOrNull(::descent) ?: 0f)
            above to below
        }

        val width = lines.maxOf { boxes -> boxes.sumOf { it.first.width } }.coerceIn(constraints.minWidth, constraints.maxWidth)
        val height = metrics.sumOf { (above, below) -> kotlin.math.ceil(above + below).toInt() }.coerceIn(constraints.minHeight, constraints.maxHeight)

        layout(width, height) {
            var top = 0
            lines.forEachIndexed { i, boxes ->
                val (above, below) = metrics[i]
                var left = 0
                for (item in boxes) {
                    item.first.placeRelative(left, (top + above - ascent(item)).toInt())
                    left += item.first.width
                }
                top += kotlin.math.ceil(above + below).toInt()
            }
        }
    }
}

/** The first baseline, or the bottom for a box with no text (an image sits on the baseline). */
private fun Placeable.baseline(): Int = this[FirstBaseline].takeIf { it != androidx.compose.ui.layout.AlignmentLine.Unspecified } ?: height

/**
 * A CSS inline box's background (display: inline): its horizontal padding takes room on the line,
 * but its vertical padding is only painted, over the lines around it, adding nothing to the line's
 * height. [marginStart] and [marginEnd] are its horizontal margins, outside the background.
 */
fun Modifier.inlineBackground(color: Color, padding: Dp, radius: Dp, marginStart: Dp = 0.dp, marginEnd: Dp = 0.dp): Modifier =
    this.padding(start = marginStart, end = marginEnd)
        .drawBehind {
            val extra = padding.toPx()
            drawRoundRect(color, topLeft = Offset(0f, -extra), size = Size(size.width, size.height + extra * 2), cornerRadius = CornerRadius(radius.toPx()))
        }
        .padding(horizontal = padding)
