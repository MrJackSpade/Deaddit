package com.mrjackspade.deaddit.ui

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.staticCompositionLocalOf
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.TextUnit
import androidx.compose.ui.unit.sp
import com.mrjackspade.deaddit.core.config.ApplicationHacks
import com.mrjackspade.deaddit.core.config.ApplicationStyling
import com.mrjackspade.deaddit.core.config.DynamicColor

/** The user's styling settings: every color and size the app draws with comes from here. */
val LocalStyling = staticCompositionLocalOf { ApplicationStyling() }

/** The user's "Hacks" settings, for the display options among them. */
val LocalHacks = staticCompositionLocalOf { ApplicationHacks() }

val DynamicColor.color: Color get() = Color(toArgb())

/** The C# app sized text in CSS px, which Android draws at about the size of sp. */
val Double.px: TextUnit get() = toFloat().sp

@Composable
fun DeadditTheme(styling: ApplicationStyling, hacks: ApplicationHacks, content: @Composable () -> Unit) {
    val scheme = darkColorScheme(
        primary = styling.hyperlinkColor.color,
        background = styling.secondaryColor.color,
        surface = styling.primaryColor.color,
        surfaceContainerHigh = styling.tertiaryColor.color,
        onBackground = styling.textColor.color,
        onSurface = styling.textColor.color,
        onSurfaceVariant = styling.subTextColor.color,
    )

    CompositionLocalProvider(LocalStyling provides styling, LocalHacks provides hacks) {
        MaterialTheme(colorScheme = scheme, content = content)
    }
}
