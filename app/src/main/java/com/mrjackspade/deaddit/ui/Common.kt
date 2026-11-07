package com.mrjackspade.deaddit.ui

import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.RowScope
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.drawscope.rotate
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.unit.dp

/** The bar across the top of a page, in the primary color. */
@Composable
fun TopBar(content: @Composable RowScope.() -> Unit) {
    val styling = LocalStyling.current
    Row(
        Modifier.fillMaxWidth().background(styling.primaryColor.color).statusBarsPadding(),
        verticalAlignment = Alignment.CenterVertically,
        content = content,
    )
}

/**
 * An icon button, for top bars and the landing page: every one the same icon set, size and
 * color. [description] is what screen readers announce.
 */
@Composable
fun TopBarButton(icon: ImageVector, description: String, color: Color = LocalStyling.current.textColor.color, onClick: () -> Unit) {
    Box(
        Modifier.clickable(role = Role.Button, onClick = onClick).padding(12.dp),
        contentAlignment = Alignment.Center,
    ) {
        Icon(icon, contentDescription = description, tint = color, modifier = Modifier.size(ICON_SIZE))
    }
}

/** The size of every icon button. */
val ICON_SIZE = 26.dp

/**
 * The loading indicator: the C# app's CSS spinner (site.css "spinner"), a 48px ring with a 5px
 * white border whose bottom segment is the highlight color, turning once a second, centered
 * across the width.
 */
@Composable
fun Spinner() {
    val highlight = LocalStyling.current.highlightColor.color
    val turn by rememberInfiniteTransition(label = "spinner").animateFloat(
        initialValue = 0f,
        targetValue = 360f,
        animationSpec = infiniteRepeatable(tween(1000, easing = LinearEasing), RepeatMode.Restart),
        label = "rotation",
    )

    Box(Modifier.fillMaxWidth(), contentAlignment = Alignment.Center) {
        Canvas(Modifier.size(48.dp)) {
            val border = 5.dp.toPx()
            // CSS draws the border inside the box (box-sizing: border-box): the stroke runs
            // along the middle of that band.
            val inset = border / 2
            val topLeft = Offset(inset, inset)
            val arcSize = Size(size.width - border, size.height - border)
            val stroke = Stroke(border)
            rotate(turn) {
                drawArc(Color.White, 0f, 360f, useCenter = false, topLeft = topLeft, size = arcSize, style = stroke)
                // A round box's bottom border runs between its diagonal corners: 45° to 135°.
                drawArc(highlight, 45f, 90f, useCenter = false, topLeft = topLeft, size = arcSize, style = stroke)
            }
        }
    }
}

/** Work in progress, shown as a [BusyOverlay] while any is running (the C# BusyOverlay's Show/Hide). */
class BusyCounter {
    var count by mutableIntStateOf(0)
        private set

    suspend fun <T> run(work: suspend () -> T): T {
        count++
        try {
            return work()
        } finally {
            count--
        }
    }
}

/**
 * The C# BusyOverlay: a translucent rounded block with a white spinner, centered over the page
 * while [busy] has work running. The block takes the taps that land on it; the page around it
 * stays usable, as the C# overlay was input-transparent outside its block.
 */
@Composable
fun BusyOverlay(busy: BusyCounter) {
    if (busy.count <= 0) return
    Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
        Box(
            Modifier.background(Color(0xAA000000), RoundedCornerShape(12.dp))
                .pointerInput(Unit) {
                    awaitPointerEventScope {
                        while (true) awaitPointerEvent().changes.forEach { it.consume() }
                    }
                }
                .padding(30.dp),
        ) {
            CircularProgressIndicator(color = Color.White)
        }
    }
}
