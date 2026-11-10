package com.mrjackspade.deaddit.ui

import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.RowScope
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.graphics.drawscope.rotate
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp

/** The bar across the top of a page, in the primary color, under the status bar. */
@Composable
fun TopBar(content: @Composable RowScope.() -> Unit) {
    val styling = LocalStyling.current
    Row(
        Modifier.fillMaxWidth().background(styling.primaryColor.color).statusBarsPadding()
            .heightIn(min = 56.dp).padding(horizontal = 4.dp),
        verticalAlignment = Alignment.CenterVertically,
        content = content,
    )
}

/** A page's title in its top bar, taking the room the buttons leave. */
@Composable
fun RowScope.TopBarTitle(text: String) {
    Text(
        text,
        Modifier.weight(1f).padding(horizontal = 8.dp),
        color = LocalStyling.current.textColor.color,
        fontSize = 20.sp,
        fontWeight = FontWeight.SemiBold,
        maxLines = 1,
        overflow = TextOverflow.Ellipsis,
    )
}

/**
 * An icon button for top bars: every one the same icon set, size and color. [description] is
 * what screen readers announce; [active] marks a toggle that's on, in the accent.
 */
@Composable
fun TopBarButton(
    icon: ImageVector,
    description: String,
    color: Color = LocalStyling.current.subTextColor.color,
    active: Boolean = false,
    onClick: () -> Unit,
) {
    IconAction(icon, description, tint = if (active) LocalStyling.current.accentColor.color else color, active = active, size = 44.dp, onClick = onClick)
}

/** The size of the icons in icon buttons. */
val ICON_SIZE = 22.dp

/** The loading indicator: a ring in the border color with an accent arc, turning once a second, centered across the width. */
@Composable
fun Spinner() {
    val styling = LocalStyling.current
    val ring = styling.borderColor.color
    val arc = styling.accentColor.color
    val turn by rememberInfiniteTransition(label = "spinner").animateFloat(
        initialValue = 0f,
        targetValue = 360f,
        animationSpec = infiniteRepeatable(tween(1000, easing = LinearEasing), RepeatMode.Restart),
        label = "rotation",
    )

    Box(Modifier.fillMaxWidth().padding(vertical = 16.dp), contentAlignment = Alignment.Center) {
        Canvas(Modifier.size(32.dp)) {
            val border = 4.dp.toPx()
            val inset = border / 2
            val topLeft = Offset(inset, inset)
            val arcSize = Size(size.width - border, size.height - border)
            rotate(turn) {
                drawArc(ring, 0f, 360f, useCenter = false, topLeft = topLeft, size = arcSize, style = Stroke(border))
                drawArc(arc, 45f, 90f, useCenter = false, topLeft = topLeft, size = arcSize, style = Stroke(border, cap = StrokeCap.Round))
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
 * The C# BusyOverlay: a translucent card with a spinner, centered over the page while [busy] has
 * work running. The card takes the taps that land on it; the page around it stays usable, as the
 * C# overlay was input-transparent outside its block.
 */
@Composable
fun BusyOverlay(busy: BusyCounter) {
    if (busy.count <= 0) return
    val styling = LocalStyling.current
    Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
        Box(
            Modifier.background(styling.cardColor.color.copy(alpha = 0.92f), styling.cardShape)
                .pointerInput(Unit) {
                    awaitPointerEventScope {
                        while (true) awaitPointerEvent().changes.forEach { it.consume() }
                    }
                }
                .padding(28.dp),
        ) {
            CircularProgressIndicator(color = styling.accentColor.color, trackColor = styling.borderColor.color)
        }
    }
}
