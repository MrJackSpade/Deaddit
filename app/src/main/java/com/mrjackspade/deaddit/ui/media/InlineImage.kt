package com.mrjackspade.deaddit.ui.media

import androidx.compose.foundation.Image
import androidx.compose.foundation.clickable
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.isSpecified
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.layout.Layout
import androidx.compose.ui.platform.LocalConfiguration
import androidx.compose.ui.unit.Constraints
import androidx.compose.ui.unit.dp
import coil3.compose.AsyncImagePainter
import coil3.compose.LocalPlatformContext
import coil3.compose.rememberAsyncImagePainter
import coil3.request.ImageRequest
import coil3.size.Size
import kotlin.math.roundToInt

/**
 * An image in a comment, sized as the C# app's web view sized it: one image pixel per dp (a CSS
 * pixel), no wider than the space it's in and no taller than the screen, keeping its shape.
 * Loaded at its original resolution, so it's never sampled down; small images are drawn larger.
 * A plain layout (not BoxWithConstraints), so rows that measure intrinsically can hold it.
 */
@Composable
fun InlineImage(url: String, description: String, onClick: () -> Unit, modifier: Modifier = Modifier) {
    val painter = rememberAsyncImagePainter(
        ImageRequest.Builder(LocalPlatformContext.current).data(url).size(Size.ORIGINAL).build(),
    )
    // Redraws when the image arrives and its size is known.
    val state by painter.state.collectAsState()
    val screenHeight = LocalConfiguration.current.screenHeightDp.dp

    Layout(
        content = { Image(painter, contentDescription = description, contentScale = ContentScale.Fit, modifier = Modifier.clickable(onClick = onClick)) },
        modifier = modifier,
    ) { measurables, constraints ->
        val intrinsic = painter.intrinsicSize
        val loaded = state is AsyncImagePainter.State.Success && intrinsic.isSpecified && intrinsic.width > 0f && intrinsic.height > 0f

        // Nothing until it has loaded, as the web view showed nothing; the painter still loads.
        if (!loaded) {
            val placeable = measurables.single().measure(Constraints.fixed(0, 0))
            return@Layout layout(0, 0) { placeable.place(0, 0) }
        }

        val naturalWidth = intrinsic.width.dp.toPx()
        val naturalHeight = intrinsic.height.dp.toPx()
        val maxWidth = if (constraints.hasBoundedWidth) constraints.maxWidth.toFloat() else naturalWidth
        val scale = minOf(1f, maxWidth / naturalWidth, screenHeight.toPx() / naturalHeight)
        val width = (naturalWidth * scale).roundToInt()
        val height = (naturalHeight * scale).roundToInt()

        val placeable = measurables.single().measure(Constraints.fixed(width, height))
        layout(width, height) { placeable.place(0, 0) }
    }
}
