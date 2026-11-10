package com.mrjackspade.deaddit.ui

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Typography
import androidx.compose.material3.darkColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.staticCompositionLocalOf
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.ExperimentalTextApi
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.Font
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontStyle
import androidx.compose.ui.text.font.FontVariation
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.TextUnit
import androidx.compose.ui.unit.sp
import com.mrjackspade.deaddit.R
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

/**
 * Atkinson Hyperlegible Next (SIL Open Font License, assets/licenses): made for readers with low
 * vision, its letters are built to be told apart. One variable font per style, at the weights used.
 */
@OptIn(ExperimentalTextApi::class)
val AppFont = FontFamily(
    listOf(FontWeight.Normal, FontWeight.Medium, FontWeight.SemiBold, FontWeight.Bold).flatMap { weight ->
        listOf(
            Font(R.font.atkinson_hyperlegible_next, weight, FontStyle.Normal, variationSettings = FontVariation.Settings(FontVariation.weight(weight.weight))),
            Font(R.font.atkinson_hyperlegible_next_italic, weight, FontStyle.Italic, variationSettings = FontVariation.Settings(FontVariation.weight(weight.weight))),
        )
    },
)

@Composable
fun DeadditTheme(styling: ApplicationStyling, hacks: ApplicationHacks, content: @Composable () -> Unit) {
    val scheme = darkColorScheme(
        primary = styling.accentColor.color,
        onPrimary = styling.secondaryColor.color,
        background = styling.secondaryColor.color,
        onBackground = styling.textColor.color,
        surface = styling.cardColor.color,
        surfaceContainerHigh = styling.cardColor.color,
        surfaceContainer = styling.cardColor.color,
        onSurface = styling.textColor.color,
        onSurfaceVariant = styling.subTextColor.color,
        outline = styling.borderColor.color,
        outlineVariant = styling.borderColor.color,
    )

    CompositionLocalProvider(LocalStyling provides styling, LocalHacks provides hacks) {
        MaterialTheme(colorScheme = scheme, typography = AppTypography, content = content)
    }
}

/**
 * Material's text styles fix each line's height for their own font size (24sp for body text);
 * with the user's sizes instead that spread lines apart. Lines take the font's own height
 * instead, as they did in the C# app's WebView (CSS "line-height: normal"). Material also spaces
 * letters apart (0.5sp for body text); the C# app's text had no letter spacing. Every style is
 * in the app's font.
 */
private val AppTypography: Typography = Typography().let { t ->
    fun TextStyle.natural() = copy(lineHeight = TextUnit.Unspecified, letterSpacing = 0.sp, fontFamily = AppFont)
    Typography(
        displayLarge = t.displayLarge.natural(), displayMedium = t.displayMedium.natural(), displaySmall = t.displaySmall.natural(),
        headlineLarge = t.headlineLarge.natural(), headlineMedium = t.headlineMedium.natural(), headlineSmall = t.headlineSmall.natural(),
        titleLarge = t.titleLarge.natural(), titleMedium = t.titleMedium.natural(), titleSmall = t.titleSmall.natural(),
        bodyLarge = t.bodyLarge.natural(), bodyMedium = t.bodyMedium.natural(), bodySmall = t.bodySmall.natural(),
        labelLarge = t.labelLarge.natural(), labelMedium = t.labelMedium.natural(), labelSmall = t.labelSmall.natural(),
    )
}
