package com.mrjackspade.deaddit.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.RowScope
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp

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

/** The loading indicator, in the highlight color. */
@Composable
fun Spinner() {
    Box(Modifier.fillMaxWidth().padding(8.dp), contentAlignment = Alignment.Center) {
        CircularProgressIndicator(Modifier.size(48.dp), color = LocalStyling.current.highlightColor.color, strokeWidth = 5.dp)
    }
}
