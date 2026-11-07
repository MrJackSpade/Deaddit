package com.mrjackspade.deaddit.ui

import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.layout.Layout
import androidx.compose.ui.unit.Constraints

/**
 * A row laid out as the C# app's CSS "display: flex" rows with "flex-grow: 1" children: each child
 * starts at its content's own width and the room left over is shared equally, so a longer label
 * ("Unsave") gets more of the row than a short one ("..."). Short of room, children shrink in
 * proportion to their width, as CSS's default flex-shrink does. Children are as tall as the tallest.
 */
@Composable
fun FlexRow(modifier: Modifier = Modifier, content: @Composable () -> Unit) {
    Layout(content, modifier) { measurables, constraints ->
        val natural = measurables.map { it.maxIntrinsicWidth(Constraints.Infinity) }
        val total = natural.sum()
        val available = constraints.maxWidth

        val widths = if (total <= available) {
            val extra = available - total
            natural.mapIndexed { i, w -> w + extra / natural.size + if (i < extra % natural.size) 1 else 0 }
        } else {
            natural.map { w -> w * available / total }
        }

        val height = measurables.zip(widths).maxOf { (m, w) -> m.maxIntrinsicHeight(w) }
        val placeables = measurables.zip(widths).map { (m, w) -> m.measure(Constraints.fixed(w, height)) }

        layout(available, height) {
            var x = 0
            for (p in placeables) {
                p.placeRelative(x, 0)
                x += p.width
            }
        }
    }
}
