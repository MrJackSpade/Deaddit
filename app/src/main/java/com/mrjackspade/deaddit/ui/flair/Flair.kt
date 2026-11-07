package com.mrjackspade.deaddit.ui.flair

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.TextUnit
import androidx.compose.ui.unit.dp
import coil3.compose.AsyncImage
import com.mrjackspade.deaddit.core.model.FlairPart
import com.mrjackspade.deaddit.core.util.CssColor
import com.mrjackspade.deaddit.core.util.CssColorValue
import com.mrjackspade.deaddit.ui.LocalHacks
import com.mrjackspade.deaddit.ui.LocalStyling
import com.mrjackspade.deaddit.ui.color
import com.mrjackspade.deaddit.ui.html.InlineFlow
import com.mrjackspade.deaddit.ui.html.inlineBackground
import com.mrjackspade.deaddit.ui.px

private val EMOJI_TAG = Regex(":[a-zA-Z0-9\\-_]*:")

/**
 * A user or link flair: its richtext (with emoji images) when there is some and flair images are
 * resolved, otherwise its text. Draws nothing when there's nothing to show.
 * [textColor] is Reddit's "dark"/"light"; [background] is a CSS color or null.
 *
 * [inline] is how a comment shows it, on a line of its own: text flair is an inline box there, its
 * padding painted over the lines around it without making its line taller ([InlineChip]). A post
 * shows it as a block (a flex item in the C# app), padding and margin included in its height.
 * Richtext flair is an inline-flex box either way, so its padding always counts.
 */
@Composable
fun Flair(text: String?, richtext: List<FlairPart>, background: String?, textColor: String?, modifier: Modifier = Modifier, inline: Boolean = false) {
    val styling = LocalStyling.current
    val hacks = LocalHacks.current

    val resolveImages = !hacks.skipResolvingFlairImages
    val parts = if (resolveImages && richtext.isNotEmpty()) richtext.filter { isShown(it) } else null
    val plain = if (parts == null) text?.let { if (hacks.skipResolvingFlairImages) it.replace(EMOJI_TAG, "").trim() else it } else null

    if (parts.isNullOrEmpty() && plain.isNullOrBlank()) return

    val flairText = when (textColor) {
        "dark" -> styling.flairDarkTextColor.color
        "light" -> styling.flairLightTextColor.color
        else -> styling.subTextColor.color
    }

    // The C# app put the color into the flair's style, where a value the browser rejects applied
    // nothing: no background, or (swapped) the inherited text color.
    val parsed = remember(background) { background?.let(CssColor::parse) }
    val swapped = styling.swapFlairColors && background != null

    val fg = if (swapped) (parsed as? CssColorValue.Argb)?.let { Color(it.argb) } ?: Color.Unspecified else flairText
    val bg = when {
        swapped || background == null -> styling.primaryColor.color
        parsed is CssColorValue.Argb -> Color(parsed.argb)
        parsed == CssColorValue.CurrentColor -> fg
        else -> Color.Transparent
    }

    if (parts == null && inline) {
        InlineFlow(modifier) { InlineChip(plain!!, fg, bg, styling.subTextFontSize.px) }
        return
    }

    val box: @Composable (Modifier) -> Unit = { m ->
        Row(
            m.padding(2.dp).background(bg, RoundedCornerShape(4.dp)).padding(4.dp),
            horizontalArrangement = Arrangement.spacedBy(2.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            if (parts != null) {
                for (part in parts) {
                    if (part.type == "emoji") {
                        val size = (styling.subTextFontSize * 1.2).toInt().dp
                        AsyncImage(part.url, contentDescription = part.alias, modifier = Modifier.size(size))
                    } else {
                        Text(part.text!!, color = fg, fontSize = styling.subTextFontSize.px)
                    }
                }
            } else {
                Text(plain!!, color = fg, fontSize = styling.subTextFontSize.px)
            }
        }
    }

    if (inline) InlineFlow(modifier) { box(Modifier) } else box(modifier)
}

/**
 * A text flair or user tag as an inline box (CSS display: inline): 4px of padding and 2px of
 * margin across, the padding above and below painted only, with 4px corners.
 */
@Composable
fun InlineChip(text: String, color: Color, background: Color, fontSize: TextUnit) {
    Text(
        text,
        Modifier.inlineBackground(background, padding = 4.dp, radius = 4.dp, marginStart = 2.dp, marginEnd = 2.dp),
        color = color,
        fontSize = fontSize,
    )
}

private fun isShown(part: FlairPart): Boolean =
    if (part.type == "emoji") !part.url.isNullOrBlank() else !part.text.isNullOrBlank()
